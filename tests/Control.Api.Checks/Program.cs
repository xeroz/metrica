using System.IO.Compression;
using Control.Api.Application;
using Control.Api.Application.Dto;
using Control.Api.Application.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

static IFormFile File(byte[] content, string name) =>
    new FormFile(new MemoryStream(content), 0, content.Length, "field", name);

using var validBytes = new MemoryStream();
using (var zip = new ZipArchive(validBytes, ZipArchiveMode.Create, leaveOpen: true))
{
    zip.CreateEntry("[Content_Types].xml");
    zip.CreateEntry("xl/workbook.xml");
}

const string email = "usuario@example.com";
var valid = File(validBytes.ToArray(), "sample.xlsx");
var validator = new UploadFileRequestValidator();
if (!validator.Validate(new UploadFileRequest { Field = valid, Email = email }).IsValid) throw new Exception("Valid Excel container rejected.");
if (validator.Validate(new UploadFileRequest { Field = valid, Email = "invalid" }).IsValid)
    throw new Exception("Invalid email accepted.");
if (validator.Validate(new UploadFileRequest { Field = File(validBytes.ToArray(), "sample.txt"), Email = email }).IsValid)
    throw new Exception("Wrong extension accepted.");
if (validator.Validate(new UploadFileRequest { Field = File([1, 2, 3], "sample.xlsx"), Email = email }).IsValid)
    throw new Exception("Invalid Excel container accepted.");
if (validator.Validate(new UploadFileRequest { Field = File(new byte[UploadFileRequestValidator.MaxFileSize + 1], "large.xlsx"), Email = email })
    .Errors[0].ErrorCode != UploadFileRequestValidator.FileTooLargeCode)
    throw new Exception("Oversized file accepted.");
if (validator.Validate(new UploadFileRequest()).IsValid)
    throw new Exception("Missing file accepted.");

var wrongContentType = new DefaultHttpContext();
wrongContentType.Request.ContentType = "application/x-www-form-urlencoded";
var (_, contentTypeValidation) = await validator.ValidateRequestAsync(wrongContentType.Request, CancellationToken.None);
if (contentTypeValidation.Errors[0].ErrorMessage != "Expected multipart/form-data.")
    throw new Exception("Non-multipart request accepted.");

var invalidMultipart = new DefaultHttpContext();
invalidMultipart.Request.ContentType = "multipart/form-data";
var (_, multipartValidation) = await validator.ValidateRequestAsync(invalidMultipart.Request, CancellationToken.None);
if (multipartValidation.Errors[0].ErrorMessage != "Invalid multipart form.")
    throw new Exception("Malformed multipart request accepted.");

var multipart = new DefaultHttpContext();
multipart.Request.ContentType = "multipart/form-data; boundary=test";
multipart.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
    { ["email"] = email }, new FormFileCollection { valid });
var (parsedRequest, parsedValidation) = await validator.ValidateRequestAsync(multipart.Request, CancellationToken.None);
if (!parsedValidation.IsValid || parsedRequest.Field != valid || parsedRequest.Email != email)
    throw new Exception("Valid multipart file was not extracted.");

multipart.Request.Form = new FormCollection(null, new FormFileCollection());
var (_, missingFieldValidation) = await validator.ValidateRequestAsync(multipart.Request, CancellationToken.None);
if (missingFieldValidation.IsValid)
    throw new Exception("Multipart request without field accepted.");

if (args.Length > 0)
{
    var sample = new FileInfo(args[0]);
    await using var sampleStream = sample.OpenRead();
    var realFile = new FormFile(sampleStream, 0, sample.Length, "field", sample.Name);
    if (!validator.Validate(new UploadFileRequest { Field = realFile, Email = email }).IsValid)
        throw new Exception("Provided sample was rejected.");
}

var storage = new FakeStorage();
var repository = new FakeRepository();
var uploads = new UploadService(storage, repository, NullLogger<UploadService>.Instance);
var identifier = await uploads.UploadAsync(valid.OpenReadStream(), valid.FileName, email, CancellationToken.None);
if (repository.Identifier != identifier || repository.Email != email || !storage.Saved || storage.Deleted)
    throw new Exception("Accepted upload was not stored and registered.");

repository.Fail = true;
try { await uploads.UploadAsync(valid.OpenReadStream(), valid.FileName, email, CancellationToken.None); throw new Exception("Registration failure was ignored."); }
catch (IOException) { }
if (!storage.Deleted) throw new Exception("Orphan upload was not removed.");

Console.WriteLine("Control upload checks passed.");

sealed class FakeStorage : IFileStorage
{
    public bool Saved { get; private set; }
    public bool Deleted { get; private set; }
    public Task SaveAsync(string path, Stream content, CancellationToken cancellationToken)
    {
        Saved = true;
        return Task.CompletedTask;
    }
    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        Deleted = true;
        return Task.CompletedTask;
    }
}

sealed class FakeRepository : IStatusFileRepository
{
    public Guid Identifier { get; private set; }
    public string? Email { get; private set; }
    public bool Fail { get; set; }
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RegisterAsync(Guid identifier, string fileName, string storagePath, string email, CancellationToken cancellationToken)
    {
        if (Fail) throw new IOException("Simulated database failure.");
        Identifier = identifier;
        Email = email;
        return Task.CompletedTask;
    }
    public Task<StatusFile?> GetAsync(Guid identifier, CancellationToken cancellationToken) => Task.FromResult<StatusFile?>(null);
}
