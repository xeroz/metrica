using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Metrica.Shared;
using Npgsql;
using RabbitMQ.Client;

namespace Notifications.Worker;

internal sealed record CompletionMessage(long IdCarga, DateTimeOffset FechaFin,
    string Resultado, int Insertados, int Fallidos);

internal sealed class NotificationConsumer(IConfiguration configuration,
    ILogger<NotificationConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ConsumeAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Notification consumer unavailable; reconnecting");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken token)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = configuration["RabbitMQ:User"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };
        await using var rabbit = await factory.CreateConnectionAsync(token);
        await using var channel = await rabbit.CreateChannelAsync(cancellationToken: token);
        await channel.QueueDeclareAsync("notificaciones", durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: token);
        while (!token.IsCancellationRequested)
        {
            var delivery = await channel.BasicGetAsync("notificaciones", autoAck: false, token);
            if (delivery is null)
            {
                await Task.Delay(500, token);
                continue;
            }

            CompletionMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<CompletionMessage>(delivery.Body.Span,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (message is null || message.IdCarga <= 0 || string.IsNullOrWhiteSpace(message.Resultado) ||
                    message.Insertados < 0 || message.Fallidos < 0)
                    throw new JsonException("Invalid completion message.");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid notification message rejected");
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
                continue;
            }

            try
            {
                await NotifyAsync(message, token);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, token);
            }
            catch (InvalidDataException ex)
            {
                logger.LogError(ex, "Notification for load {LoadId} rejected", message.IdCarga);
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Notification for load {LoadId} failed; retrying", message.IdCarga);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, token);
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
    }

    private async Task NotifyAsync(CompletionMessage message, CancellationToken token)
    {
        await using var database = new NpgsqlConnection(configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database is required."));
        await database.OpenAsync(token);
        await using var lookup = new NpgsqlCommand(
            "SELECT status, email FROM \"StatusFile\" WHERE id = @id", database);
        lookup.Parameters.AddWithValue("id", message.IdCarga);
        await using var reader = await lookup.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token))
            throw new InvalidDataException($"Load {message.IdCarga} does not exist.");
        var status = (LoadStatus)reader.GetInt16(0);
        var email = reader.IsDBNull(1) ? null : reader.GetString(1);
        await reader.CloseAsync();
        if (status == LoadStatus.Notified) return;
        if (status != LoadStatus.Completed)
            throw new InvalidDataException($"Load {message.IdCarga} is not completed.");
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidDataException($"Load {message.IdCarga} has no recipient email.");
        if (!MailboxAddress.TryParse(email, out var recipient))
            throw new InvalidDataException($"Load {message.IdCarga} has an invalid recipient email.");

        var mail = new MimeMessage();
        mail.From.Add(MailboxAddress.Parse(configuration["Smtp:From"]
            ?? throw new InvalidOperationException("Smtp:From is required.")));
        mail.To.Add(recipient);
        mail.Subject = $"Carga {message.IdCarga} finalizada: {message.Resultado}";
        mail.Body = new TextPart("plain")
        {
            Text = $"Tu carga {message.IdCarga} finalizó el {message.FechaFin:yyyy-MM-dd HH:mm:ss} UTC.\n" +
                   $"Resultado: {message.Resultado}\nInsertados: {message.Insertados}\nFallidos: {message.Fallidos}."
        };

        using var smtp = new SmtpClient();
        var security = Enum.Parse<SecureSocketOptions>(configuration["Smtp:Security"] ?? "StartTls", true);
        await smtp.ConnectAsync(configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("Smtp:Host is required."),
            configuration.GetValue("Smtp:Port", 25), security, token);
        var smtpUser = configuration["Smtp:User"];
        if (!string.IsNullOrWhiteSpace(smtpUser))
            await smtp.AuthenticateAsync(smtpUser, configuration["Smtp:Password"]
                ?? throw new InvalidOperationException("Smtp:Password is required when Smtp:User is set."), token);
        await smtp.SendAsync(mail, token);

        await using var update = new NpgsqlCommand(
            "UPDATE \"StatusFile\" SET status = @notified WHERE id = @id AND status = @completed", database);
        update.Parameters.AddWithValue("notified", (short)LoadStatus.Notified);
        update.Parameters.AddWithValue("completed", (short)LoadStatus.Completed);
        update.Parameters.AddWithValue("id", message.IdCarga);
        if (await update.ExecuteNonQueryAsync(token) != 1)
            throw new InvalidOperationException($"Load {message.IdCarga} changed state during notification.");
        logger.LogInformation("Email sent for load {LoadId}", message.IdCarga);
    }
}
