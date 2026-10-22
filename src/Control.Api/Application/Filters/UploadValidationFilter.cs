using Control.Api.Application.Dto;
using Control.Api.Application.Validators;
using Microsoft.AspNetCore.Http;

namespace Control.Api.Application.Filters;

internal sealed class UploadValidationFilter(UploadFileRequestValidator validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.GetArgument<UploadFileRequest>(0);
        var validation = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (validation.IsValid) return await next(context);

        var error = validation.Errors[0];
        var statusCode = error.ErrorCode == UploadFileRequestValidator.FileTooLargeCode
            ? StatusCodes.Status413PayloadTooLarge : StatusCodes.Status400BadRequest;
        return Results.Json(new { message = error.ErrorMessage }, statusCode: statusCode);
    }
}
