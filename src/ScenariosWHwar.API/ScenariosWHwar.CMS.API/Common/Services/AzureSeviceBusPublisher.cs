
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Configurations;
using System.Diagnostics;
using System.Text.Json;

namespace ScenariosWHwar.CMS.API.Common.Services;

/// <summary>
/// Azure Service Bus implementation for publishing integration events.
/// Provides reliable message delivery to Service Bus queues for integration
/// with external systems and microservices.
/// </summary>
/// <remarks>
/// This service implements the integration events publishing functionality using Azure Service Bus.
/// It handles message serialization, delivery, and proper resource management.
///
/// Key features:
/// - Automatic message serialization to JSON
/// - Proper connection management and disposal
/// - Retry mechanisms via Azure Service Bus client
/// - Comprehensive logging for monitoring and debugging
/// - Support for message metadata and correlation
///
/// The service follows Azure Service Bus best practices:
/// - Uses ServiceBusClient for connection pooling
/// - Implements proper disposal pattern
/// - Handles transient errors gracefully
/// - Provides detailed telemetry and logging
/// </remarks>
public class AzureSeviceBusPublisher : IIntegrationEventsPublisher, IDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusConfig _config;
    private readonly ILogger<AzureSeviceBusPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the AzureServiceBusPublisher.
    /// </summary>
    /// <param name="config">Service Bus configuration options</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <exception cref="ArgumentNullException">Thrown when config or logger is null</exception>
    /// <exception cref="ArgumentException">Thrown when connection string is null or empty</exception>
    public AzureSeviceBusPublisher(
        IOptions<ServiceBusConfig> config,
        ILogger<AzureSeviceBusPublisher> logger)
    {
        ThrowIfNull(config);
        ThrowIfNull(logger);

        _config = config.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_config.ConnectionString))
        {
            throw new ArgumentException("Service Bus connection string cannot be null or empty", nameof(config));
        }

        // Configure JSON serialization options for consistent message format
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false // Compact JSON for efficiency
        };

        try
        {
            // Create Service Bus client with default retry options
            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp,
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = ServiceBusRetryMode.Exponential,
                    MaxRetries = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.FromSeconds(30),
                    TryTimeout = TimeSpan.FromMinutes(1)
                }
            };

            _serviceBusClient = new ServiceBusClient(_config.ConnectionString, clientOptions);

            _logger.LogDebug("Azure Service Bus client initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Service Bus client");
            throw new InvalidOperationException("Failed to initialize Service Bus connection", ex);
        }
    }

    /// <summary>
    /// Publishes a message to the specified Azure Service Bus queue.
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="message">The message object to serialize and send</param>
    /// <param name="queueName">The name of the Service Bus queue to send to</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous publish operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when message is null</exception>
    /// <exception cref="ArgumentException">Thrown when queueName is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when Service Bus operations fail</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the publisher has been disposed</exception>
    /// <remarks>
    /// This method:
    /// 1. Validates input parameters
    /// 2. Serializes the message to JSON format
    /// 3. Creates a Service Bus message with appropriate metadata
    /// 4. Sends the message to the specified queue
    /// 5. Handles errors and provides comprehensive logging
    ///
    /// Message format:
    /// - Body: JSON serialized message content
    /// - ContentType: "application/json"
    /// - Subject: Message type name for easy filtering
    /// - MessageId: Unique identifier for deduplication
    /// - TimeToLive: Default TTL to prevent queue bloat
    ///
    /// Error handling:
    /// - Transient errors are automatically retried by the Azure SDK
    /// - Non-transient errors are logged and re-thrown
    /// - All exceptions include contextual information for debugging
    /// </remarks>
    public async Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Validate input parameters
        ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));
        }

        var messageType = typeof(T).Name;
        var messageId = Guid.NewGuid().ToString();

        _logger.LogDebug("Publishing message of type {MessageType} to queue {QueueName} with ID {MessageId}",
            messageType, queueName, messageId);

        ServiceBusSender? sender = null;

        try
        {
            // Create sender for the specific queue
            sender = _serviceBusClient.CreateSender(queueName);

            // Serialize message to JSON
            var messageBody = JsonSerializer.Serialize(message, _jsonOptions);

            // Create Service Bus message with metadata
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                Subject = messageType, // Allows consumers to filter by message type
                MessageId = messageId, // Enables deduplication if needed
                TimeToLive = TimeSpan.FromDays(7), // Prevent indefinite queue growth
                CorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString() // For distributed tracing
            };

            // Add custom properties for additional metadata
            serviceBusMessage.ApplicationProperties.Add("MessageType", messageType);
            serviceBusMessage.ApplicationProperties.Add("PublishedAt", DateTimeOffset.UtcNow);
            serviceBusMessage.ApplicationProperties.Add("PublisherService", "CMS.API");

            // Send the message
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation("Successfully published message of type {MessageType} to queue {QueueName} with ID {MessageId}",
                messageType, queueName, messageId);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            _logger.LogError(ex, "Service Bus queue {QueueName} does not exist. Message type: {MessageType}, ID: {MessageId}",
                queueName, messageType, messageId);
            throw new InvalidOperationException($"Service Bus queue '{queueName}' does not exist", ex);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.QuotaExceeded)
        {
            _logger.LogError(ex, "Service Bus queue {QueueName} quota exceeded. Message type: {MessageType}, ID: {MessageId}",
                queueName, messageType, messageId);
            throw new InvalidOperationException($"Service Bus queue '{queueName}' quota exceeded", ex);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "Service Bus error when publishing to queue {QueueName}. Reason: {Reason}, Message type: {MessageType}, ID: {MessageId}",
                queueName, ex.Reason, messageType, messageId);
            throw new InvalidOperationException($"Failed to publish message to Service Bus queue '{queueName}': {ex.Reason}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize message of type {MessageType} to JSON. ID: {MessageId}",
                messageType, messageId);
            throw new InvalidOperationException($"Failed to serialize message of type '{messageType}' to JSON", ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Publishing message of type {MessageType} to queue {QueueName} was cancelled. ID: {MessageId}",
                messageType, queueName, messageId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when publishing message of type {MessageType} to queue {QueueName}. ID: {MessageId}",
                messageType, queueName, messageId);
            throw new InvalidOperationException($"Failed to publish message to Service Bus queue '{queueName}'", ex);
        }
        finally
        {
            // Dispose sender to release resources
            if (sender != null)
            {
                await sender.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the AzureServiceBusPublisher.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _serviceBusClient?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
                _logger.LogDebug("Azure Service Bus client disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while disposing Azure Service Bus client");
            }

            _disposed = true;
        }
    }
}
