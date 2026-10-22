using System.Text.Json;
using Metrica.Shared;
using RabbitMQ.Client;

namespace Processing.Worker;

internal sealed record LoadMessage(long IdCarga, Guid UniqueIdentifier, string RutaArchivo);

internal sealed class ProcessingConsumer(
    LoadRepository repository,
    WorkbookStorage storage,
    IConfiguration configuration,
    ILogger<ProcessingConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await repository.InitializeAsync(stoppingToken);
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Processing consumer unavailable; reconnecting");
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
        await channel.QueueDeclareAsync("carga_masiva", durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: token);
        while (!token.IsCancellationRequested)
        {
            var delivery = await channel.BasicGetAsync("carga_masiva", autoAck: false, token);
            if (delivery is null)
            {
                await Task.Delay(500, token);
                continue;
            }

            LoadMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<LoadMessage>(delivery.Body.Span,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (message is null || message.IdCarga <= 0 || message.UniqueIdentifier == Guid.Empty ||
                    message.RutaArchivo != $"/uploads/{message.UniqueIdentifier:N}.xlsx")
                    throw new JsonException("Invalid load message.");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid load message rejected");
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, token);
                continue;
            }

            try
            {
                await ProcessAsync(message, token);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Load {LoadId} failed; message will be retried", message.IdCarga);
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, token);
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
    }

    private async Task ProcessAsync(LoadMessage message, CancellationToken token)
    {
        var status = await repository.StartAsync(message.IdCarga, message.UniqueIdentifier, token);
        if (status is null)
        {
            logger.LogWarning("Ignoring load {LoadId}: no matching StatusFile", message.IdCarga);
            return;
        }
        if (status is LoadStatus.Completed or LoadStatus.Notified) return;
        if (status == LoadStatus.Loaded)
        {
            await repository.FinishAsync(message.IdCarga, null, token);
            return;
        }

        WorkbookRows rows;
        try
        {
            await using var workbook = await storage.OpenAsync(message.UniqueIdentifier, token);
            rows = WorkbookReader.Read(workbook);
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException
                                   or InvalidOperationException or FormatException)
        {
            await repository.RejectAsync(message.IdCarga, $"Excel inválido: {ex.Message}", token);
            return;
        }

        if (rows.Products.Count == 0)
        {
            await repository.RejectAsync(message.IdCarga, "El Excel no contiene productos válidos.",
                token, rows.Failures);
            return;
        }
        await repository.SaveRowsAsync(message.IdCarga, rows, token);
        await repository.FinishAsync(message.IdCarga, null, token);
        logger.LogInformation("Load {LoadId} finished", message.IdCarga);
    }
}
