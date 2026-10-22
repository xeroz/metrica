using Control.Api.Application;
using Control.Api.Application.Abstractions;
using Control.Api.Application.Dto;
using Control.Api.Application.Dto.Responses;
using Control.Api.EndpointConventions;
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
            
        }).ConfigureUpload();

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
        }).ConfigureStatus();
    }
}
