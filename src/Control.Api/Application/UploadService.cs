using Control.Api.Application.Abstractions;
using Control.Api.Application.ReadModels;
using Microsoft.Extensions.Logging;

namespace Control.Api.Application;

internal sealed class UploadService(IFileStorage storage, IStatusFileRepository files, ILogger<UploadService> logger)
{
    internal Task<StatusFile?> GetStatusAsync(Guid identifier, long userId, CancellationToken cancellationToken) =>
        files.GetAsync(identifier, userId, cancellationToken);

    internal async Task<Guid> UploadAsync(Stream content, string fileName, string email, long userId,
        CancellationToken cancellationToken)
    {
        var identifier = Guid.NewGuid();
        var storagePath = $"/uploads/{identifier:N}.xlsx";
        await storage.SaveAsync(storagePath, content, cancellationToken);

        try
        {
            await files.RegisterAsync(identifier, Path.GetFileName(fileName), storagePath, email, userId,
                cancellationToken);
        }
        catch (Exception registrationError)
        {
            logger.LogError(registrationError, "Could not register uploaded file {Identifier}", identifier);
            try { await storage.DeleteAsync(storagePath, cancellationToken); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not remove orphan file {Identifier}", identifier); }
            throw;
        }
        return identifier;
    }
}
