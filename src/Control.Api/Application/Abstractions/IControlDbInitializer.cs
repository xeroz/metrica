namespace Control.Api.Application.Abstractions;

internal interface IControlDbInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
