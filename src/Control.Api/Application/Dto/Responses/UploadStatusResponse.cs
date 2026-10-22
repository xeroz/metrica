namespace Control.Api.Application.Dto.Responses;

internal sealed record UploadStatusResponse(Guid UniqueIdentifier, string Status, string FileName);
