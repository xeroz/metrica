using Metrica.Shared;

namespace Processing.Worker.Application.Abstractions;

internal interface ILoadRepository
{
    Task<LoadStatus?> StartAsync(long loadId, Guid identifier, CancellationToken token);
    Task SaveRowsAsync(long loadId, WorkbookRows rows, CancellationToken token);
    Task RejectAsync(long loadId, string reason, CancellationToken token,
        IReadOnlyList<RowFailure>? rowFailures = null);
    Task FinishAsync(long loadId, string? outcome, CancellationToken token);
}
