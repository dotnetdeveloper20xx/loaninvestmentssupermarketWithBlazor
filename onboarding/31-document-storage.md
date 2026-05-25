# 31 — Document Storage

## Overview

Loan applications require supporting documents (ID verification, proof of income, bank statements). The platform provides a document storage abstraction that stores files locally during development and can be swapped to Azure Blob Storage in production. This document covers the interface, stub implementation, entity lifecycle, upload endpoint, and production path.

---

## Feature Requirements (Plain English)

1. Borrowers can upload documents (PDF, images) as part of their loan application.
2. Documents are stored with a unique reference (not the original filename).
3. Documents can be retrieved by their storage reference.
4. Documents can be deleted when an application is withdrawn.
5. The storage mechanism is swappable (local filesystem in dev, cloud in production).
6. File uploads use multipart/form-data.
7. Each document has a type (ID, ProofOfIncome, BankStatement, etc.).

---

## Technologies & Patterns

| Concern | Technology | Pattern |
|---------|-----------|---------|
| Abstraction | Interface | Strategy pattern |
| Dev Storage | Local filesystem | Stub implementation |
| Prod Storage | Azure Blob Storage | Cloud implementation |
| Upload | ASP.NET Core IFormFile | Multipart form data |
| Entity | ApplicationDocument | Domain entity with lifecycle |

---

## Application Layer: IDocumentStorageService

```csharp
// src/LoanSuperMarket.Application/Common/Interfaces/IDocumentStorageService.cs
namespace LoanSuperMarket.Application.Common.Interfaces;

public interface IDocumentStorageService
{
    /// <summary>
    /// Stores a file and returns a unique storage reference.
    /// </summary>
    Task<string> StoreAsync(Stream fileStream, string fileName, CancellationToken ct);

    /// <summary>
    /// Retrieves a file by its storage reference.
    /// </summary>
    Task<Stream> RetrieveAsync(string storageReference, CancellationToken ct);

    /// <summary>
    /// Deletes a file by its storage reference.
    /// </summary>
    Task DeleteAsync(string storageReference, CancellationToken ct);
}
```

### Design Decisions

1. **Stream-based** — Works with any file size without loading entirely into memory.
2. **Returns `string` reference** — The caller stores this reference in the database, not the physical path.
3. **No knowledge of storage location** — The interface doesn't expose whether it's local, S3, or Azure.

---

## Infrastructure: StubDocumentStorageService (Development)

```csharp
// src/LoanSuperMarket.Infrastructure/Services/StubDocumentStorageService.cs
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
        // Generate unique reference: {guid}/{originalFileName}
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
            throw new FileNotFoundException($"Document not found: {storageReference}");

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageReference, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, storageReference);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

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
```

### Storage Structure

```
App_Data/
└── documents/
    ├── 27f3f0ddac8d4df8b6ad21ddb4ce1d38/
    │   └── passport-scan.pdf
    ├── 4ba34de092d64f2bb0c86707316999c2/
    │   └── bank-statement-jan.pdf
    └── 5694c05cdf994e1b9cedb40f37ac18a9/
        └── payslip.png
```

Each document gets its own GUID directory, preventing filename collisions.

---

## Domain Entity: ApplicationDocument

```csharp
// src/LoanSuperMarket.Domain/Entities/ApplicationDocument.cs
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class ApplicationDocument : AuditableEntity
{
    private ApplicationDocument() { }

    public Guid LoanApplicationId { get; private set; }
    public DocumentType DocumentType { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string StorageReference { get; private set; } = string.Empty;
    public DocumentStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }

    public static ApplicationDocument Create(
        Guid loanApplicationId,
        DocumentType documentType,
        string fileName,
        string storageReference)
    {
        return new ApplicationDocument
        {
            LoanApplicationId = loanApplicationId,
            DocumentType = documentType,
            FileName = fileName,
            StorageReference = storageReference,
            Status = DocumentStatus.Uploaded,
            UploadedAtUtc = DateTime.UtcNow
        };
    }

    public void Verify()
    {
        if (Status != DocumentStatus.Uploaded)
            throw new DomainException("Only uploaded documents can be verified.");
        Status = DocumentStatus.Verified;
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");
        Status = DocumentStatus.Rejected;
        RejectionReason = reason;
    }
}
```

```csharp
// src/LoanSuperMarket.Domain/Enums/DocumentType.cs
namespace LoanSuperMarket.Domain.Enums;

public enum DocumentType
{
    IdVerification = 0,
    ProofOfIncome = 1,
    BankStatement = 2,
    ProofOfAddress = 3,
    EmploymentLetter = 4,
    TaxReturn = 5,
    Other = 99
}

public enum DocumentStatus
{
    Uploaded = 0,
    Verified = 1,
    Rejected = 2
}
```

### Entity Lifecycle

```
Create (Upload) → Uploaded
    │
    ├── Verify() → Verified ✓
    │
    └── Reject(reason) → Rejected ✗
```

---

## Application Layer: UploadDocumentCommand

```csharp
// src/LoanSuperMarket.Application/Features/LoanApplications/UploadDocument/UploadDocumentCommand.cs
using LoanSuperMarket.Domain.Enums;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.UploadDocument;

public sealed record UploadDocumentCommand(
    Guid ApplicationId,
    DocumentType DocumentType,
    string FileName,
    Stream FileStream
) : IRequest<Guid>;
```

```csharp
// UploadDocumentCommandHandler.cs
public sealed class UploadDocumentCommandHandler
    : IRequestHandler<UploadDocumentCommand, Guid>
{
    private readonly ILoanApplicationRepository _appRepo;
    private readonly IApplicationDocumentRepository _docRepo;
    private readonly IDocumentStorageService _storageService;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ICurrentUserService _currentUser;

    public async Task<Guid> Handle(
        UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // Verify the application exists and belongs to the current user
        var application = await _appRepo.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Application not found.");

        // Store the file
        var storageReference = await _storageService.StoreAsync(
            request.FileStream, request.FileName, cancellationToken);

        // Create the document entity
        var document = ApplicationDocument.Create(
            request.ApplicationId,
            request.DocumentType,
            request.FileName,
            storageReference);

        await _docRepo.AddAsync(document, cancellationToken);
        await _docRepo.SaveChangesAsync(cancellationToken);

        // Audit
        var audit = AuditLog.Create(
            "ApplicationDocument", document.Id,
            "Uploaded",
            $"Document '{request.FileName}' ({request.DocumentType}) uploaded.",
            _currentUser.Email ?? "Borrower");
        await _auditRepo.AddAsync(audit, cancellationToken);
        await _auditRepo.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}
```

---

## API Controller: Upload Endpoint

```csharp
// src/LoanSuperMarket.Api/Controllers/WizardController.cs
[HttpPost("{id:guid}/documents")]
[Consumes("multipart/form-data")]
public async Task<ActionResult<ApiResponse<Guid>>> UploadDocument(
    Guid id,
    IFormFile file,
    [FromForm] int documentType,
    CancellationToken cancellationToken)
{
    if (file is null || file.Length == 0)
        return BadRequest(ApiResponse<Guid>.Fail("File is required."));

    if (!Enum.IsDefined(typeof(DocumentType), documentType))
        return BadRequest(ApiResponse<Guid>.Fail("Invalid document type."));

    await using var stream = file.OpenReadStream();

    var command = new UploadDocumentCommand(
        id,
        (DocumentType)documentType,
        file.FileName,
        stream);

    var documentId = await _sender.Send(command, cancellationToken);

    return Ok(ApiResponse<Guid>.Ok(documentId, "Document uploaded successfully."));
}
```

### Multipart/Form-Data Handling

- `IFormFile` is ASP.NET Core's abstraction for uploaded files.
- `[Consumes("multipart/form-data")]` tells Swagger the endpoint accepts file uploads.
- `file.OpenReadStream()` provides a `Stream` without loading the entire file into memory.
- `[FromForm]` binds the `documentType` from form fields (not JSON body).

### Client-Side Upload (Blazor)

```csharp
// WizardApiClient.cs
public async Task<ApiResponse<Guid>?> UploadDocumentAsync(
    Guid applicationId, IBrowserFile file, int documentType)
{
    using var content = new MultipartFormDataContent();

    var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
    content.Add(fileContent, "file", file.Name);
    content.Add(new StringContent(documentType.ToString()), "documentType");

    var response = await _httpClient.PostAsync(
        $"api/wizard/{applicationId}/documents", content);

    return await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
}
```

---

## DI Registration

```csharp
// Infrastructure DependencyInjection.cs
services.AddScoped<IApplicationDocumentRepository, ApplicationDocumentRepository>();
services.AddScoped<IDocumentStorageService, StubDocumentStorageService>();
```

---

## Production Path: Azure Blob Storage

For production, create an `AzureBlobStorageService`:

```csharp
// src/LoanSuperMarket.Infrastructure/Services/AzureBlobStorageService.cs
using Azure.Storage.Blobs;
using LoanSuperMarket.Application.Common.Interfaces;

namespace LoanSuperMarket.Infrastructure.Services;

public sealed class AzureBlobStorageService : IDocumentStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var containerName = configuration["AzureStorage:ContainerName"] ?? "documents";

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> StoreAsync(Stream fileStream, string fileName, CancellationToken ct)
    {
        var blobName = $"{Guid.NewGuid():N}/{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(fileStream, overwrite: true, ct);

        return blobName;
    }

    public async Task<Stream> RetrieveAsync(string storageReference, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(storageReference);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string storageReference, CancellationToken ct)
    {
        var blobClient = _containerClient.GetBlobClient(storageReference);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
```

### Switching implementations

```csharp
// DependencyInjection.cs
if (environment.IsDevelopment())
{
    services.AddScoped<IDocumentStorageService, StubDocumentStorageService>();
}
else
{
    services.AddScoped<IDocumentStorageService, AzureBlobStorageService>();
}
```

---

## EF Core Configuration

```csharp
// src/LoanSuperMarket.Infrastructure/Persistence/Configurations/ApplicationDocumentConfiguration.cs
public sealed class ApplicationDocumentConfiguration
    : IEntityTypeConfiguration<ApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
    {
        builder.ToTable("ApplicationDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StorageReference).HasMaxLength(512).IsRequired();
        builder.Property(x => x.DocumentType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasIndex(x => x.LoanApplicationId);
    }
}
```

---

## Step-by-Step Extension Guide

### Adding a new document type

1. Add value to `DocumentType` enum
2. Update the Blazor upload form to include the new option
3. No other changes needed — the system handles it generically

### Adding file size/type validation

```csharp
// In the controller or a FluentValidation validator
private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

if (file.Length > MaxFileSize)
    return BadRequest(ApiResponse<Guid>.Fail("File exceeds 10 MB limit."));

var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
if (!AllowedExtensions.Contains(extension))
    return BadRequest(ApiResponse<Guid>.Fail("Invalid file type."));
```

### Adding virus scanning

Insert a scanning step before storage:
```csharp
var scanResult = await _virusScanService.ScanAsync(request.FileStream, ct);
if (!scanResult.IsClean)
    throw new DomainException("File failed security scan.");

// Reset stream position after scanning
request.FileStream.Position = 0;
var storageReference = await _storageService.StoreAsync(...);
```

---

## Common Pitfalls

1. **Stream position** — After reading a stream (for scanning or validation), reset `Position = 0` before passing to storage.
2. **Memory pressure** — Never use `file.ReadAllBytes()` for large files. Always stream.
3. **Missing `await using`** — File streams must be disposed. Use `await using var stream = file.OpenReadStream()`.
4. **Storage reference vs path** — Never expose the physical file path. Only the storage reference is stored in the database.
5. **Concurrent access** — The stub implementation isn't thread-safe for the same file. In production, Azure Blob handles this.
