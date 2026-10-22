using MailKit.Net.Smtp;
using MailKit.Security;
using Notifications.Worker.Application.Abstractions;
using Notifications.Worker.Application.Dto;

namespace Notifications.Worker.Infrastructure.Email;

internal sealed class SmtpEmailSender(IConfiguration configuration, NotificationEmailFactory emails) : IEmailSender
{
    public async Task SendAsync(CompletionMessage message, string recipient, CancellationToken token)
    {
        var mail = emails.Create(message, recipient);
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
    }
}
