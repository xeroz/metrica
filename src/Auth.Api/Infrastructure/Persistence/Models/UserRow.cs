namespace Auth.Api.Infrastructure.Persistence.Models;

internal sealed class UserRow
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
