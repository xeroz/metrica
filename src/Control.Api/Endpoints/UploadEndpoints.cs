using Control.Api.Application;
using Control.Api.Application.Abstractions;
using Control.Api.Application.Dto;
using Control.Api.Application.Filters;
using Microsoft.AspNetCore.Mvc;
using Metrica.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Control.Api.Endpoints;

internal static class UploadEndpoints
{
    internal static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/cargas", async ([FromForm] UploadFileRequest uploadRequest, ClaimsPrincipal user,
            IUserEmailRepository userEmails, UploadService uploads,
            CancellationToken cancellationToken) =>
        {
            if (!long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
                return Results.Unauthorized();
            var email = await userEmails.GetByIdAsync(userId, cancellationToken);
            if (email is null) return Results.Unauthorized();

            await using var stream = uploadRequest.Field!.OpenReadStream();
            var identifier = await uploads.UploadAsync(stream, uploadRequest.Field.FileName,
                email, userId, cancellationToken);
            return Results.Ok(new UploadAcceptedResponse(
                "Processing file", EstadoCarga.Pending.ToString(), identifier));
            
        }).RequireAuthorization().AddEndpointFilter<UploadValidationFilter>().DisableAntiforgery()
          .Accepts<UploadFileRequest>("multipart/form-data")
          .Produces<UploadAcceptedResponse>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status400BadRequest)
          .Produces(StatusCodes.Status401Unauthorized)
          .Produces(StatusCodes.Status413PayloadTooLarge)
          .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
          .WithTags("Cargas")
          .WithSummary("Subir un archivo Excel")
          .WithDescription("Recibe un .xlsx de hasta 5 MB en field. El correo de notificación se obtiene del usuario autenticado.");

        app.MapGet("/cargas/{identifier:guid}", async (
            Guid identifier, ClaimsPrincipal user, IUserEmailRepository userEmails,
            UploadService uploads, CancellationToken cancellationToken) =>
        {
            if (!long.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
                return Results.Unauthorized();
            if (await userEmails.GetByIdAsync(userId, cancellationToken) is null)
                return Results.Unauthorized();

            var file = await uploads.GetStatusAsync(identifier, userId, cancellationToken);
            return file is null ? Results.NotFound() : Results.Ok(new UploadStatusResponse(
                identifier, file.Status.ToString(), file.FileName));
        }).RequireAuthorization()
          .Produces<UploadStatusResponse>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status401Unauthorized)
          .Produces(StatusCodes.Status404NotFound)
          .WithTags("Cargas")
          .WithSummary("Consultar el estado de una carga");
    }
}

internal sealed record UploadAcceptedResponse(string Message, string Status, Guid UniqueIdentifier);
internal sealed record UploadStatusResponse(Guid UniqueIdentifier, string Status, string FileName);
