namespace Control.Api.Application.Abstractions;

internal interface IFileStorage
{
    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken);
    Task DeleteAsync(string path, CancellationToken cancellationToken);
}
