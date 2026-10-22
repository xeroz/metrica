using System.IO.Compression;
using Control.Api.Application.Dto;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace Control.Api.Application.Validators;

internal sealed class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    internal const long MaxFileSize = 5 * 1024 * 1024;
    internal const string FileTooLargeCode = "FileTooLarge";

    public UploadFileRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Field)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("The field must contain a non-empty Excel file.")
            .Must(file => file!.Length > 0).WithMessage("The field must contain a non-empty Excel file.")
            .Must(file => file!.Length <= MaxFileSize).WithMessage("File exceeds 5 MB.").WithErrorCode(FileTooLargeCode)
            .Must(file => string.Equals(Path.GetExtension(file!.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Only .xlsx files are accepted.")
            .Must(BeExcelFile).WithMessage("Invalid .xlsx file.");
    }

    public async Task<(UploadFileRequest Request, ValidationResult Validation)> ValidateRequestAsync(
        HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType ||
            request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) != true)
            return (new UploadFileRequest(), new ValidationResult([
                new ValidationFailure(nameof(UploadFileRequest.Field), "Expected multipart/form-data.")
            ]));

        IFormCollection form;
        try { form = await request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException)
        {
            return (new UploadFileRequest(), new ValidationResult([
                new ValidationFailure(nameof(UploadFileRequest.Field), "Invalid multipart form.")
            ]));
        }

        var uploadRequest = new UploadFileRequest
        {
            Field = form.Files.GetFile("field"),
            Email = form["email"].ToString().Trim()
        };
        return (uploadRequest, await ValidateAsync(uploadRequest, cancellationToken));
    }

    private static bool BeExcelFile(IFormFile? file)
    {
        try
        {
            using var archive = new ZipArchive(file!.OpenReadStream(), ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") is not null
                && archive.GetEntry("xl/workbook.xml") is not null;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            return false;
        }
    }
}
