using Control.Api.Application.Abstractions;
using Control.Api.Application.Exceptions;

namespace Control.Api.Infrastructure.Storage;

internal sealed class SeaweedFileStorage(HttpClient client) : IFileStorage
{
    public async Task SaveAsync(string path, Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            using var response = await client.PutAsync(path, content, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new UploadUnavailableException("File storage is unavailable.", ex);
        }
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await client.DeleteAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
