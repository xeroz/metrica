namespace Control.Api.Endpoints;

internal sealed record UploadAcceptedResponse(string Message, string Status, Guid UniqueIdentifier);
internal sealed record UploadStatusResponse(Guid UniqueIdentifier, string Status, string FileName);
