using Microsoft.AspNetCore.Http;

namespace Control.Api.Application.Dto;

public sealed class UploadFileRequest
{
    public IFormFile? Field { get; set; }
    public string? Email { get; set; }
}
