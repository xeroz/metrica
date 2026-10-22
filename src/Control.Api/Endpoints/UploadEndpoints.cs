using Control.Api.Application;
using Control.Api.Application.Abstractions;
using Control.Api.Application.Dto;
using Control.Api.Application.Dto.Responses;
using Control.Api.Application.Filters;
using Control.Api.EndpointConventions;
using Microsoft.AspNetCore.Mvc;
using Metrica.Shared;

namespace Control.Api.Endpoints;

internal static class UploadEndpoints
{
    internal static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/cargas", async ([FromForm] UploadFileRequest uploadRequest, HttpContext http,
            UploadService uploads, CancellationToken cancellationToken) =>
        {
            var user = (CurrentUser)http.Items[CurrentUserFilter.ItemKey]!;
            await using var stream = uploadRequest.Field!.OpenReadStream();
            var identifier = await uploads.UploadAsync(stream, uploadRequest.Field.FileName,
                user.Email, user.UserId, cancellationToken);
            return Results.Ok(new UploadAcceptedResponse(
                "Processing file", LoadStatus.Pending.ToString(), identifier));

        }).ConfigureUpload();

        app.MapGet("/cargas/{identifier:guid}", async (
            Guid identifier, HttpContext http, UploadService uploads,
            CancellationToken cancellationToken) =>
        {
            var user = (CurrentUser)http.Items[CurrentUserFilter.ItemKey]!;
            var file = await uploads.GetStatusAsync(identifier, user.UserId, cancellationToken);
            return file is null ? Results.NotFound() : Results.Ok(new UploadStatusResponse(
                identifier, file.Status.ToString(), file.FileName));
        }).ConfigureStatus();
    }
}
