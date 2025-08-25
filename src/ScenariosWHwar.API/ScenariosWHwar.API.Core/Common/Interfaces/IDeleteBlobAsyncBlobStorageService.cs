namespace ScenariosWHwar.API.Core.Common.Interfaces;

public interface IDeleteBlobAsyncBlobStorageService : IBlobStorageService
{
    Task<bool> DeleteBlobAsync(string blobPath, CancellationToken cancellationToken = default);
}