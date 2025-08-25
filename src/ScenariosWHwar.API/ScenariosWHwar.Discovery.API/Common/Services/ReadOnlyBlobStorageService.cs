using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Configurations;
using ScenariosWHwar.API.Core.Common.Interfaces;

namespace ScenariosWHwar.Discovery.API.Common.Services;

/// <summary>
/// Service for handling read-only Azure Blob Storage operations.
/// Provides functionality to generate secure read URLs for accessing blob content.
/// </summary>
/// <remarks>
/// This service implements the read-only blob storage operations for the Discovery API.
/// It generates SAS (Shared Access Signature) URLs that allow clients to read files
/// from Azure Blob Storage without exposing storage account credentials.
///
/// The service follows the principle of least privilege by only granting read
/// permissions for the specific blob path and limited time duration.
///
/// For public content or CDN scenarios, it also provides public URL generation
/// without SAS tokens.
/// </remarks>
public class ReadOnlyBlobStorageService : IReadOnlyBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageConfig _config;
    private readonly ILogger<ReadOnlyBlobStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the ReadOnlyBlobStorageService.
    /// </summary>
    /// <param name="config">Azure Storage configuration options</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <exception cref="ArgumentNullException">Thrown when config or logger is null</exception>
    /// <exception cref="ArgumentException">Thrown when connection string is null or empty</exception>
    public ReadOnlyBlobStorageService(
        IOptions<AzureStorageConfig> config,
        ILogger<ReadOnlyBlobStorageService> logger)
    {
        ThrowIfNull(config);
        ThrowIfNull(logger);

        _config = config.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_config.ConnectionString))
        {
            throw new ArgumentException("Azure Storage connection string cannot be null or empty", nameof(config));
        }

        _blobServiceClient = new BlobServiceClient(_config.ConnectionString);
    }

    /// <summary>
    /// Generates a secure read URL for accessing a blob with default expiration.
    /// </summary>
    /// <param name="blobPath">The path to the blob within the container</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A URL that allows read access to the specified blob</returns>
    /// <exception cref="ArgumentException">Thrown when blobPath is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when blob container operations fail</exception>
    /// <remarks>
    /// This method:
    /// 1. Validates the blob path parameter
    /// 2. Ensures the target container exists
    /// 3. Generates a SAS token with read permissions only
    /// 4. Returns a complete URL that clients can use for direct access
    ///
    /// The SAS token includes:
    /// - Read permission only (principle of least privilege)
    /// - Expiration time based on configuration (default: 60 minutes)
    /// - Scope limited to the specific blob path
    ///
    /// Security considerations:
    /// - The URL grants temporary read access to a specific path only
    /// - No write, delete, or list permissions are granted
    /// - The token automatically expires after the configured time
    /// </remarks>
    public async Task<string> GenerateReadUrlAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        return await GenerateReadUrlAsync(blobPath, _config.SasTokenExpiryMinutes, cancellationToken);
    }

    /// <summary>
    /// Generates a secure read URL for accessing a blob with custom expiration time.
    /// </summary>
    /// <param name="blobPath">The path to the blob within the container</param>
    /// <param name="expirationMinutes">Custom expiration time in minutes</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A URL that allows read access to the specified blob</returns>
    /// <exception cref="ArgumentException">Thrown when blobPath is null or empty or expirationMinutes is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when blob container operations fail</exception>
    public async Task<string> GenerateReadUrlAsync(string blobPath, int expirationMinutes, CancellationToken cancellationToken = default)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new ArgumentException("Blob path cannot be null or empty", nameof(blobPath));
        }

        if (expirationMinutes <= 0)
        {
            throw new ArgumentException("Expiration minutes must be greater than zero", nameof(expirationMinutes));
        }

        _logger.LogDebug("Generating read SAS URL for blob path: {BlobPathValue}, expiration: {ExpirationMinutes} minutes",
            blobPath, expirationMinutes);

        try
        {
            // Remove leading slash if present for consistency
            blobPath = blobPath.TrimStart('/');

            // Get the container client
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.VideosContainerName);

            // Verify the container exists
            var containerExists = await containerClient.ExistsAsync(cancellationToken);
            if (!containerExists.Value)
            {
                _logger.LogError("Container {ContainerName} does not exist", _config.VideosContainerName);
                throw new InvalidOperationException($"Container '{_config.VideosContainerName}' does not exist");
            }

            // Get the blob client for the specific path
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Verify the blob exists
            var blobExists = await blobClient.ExistsAsync(cancellationToken);
            if (!blobExists.Value)
            {
                _logger.LogWarning("Blob does not exist: {BlobPathValue}", blobPath);
                throw new InvalidOperationException($"Blob does not exist: {blobPath}");
            }

            // Create SAS builder with specific permissions and expiration
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _config.VideosContainerName,
                BlobName = blobPath,
                Resource = "b", // 'b' indicates this is for a blob (as opposed to container)
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
            };

            // Set minimal required permissions - only read
            // This follows the principle of least privilege
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Generate the SAS URI
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            var sasUrl = sasUri.ToString();

            _logger.LogDebug("Successfully generated read SAS URL for blob: {BlobPathValue}, expires at: {ExpiryTime}",
                blobPath, sasBuilder.ExpiresOn);

            return sasUrl;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError(ex, "Access denied when generating read SAS URL for blob: {BlobPathValue}. Check storage account permissions.", blobPath);
            throw new InvalidOperationException($"Access denied to storage account. Verify connection string and permissions.", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "Blob not found when generating read SAS URL: {BlobPathValue}", blobPath);
            throw new InvalidOperationException($"Blob not found: {blobPath}", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status is >= 400 and < 500)
        {
            _logger.LogError(ex, "Client error when generating read SAS URL for blob: {BlobPathValue}. Status: {Status}", blobPath, ex.Status);
            throw new InvalidOperationException($"Failed to generate read SAS URL due to client error: {ex.Message}", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status >= 500)
        {
            _logger.LogError(ex, "Server error when generating read SAS URL for blob: {BlobPathValue}. Status: {Status}", blobPath, ex.Status);
            throw new InvalidOperationException($"Azure Storage service is temporarily unavailable: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when generating read SAS URL for blob: {BlobPathValue}", blobPath);
            throw new InvalidOperationException($"Failed to generate read SAS URL for blob: {blobPath}", ex);
        }
    }

    /// <summary>
    /// Gets the public URL for a blob (without SAS token).
    /// Use this for publicly accessible blobs or when using CDN.
    /// </summary>
    /// <param name="blobPath">The path to the blob within the container</param>
    /// <returns>The public URL of the blob</returns>
    /// <exception cref="ArgumentException">Thrown when blobPath is null or empty</exception>
    /// <remarks>
    /// This method generates a public URL without SAS token authentication.
    /// Use this when:
    /// - The blob container is configured for public access
    /// - You're using Azure CDN for content delivery
    /// - The blob will be accessed through a public endpoint
    ///
    /// Note: This URL will only work if the container allows public access
    /// or if you have other authentication mechanisms in place.
    /// </remarks>
    public string GetPublicUrl(string blobPath)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new ArgumentException("Blob path cannot be null or empty", nameof(blobPath));
        }

        _logger.LogDebug("Generating public URL for blob path: {BlobPathValue}", blobPath);

        try
        {
            // Remove leading slash if present for consistency
            blobPath = blobPath.TrimStart('/');

            // Get the container client
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.VideosContainerName);

            // Get the blob client for the specific path
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Return the public URI
            var publicUrl = blobClient.Uri.ToString();

            _logger.LogDebug("Generated public URL for blob: {BlobPathValue} -> {PublicUrl}", blobPath, publicUrl);

            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating public URL for blob: {BlobPathValue}", blobPath);
            throw new InvalidOperationException($"Failed to generate public URL for blob: {blobPath}", ex);
        }
    }
}
