using MimeKit;
using Notifications.Worker.Application.Dto;

namespace Notifications.Worker.Infrastructure.Email;

internal sealed class NotificationEmailFactory(IConfiguration configuration)
{
    public MimeMessage Create(CompletionMessage message, string email)
    {
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
                   $"Resultado: {message.Resultado}\nInsertados: {message.Insertados}\n" +
                   $"Registros ya existentes: {message.Existentes}\nFallidos: {message.Fallidos}."
        };
        return mail;
    }
}
