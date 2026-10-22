using Metrica.Contracts;

namespace Control.Api.Application;

internal sealed record StatusFile(EstadoCarga Status, string FileName);

internal interface IUserEmailRepository
{
    Task<string?> GetByIdAsync(long id, CancellationToken cancellationToken);
}

internal interface IStatusFileRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RegisterAsync(Guid identifier, string fileName, string storagePath, string email, long userId,
        CancellationToken cancellationToken);
    Task<StatusFile?> GetAsync(Guid identifier, long userId, CancellationToken cancellationToken);
}

internal interface IFileStorage
{
    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken);
    Task DeleteAsync(string path, CancellationToken cancellationToken);
}
