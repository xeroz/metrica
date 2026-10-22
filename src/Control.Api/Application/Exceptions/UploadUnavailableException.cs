namespace Control.Api.Application.Exceptions;

internal sealed class UploadUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
