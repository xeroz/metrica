using System.ComponentModel.DataAnnotations;
using Auth.Api.Application.Dto;

namespace Auth.Api.Application.Validators;

internal sealed class RegisterRequestValidator
{
    private static readonly EmailAddressAttribute EmailAddress = new();

    internal bool IsValid(RegisterRequest request)
    {
        var email = request.Email?.Trim();
        return email is { Length: > 0 and <= 320 }
            && EmailAddress.IsValid(email)
            && request.Password is { Length: >= 8 and <= 128 };
    }
}
