using Auth.Api.Application;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Auth.Api.Infrastructure;

internal sealed class UserRepository(AuthDbContext database) : IUserRepository
{
    public Task<AuthenticatedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        database.Users.AsNoTracking().Where(user => user.Email == email)
            .Select(user => new AuthenticatedUser(user.Id, user.Email, user.Password))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<long?> CreateAsync(string email, string passwordHash, CancellationToken cancellationToken)
    {
        var user = new UserRow { Email = email, Password = passwordHash };
        database.Users.Add(user);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            database.Entry(user).State = EntityState.Detached;
            return null;
        }
    }
}
