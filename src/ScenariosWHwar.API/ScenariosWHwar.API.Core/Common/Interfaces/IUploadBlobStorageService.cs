namespace ScenariosWHwar.API.Core.Common.Interfaces;

public interface IUploadBlobStorageService : IBlobStorageService
{
    Task<string> GenerateUploadSasUrlAsync(string blobPath, CancellationToken cancellationToken = default);
}