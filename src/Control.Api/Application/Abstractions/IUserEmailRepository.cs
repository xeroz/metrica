namespace Control.Api.Application.Abstractions;

internal interface IUserEmailRepository
{
    Task<string?> GetByIdAsync(long id, CancellationToken cancellationToken);
}
