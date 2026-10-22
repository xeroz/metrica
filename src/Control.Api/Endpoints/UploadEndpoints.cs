using Control.Api.Application;
using Control.Api.Application.Dto;
using Control.Api.Application.Filters;
using Microsoft.AspNetCore.Mvc;
using Metrica.Contracts;

namespace Control.Api.Endpoints;

internal static class UploadEndpoints
{
    internal static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/cargas", async ([FromForm] UploadFileRequest uploadRequest, UploadService uploads,
            CancellationToken cancellationToken) =>
        {
            await using var stream = uploadRequest.Field!.OpenReadStream();
            var identifier = await uploads.UploadAsync(stream, uploadRequest.Field.FileName,
                uploadRequest.Email!, cancellationToken);
            return Results.Ok(new UploadAcceptedResponse(
                "Processing file", EstadoCarga.Pending.ToString(), identifier));
            
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
            Guid identifier, UploadService uploads, CancellationToken cancellationToken) =>
        {
            var file = await uploads.GetStatusAsync(identifier, cancellationToken);
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
