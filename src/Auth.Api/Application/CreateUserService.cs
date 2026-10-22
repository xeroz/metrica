namespace Auth.Api.Application;

internal sealed record CreatedUser(long Id, string Email);

internal sealed class CreateUserService(IUserRepository users, IPasswordService passwords)
{
    internal async Task<CreatedUser?> CreateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var id = await users.CreateAsync(normalizedEmail, passwords.Hash(password), cancellationToken);
        return id is null ? null : new CreatedUser(id.Value, normalizedEmail);
    }
}
