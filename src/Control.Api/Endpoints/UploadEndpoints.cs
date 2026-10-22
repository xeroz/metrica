using Control.Api.Application;
using Control.Api.Application.Dto;
using Control.Api.Application.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Metrica.Contracts;
using Npgsql;

namespace Control.Api.Endpoints;

internal static class UploadEndpoints
{
    internal static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/cargas", async ([FromForm] UploadFileRequest uploadRequest, UploadService uploads,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await using var stream = uploadRequest.Field!.OpenReadStream();
                var identifier = await uploads.UploadAsync(stream, uploadRequest.Field.FileName,
                    uploadRequest.Email!, cancellationToken);
                return Results.Ok(new UploadAcceptedResponse(
                    "Processing file", EstadoCarga.Pending.ToString(), identifier));
            }
            catch (Exception ex) when (ex is HttpRequestException or NpgsqlException or DbUpdateException or IOException)
            {
                return Results.Problem("Upload service is unavailable.", statusCode: 503);
            }
        }).AddEndpointFilter<UploadValidationFilter>().DisableAntiforgery()
          .Accepts<UploadFileRequest>("multipart/form-data")
          .Produces<UploadAcceptedResponse>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status400BadRequest)
          .Produces(StatusCodes.Status413PayloadTooLarge)
          .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
          .WithTags("Cargas")
          .WithSummary("Subir un archivo Excel")
          .WithDescription("Recibe un .xlsx de hasta 5 MB en field y el correo de notificación en email.");

        app.MapGet("/cargas/{identifier:guid}", async (
            Guid identifier, IStatusFileRepository files, CancellationToken cancellationToken) =>
        {
            var file = await files.GetAsync(identifier, cancellationToken);
            return file is null ? Results.NotFound() : Results.Ok(new UploadStatusResponse(
                identifier, file.Status.ToString(), file.FileName));
        }).Produces<UploadStatusResponse>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status404NotFound)
          .WithTags("Cargas")
          .WithSummary("Consultar el estado de una carga");
    }
}

internal sealed record UploadAcceptedResponse(string Message, string Status, Guid UniqueIdentifier);
internal sealed record UploadStatusResponse(Guid UniqueIdentifier, string Status, string FileName);
