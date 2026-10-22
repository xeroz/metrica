using Control.Api.Application.Dto;
using Control.Api.Application.Filters;
using Control.Api.Application.Dto.Responses;

namespace Control.Api.EndpointConventions;

internal static class UploadEndpointConventions
{
    internal static void ConfigureUpload(this RouteHandlerBuilder endpoint)
    {
        endpoint.RequireAuthorization().AddEndpointFilter<UploadValidationFilter>().DisableAntiforgery()
            .Accepts<UploadFileRequest>("multipart/form-data")
            .Produces<UploadAcceptedResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithTags("Cargas")
            .WithSummary("Subir un archivo Excel")
            .WithDescription("Recibe un .xlsx de hasta 5 MB en field. El correo de notificación se obtiene del usuario autenticado.");
    }

    internal static void ConfigureStatus(this RouteHandlerBuilder endpoint)
    {
        endpoint.RequireAuthorization()
            .Produces<UploadStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithTags("Cargas")
            .WithSummary("Consultar el estado de una carga");
    }
}
