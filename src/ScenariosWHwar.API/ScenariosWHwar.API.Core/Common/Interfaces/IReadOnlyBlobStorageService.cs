namespace ScenariosWHwar.API.Core.Common.Interfaces;

/// <summary>
/// Interface for read-only blob storage operations.
/// Provides functionality to generate secure read URLs for accessing blob content.
/// </summary>
public interface IReadOnlyBlobStorageService : IBlobStorageService
{
    /// <summary>
    /// Generates a secure read URL for accessing a blob.
    /// </summary>
    /// <param name="blobPath">The path to the blob within the container</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A URL that allows read access to the specified blob</returns>
    Task<string> GenerateReadUrlAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a secure read URL with custom expiration time for accessing a blob.
    /// </summary>
    /// <param name="blobPath">The path to the blob within the container</param>
    /// <param name="expirationMinutes">Custom expiration time in minutes</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A URL that allows read access to the specified blob</returns>
    Task<string> GenerateReadUrlAsync(string blobPath, int expirationMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public URL for a blob (without SAS token).
    /// Use this for publicly accessible blobs or when using CDN.
    /// </summary>
    /// <param name="blobPath">The path to the blob within the container</param>
    /// <returns>The public URL of the blob</returns>
    string GetPublicUrl(string blobPath);
}
