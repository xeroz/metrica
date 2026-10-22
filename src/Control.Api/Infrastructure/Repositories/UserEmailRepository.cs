using Control.Api.Application;
using Control.Api.Application.Abstractions;
using Npgsql;

namespace Control.Api.Infrastructure.Repositories;

internal sealed class UserEmailRepository(NpgsqlDataSource dataSource) : IUserEmailRepository
{
    public async Task<string?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT email FROM users WHERE id = $1");
            command.Parameters.AddWithValue(id);
            return (string?)await command.ExecuteScalarAsync(cancellationToken);
        }
        catch (NpgsqlException ex)
        {
            throw new UploadUnavailableException("Database is unavailable.", ex);
        }
    }
}
