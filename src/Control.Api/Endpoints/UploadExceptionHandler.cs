using Control.Api.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Control.Api.Endpoints;

internal sealed class UploadExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UploadUnavailableException &&
            !(exception is IOException && HttpMethods.IsPost(context.Request.Method) &&
              context.Request.Path == "/cargas")) return false;

        await Results.Problem("Upload service is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable)
            .ExecuteAsync(context);
        return true;
    }
}
