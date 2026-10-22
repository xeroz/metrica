using Auth.Api.Application;
using Auth.Api.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Infrastructure;

internal sealed class PasswordService(IPasswordHasher<UserRow> hasher) : IPasswordService
{
    public string Hash(string password) => hasher.HashPassword(new UserRow(), password);

    public bool Verify(string passwordHash, string password) =>
        hasher.VerifyHashedPassword(new UserRow(), passwordHash, password) != PasswordVerificationResult.Failed;
}
