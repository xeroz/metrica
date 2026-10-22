using Control.Api.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Control.Api.Application.Filters;

internal sealed class CurrentUserFilter(IUserEmailRepository userEmails) : IEndpointFilter
{
    internal const string ItemKey = $"Control.Api.{nameof(CurrentUser)}";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!long.TryParse(context.HttpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return Results.Unauthorized();

        var email = await userEmails.GetByIdAsync(userId, context.HttpContext.RequestAborted);
        if (email is null) return Results.Unauthorized();

        context.HttpContext.Items[ItemKey] = new CurrentUser(userId, email);
        return await next(context);
    }
}
