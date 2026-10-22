using Control.Api.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Control.Api.Exceptions;

internal sealed class UploadExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UploadUnavailableException) return false;

        await Results.Problem("Upload service is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable)
            .ExecuteAsync(context);
        return true;
    }
}
