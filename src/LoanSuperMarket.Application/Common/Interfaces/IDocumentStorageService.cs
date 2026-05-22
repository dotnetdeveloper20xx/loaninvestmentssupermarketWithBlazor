namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IDocumentStorageService
{
    Task<string> StoreAsync(Stream fileStream, string fileName, CancellationToken ct);

    Task<Stream> RetrieveAsync(string storageReference, CancellationToken ct);

    Task DeleteAsync(string storageReference, CancellationToken ct);
}
