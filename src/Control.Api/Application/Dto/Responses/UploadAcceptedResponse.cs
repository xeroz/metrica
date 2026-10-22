namespace Control.Api.Application.Dto.Responses;

internal sealed record UploadAcceptedResponse(string Message, string Status, Guid UniqueIdentifier);
