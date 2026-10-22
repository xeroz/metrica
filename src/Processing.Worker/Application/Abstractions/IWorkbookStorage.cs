namespace Processing.Worker.Application.Abstractions;

internal interface IWorkbookStorage
{
    Task<Stream> OpenAsync(Guid identifier, CancellationToken cancellationToken);
}
