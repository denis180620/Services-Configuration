using Confuguration.Dbcontext;
using DTOResponseSending;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Payments;

namespace Confuguration.ServicesSending;

public class MessageSenderFactory
{
    private readonly EmailSender _email;
    private readonly SmsSender _sms;
    private readonly TelegramSender _telegram;
    private readonly ILogger<MessageSenderFactory> _logger;

    public MessageSenderFactory(
        EmailSender email,
        SmsSender sms,
        TelegramSender telegram,
        ILogger<MessageSenderFactory> logger)
    {
        _email = email;
        _sms = sms;
        _telegram = telegram;
        _logger = logger;
    }

    public async Task<Result<ResponseSender>> SendAsync(SentMessage message)
    {
        if (message == null)
            return Result<ResponseSender>.Failure("Сообщение не может быть null");

        if (string.IsNullOrEmpty(message.Channel))
            return Result<ResponseSender>.Failure("Канал отправки не указан");

        try
        {
            _logger.LogInformation("Отправка через канал: {Channel}", message.Channel);

            var sendResult = await (message.Channel?.ToLower() switch
            {
                "email" =>  _email.SendAsync(message.RecipientInfo, message.Content),
                "sms" =>  _sms.SendAsync(message.RecipientInfo, message.Content),
                "telegram" =>  _telegram.SendAsync(message.RecipientInfo, message.Content),
                _ =>  Task.FromResult(Result<ResponseSender>.Failure(errorMessage: "Некорректный канал"))
            });

            if (sendResult.IsSuccess)
            {
                return Result<ResponseSender>.Success(new ResponseSender{Success = true});
            }

            return Result<ResponseSender>.Failure(sendResult.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в фабрике отправки");
            return Result<ResponseSender>.Failure($"Ошибка: {ex.Message}");
        }
    }
}

public class ResponseSender
{
    public bool Success {get; set;}
    public string ErrorMessage {get; set;}
}