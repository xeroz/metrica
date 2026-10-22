using Control.Api.Application.ReadModels;

namespace Control.Api.Application.Abstractions;

internal interface IStatusFileRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RegisterAsync(Guid identifier, string fileName, string storagePath, string email, long userId,
        CancellationToken cancellationToken);
    Task<StatusFile?> GetAsync(Guid identifier, long userId, CancellationToken cancellationToken);
}
