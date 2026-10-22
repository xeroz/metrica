namespace Control.Api.Application;

internal sealed class UploadUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
