using LoanSuperMarket.Domain.Enums;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.UploadDocument;

public sealed record UploadDocumentCommand(
    Guid ApplicationId,
    DocumentType DocumentType,
    string FileName,
    Stream FileStream) : IRequest<Guid>;
