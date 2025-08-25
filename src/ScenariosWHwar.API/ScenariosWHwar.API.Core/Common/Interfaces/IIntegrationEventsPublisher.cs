namespace ScenariosWHwar.API.Core.Common.Interfaces;

/// <summary>
/// Interface for publishing integration events to Azure Service Bus.
/// This interface defines the contract for sending messages to Service Bus queues
/// for integration with external systems and microservices.
/// </summary>
public interface IIntegrationEventsPublisher
{
    /// <summary>
    /// Publishes a message to the specified Service Bus queue.
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="message">The message object to serialize and send</param>
    /// <param name="queueName">The name of the Service Bus queue to send to</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous publish operation</returns>
    Task PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default);
}
