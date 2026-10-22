namespace Auth.Api.Application;

internal sealed record AuthenticatedUser(long Id, string Email, string PasswordHash);

internal interface IUserRepository
{
    Task<AuthenticatedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<long?> CreateAsync(string email, string passwordHash, CancellationToken cancellationToken);
}

internal interface ITokenIssuer
{
    string Issue(AuthenticatedUser user);
}

internal interface IPasswordService
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}
