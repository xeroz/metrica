namespace Auth.Api.Application;

internal sealed class AuthenticationService(IUserRepository users, IPasswordService passwords, ITokenIssuer tokens)
{
    internal async Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !passwords.Verify(user.PasswordHash, password))
            return null;
        return tokens.Issue(user);
    }
}
