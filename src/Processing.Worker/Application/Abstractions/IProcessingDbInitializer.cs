namespace Processing.Worker.Application.Abstractions;

internal interface IProcessingDbInitializer
{
    Task InitializeAsync(CancellationToken token);
}
