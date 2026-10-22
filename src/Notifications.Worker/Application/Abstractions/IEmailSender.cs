using Notifications.Worker.Application.Dto;

namespace Notifications.Worker.Application.Abstractions;

internal interface IEmailSender
{
    Task SendAsync(CompletionMessage message, string recipient, CancellationToken token);
}
