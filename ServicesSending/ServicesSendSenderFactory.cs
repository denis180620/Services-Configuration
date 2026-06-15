using Confuguration.Dbcontext;
using DTOResponseSending;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Payments;

namespace Confuguration.ServicesSending;

public class MessageDispatcher
{
    private readonly Dictionary<string, IMessageSender> _senders;
    private readonly ILogger<MessageDispatcher> _logger;

    public MessageDispatcher(IEnumerable<IMessageSender> sender, ILogger<MessageDispatcher> logger)
    {
        _logger = logger;
        _senders = sender.ToDictionary(s => s.Channel, StringComparer.OrdinalIgnoreCase);
    }
    public async Task<Result<ResponseSender>> SendAsync(string channel, string RecipientInfo, string Content)
    {
        if (channel == null || RecipientInfo == null || Content == null)
            return Result<ResponseSender>.Failure("Сообщение не может быть null");

        if (string.IsNullOrEmpty(channel))
            return Result<ResponseSender>.Failure("Канал отправки не указан");

        if (!_senders.TryGetValue(channel, out var sender))
            return Result<ResponseSender>.Failure($"Неизвестный канал: {channel}");

        _logger.LogInformation("Отправка через канал: {Channel}", channel);
        return await sender.SendAsync(RecipientInfo, Content);
    }
}

public class ResponseSender
{
    public bool Success {get; set;}
    public string ErrorMessage {get; set;}
}