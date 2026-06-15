using Telegram.Bot;
using Microsoft.Extensions.Configuration;
using Confuguration.ServicesSending;
using DTOResponseSending;
namespace Confuguration.ServicesSending;

public class TelegramSender : IMessageSender
{
    public string Channel => "Telegram";
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<TelegramSender> _logger;

    public TelegramSender(IConfiguration configuration, ILogger<TelegramSender> logger)
    {
        _botClient = new TelegramBotClient(
            configuration["Telegram:Token"]);
            _logger = logger;
    }

    public async Task<Result<ResponseSender>> SendAsync(string recipient, string content)
    {
        try
        {
            await _botClient.SendMessage(
                chatId: recipient,
                text: content);
            return Result<ResponseSender>.Success(new ResponseSender{Success = true});
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки сообщение телеграмм");
            return Result<ResponseSender>.Failure(ex.Message);
        }
    }
}