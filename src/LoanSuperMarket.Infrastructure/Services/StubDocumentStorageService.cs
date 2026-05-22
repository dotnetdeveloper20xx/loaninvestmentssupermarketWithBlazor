using LoanSuperMarket.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace LoanSuperMarket.Infrastructure.Services;

public sealed class StubDocumentStorageService : IDocumentStorageService
{
    private readonly string _basePath;

    public StubDocumentStorageService(IHostEnvironment environment)
    {
        _basePath = Path.Combine(environment.ContentRootPath, "App_Data", "documents");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> StoreAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        var storageReference = $"{Guid.NewGuid():N}/{fileName}";
        var fullPath = Path.Combine(_basePath, storageReference);

        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        await using var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStreamOut, ct);

        return storageReference;
    }

    public Task<Stream> RetrieveAsync(string storageReference, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, storageReference);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Document not found: {storageReference}");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageReference, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, storageReference);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        // Clean up empty parent directory
        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null && Directory.Exists(directory) &&
            !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }

        return Task.CompletedTask;
    }
}
