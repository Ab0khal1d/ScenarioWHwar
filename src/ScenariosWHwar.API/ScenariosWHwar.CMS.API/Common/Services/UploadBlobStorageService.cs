
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Configurations;

namespace ScenariosWHwar.CMS.API.Common.Services;

/// <summary>
/// Service for handling Azure Blob Storage upload operations.
/// Provides functionality to generate secure upload URLs for direct client uploads.
/// </summary>
/// <remarks>
/// This service implements the upload-specific blob storage operations for the CMS API.
/// It generates SAS (Shared Access Signature) URLs that allow clients to upload files
/// directly to Azure Blob Storage without exposing storage account credentials.
///
/// The service follows the principle of least privilege by only granting write and create
/// permissions for the specific blob path and limited time duration.
/// </remarks>
public class UploadBlobStorageService : IUploadBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageConfig _config;
    private readonly ILogger<UploadBlobStorageService> _logger;

    /// <summary>
    /// Initializes a new instance of the UploadBlobStorageService.
    /// </summary>
    /// <param name="config">Azure Storage configuration options</param>
    /// <param name="logger">Logger for diagnostic information</param>
    /// <exception cref="ArgumentNullException">Thrown when config or logger is null</exception>
    /// <exception cref="ArgumentException">Thrown when connection string is null or empty</exception>
    public UploadBlobStorageService(
        IOptions<AzureStorageConfig> config,
        ILogger<UploadBlobStorageService> logger)
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
    /// Generates a pre-authorized SAS URL for uploading a file to Azure Blob Storage.
    /// </summary>
    /// <param name="blobPath">The target path for the blob within the container</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A SAS URL that allows direct upload to the specified blob path</returns>
    /// <exception cref="ArgumentException">Thrown when blobPath is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when blob container operations fail</exception>
    /// <remarks>
    /// This method:
    /// 1. Validates the blob path parameter
    /// 2. Ensures the target container exists (creates if necessary)
    /// 3. Generates a SAS token with write and create permissions
    /// 4. Returns a complete URL that clients can use for direct upload
    ///
    /// The SAS token includes:
    /// - Write and Create permissions only (principle of least privilege)
    /// - Expiration time based on configuration (default: 60 minutes)
    /// - Scope limited to the specific blob path
    ///
    /// Security considerations:
    /// - The URL grants temporary access to upload to a specific path only
    /// - No read or delete permissions are granted
    /// - The token automatically expires after the configured time
    /// </remarks>
    public async Task<string> GenerateUploadSasUrlAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(blobPath))
        {
            throw new ArgumentException("Blob path cannot be null or empty", nameof(blobPath));
        }

        _logger.LogDebug("Generating upload SAS URL for blob path: {BlobPathValue}", blobPath);

        try
        {
            blobPath = blobPath.Substring(1);
            // Get or create the container client
            var containerClient = _blobServiceClient.GetBlobContainerClient(_config.VideosContainerName);

            // Ensure the container exists - this is idempotent and safe to call multiple times
            var containerResponse = await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            if (containerResponse?.Value != null)
            {
                _logger.LogInformation("Created new blob container: {ContainerName}", _config.VideosContainerName);
            }

            // Get the blob client for the specific path
            var blobClient = containerClient.GetBlobClient(blobPath);

            // Create SAS builder with specific permissions and expiration
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _config.VideosContainerName,
                BlobName = blobPath,
                Resource = "b", // 'b' indicates this is for a blob (as opposed to container)
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_config.SasTokenExpiryMinutes)
            };

            // Set minimal required permissions - only write and create
            // This follows the principle of least privilege
            sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

            // Generate the SAS URI
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            var sasUrl = sasUri.ToString();

            _logger.LogDebug("Successfully generated SAS URL for blob: {BlobPathValue}, expires at: {ExpiryTime}",
                blobPath, sasBuilder.ExpiresOn);

            return sasUrl;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError(ex, "Access denied when generating SAS URL for blob: {BlobPathValue}. Check storage account permissions.", blobPath);
            throw new InvalidOperationException($"Access denied to storage account. Verify connection string and permissions.", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status is >= 400 and < 500)
        {
            _logger.LogError(ex, "Client error when generating SAS URL for blob: {BlobPathValue}. Status: {Status}", blobPath, ex.Status);
            throw new InvalidOperationException($"Failed to generate SAS URL due to client error: {ex.Message}", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status >= 500)
        {
            _logger.LogError(ex, "Server error when generating SAS URL for blob: {BlobPathValue}. Status: {Status}", blobPath, ex.Status);
            throw new InvalidOperationException($"Azure Storage service is temporarily unavailable: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when generating SAS URL for blob: {BlobPathValue}", blobPath);
            throw new InvalidOperationException($"Failed to generate upload SAS URL for blob: {blobPath}", ex);
        }
    }
}
