using Microsoft.Extensions.Options;
using ScenariosWHwar.Function.Processor.Configuration;

namespace ScenariosWHwar.Function.Processor.Services;

/// <summary>
/// Interface for retry policy service
/// </summary>
public interface IRetryPolicyService
{
    /// <summary>
    /// Executes a function with retry policy
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<ErrorOr<T>> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<ErrorOr<T>>> operation,
        string operationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function with retry policy (no return value)
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<ErrorOr<Success>>> operation,
        string operationName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service that provides retry policy functionality
/// </summary>
public class RetryPolicyService : IRetryPolicyService
{
    private readonly ProcessorConfig _config;
    private readonly ILogger<RetryPolicyService> _logger;

    public RetryPolicyService(
        IOptions<ProcessorConfig> config,
        ILogger<RetryPolicyService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<T>> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<ErrorOr<T>>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var maxAttempts = _config.MaxRetryAttempts;
        var baseDelay = TimeSpan.FromSeconds(_config.RetryDelaySeconds);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogDebug("Executing {OperationName}, attempt {Attempt}/{MaxAttempts}",
                    operationName, attempt, maxAttempts);

                var result = await operation(cancellationToken);

                if (result.IsError)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Operation {OperationName} failed on attempt {Attempt}/{MaxAttempts}: {Errors}",
                        operationName, attempt, maxAttempts, errors);

                    if (attempt == maxAttempts)
                    {
                        _logger.LogError("Operation {OperationName} failed after {MaxAttempts} attempts",
                            operationName, maxAttempts);
                        return result;
                    }

                    // Check if the error is retryable
                    if (!IsRetryableError(result.Errors))
                    {
                        _logger.LogWarning("Operation {OperationName} failed with non-retryable error, aborting retry attempts",
                            operationName);
                        return result;
                    }
                }
                else
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation("Operation {OperationName} succeeded on attempt {Attempt}",
                            operationName, attempt);
                    }
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation {OperationName} was cancelled", operationName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception in operation {OperationName}, attempt {Attempt}/{MaxAttempts}",
                    operationName, attempt, maxAttempts);

                if (attempt == maxAttempts)
                {
                    return Error.Failure($"{operationName}.UnexpectedError", $"Unexpected error: {ex.Message}");
                }
            }

            // Calculate exponential backoff delay
            var delay = TimeSpan.FromTicks(baseDelay.Ticks * (long)Math.Pow(2, attempt - 1));
            _logger.LogDebug("Waiting {Delay}ms before retry {NextAttempt}/{MaxAttempts}",
                delay.TotalMilliseconds, attempt + 1, maxAttempts);

            await Task.Delay(delay, cancellationToken);
        }

        // This should never be reached, but included for completeness
        return Error.Failure($"{operationName}.MaxRetriesExceeded", "Maximum retry attempts exceeded");
    }

    public async Task<ErrorOr<Success>> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<ErrorOr<Success>>> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync<Success>(operation, operationName, cancellationToken);
    }

    /// <summary>
    /// Determines if an error is retryable based on its type and characteristics
    /// </summary>
    private static bool IsRetryableError(List<Error> errors)
    {
        // Don't retry validation errors
        if (errors.Any(e => e.Type == ErrorType.Validation))
        {
            return false;
        }

        // Don't retry not found errors
        if (errors.Any(e => e.Type == ErrorType.NotFound))
        {
            return false;
        }

        // Retry failure and unexpected errors
        return errors.Any(e => e.Type == ErrorType.Failure || e.Type == ErrorType.Unexpected);
    }
}
