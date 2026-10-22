using Microsoft.OpenApi;

namespace Control.Api.Bootstrapping;

internal static class OpenApiConfigurations
{
    internal static IServiceCollection AddControlOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            foreach (var server in document.Servers ?? [])
                server.Url = server.Url?.TrimEnd('/');

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };
            foreach (var path in document.Paths.Where(path => path.Key.StartsWith("/cargas", StringComparison.Ordinal)))
            {
                if (path.Value.Operations is not { } operations) continue;
                foreach (var operation in operations.Values)
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                        }
                    ];
            }
            return Task.CompletedTask;
        }));
        return services;
    }
}
