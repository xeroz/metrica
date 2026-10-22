namespace Notifications.Worker.Application.Dto;

internal sealed record CompletionMessage(long IdCarga, DateTimeOffset FechaFin,
    string Resultado, int Insertados, int Fallidos);
