using Control.Api.Application;
using Control.Api.Application.Abstractions;
using Control.Api.Application.Filters;
using Control.Api.Application.Validators;
using Control.Api.Infrastructure.Messaging;
using Control.Api.Infrastructure.Persistence;
using Control.Api.Infrastructure.Repositories;
using Control.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Control.Api.Bootstrapping;

internal static class DependencyInjection
{
    internal static IServiceCollection AddControlPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is required.");
        services.AddDbContextFactory<ControlDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IUserEmailRepository, UserEmailRepository>();
        services.AddHttpClient<IFileStorage, SeaweedFileStorage>(client =>
            client.BaseAddress = new Uri(configuration["SeaweedFS:FilerUrl"]
                ?? throw new InvalidOperationException("SeaweedFS:FilerUrl is required.")));
        services.AddSingleton<IStatusFileRepository, StatusFileRepository>();
        services.AddHostedService<OutboxPublisher>();
        return services;
    }

    internal static IServiceCollection AddControlUploads(this IServiceCollection services)
    {
        services.AddScoped<UploadService>();
        services.AddScoped<UploadFileRequestValidator>();
        services.AddScoped<UploadValidationFilter>();
        return services;
    }

    internal static void ConfigureUploadLimits(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<FormOptions>(options =>
            options.MultipartBodyLengthLimit = UploadFileRequestValidator.MaxFileSize + 64 * 1024);
        builder.WebHost.ConfigureKestrel(options =>
            options.Limits.MaxRequestBodySize = UploadFileRequestValidator.MaxFileSize + 64 * 1024);
    }
}
