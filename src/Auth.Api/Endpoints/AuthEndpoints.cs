using Auth.Api.Application;
using Auth.Api.Application.Dto;
using Auth.Api.Application.Validators;

namespace Auth.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, AuthenticationService authentication,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest();
            var token = await authentication.LoginAsync(request.Email, request.Password, cancellationToken);
            return token is null ? Results.Unauthorized() : Results.Ok(new LoginResponse(token, "Bearer", 3600));
        }).Produces<LoginResponse>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status400BadRequest)
          .Produces(StatusCodes.Status401Unauthorized)
          .WithTags("Auth")
          .WithSummary("Iniciar sesión con email y contraseña");

        app.MapPost("/auth/register", async (RegisterRequest request, RegisterRequestValidator validator,
            CreateUserService users, CancellationToken cancellationToken) =>
        {
            if (!validator.IsValid(request)) return Results.BadRequest();
            var user = await users.CreateAsync(request.Email, request.Password, cancellationToken);
            return user is null
                ? Results.Conflict()
                : Results.Json(new RegisterResponse(user.Id, user.Email), statusCode: StatusCodes.Status201Created);
        }).Produces<RegisterResponse>(StatusCodes.Status201Created)
          .Produces(StatusCodes.Status400BadRequest)
          .Produces(StatusCodes.Status409Conflict)
          .WithTags("Auth")
          .WithSummary("Registrar usuario con email y contraseña")
          .WithDescription("El email debe ser válido y la contraseña debe tener entre 8 y 128 caracteres.");
    }
}

internal sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);
internal sealed record RegisterResponse(long Id, string Email);
