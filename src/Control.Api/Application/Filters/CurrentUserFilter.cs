using Control.Api.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Control.Api.Application.Filters;

internal sealed class CurrentUserFilter : IEndpointFilter
{
    internal const string ItemKey = $"Control.Api.{nameof(CurrentUser)}";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var principal = context.HttpContext.User;
        if (!long.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
            return ValueTask.FromResult<object?>(Results.Unauthorized());

        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email);
        if (string.IsNullOrWhiteSpace(email))
            return ValueTask.FromResult<object?>(Results.Unauthorized());

        context.HttpContext.Items[ItemKey] = new CurrentUser(userId, email);
        return next(context);
    }
}
