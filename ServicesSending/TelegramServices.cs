using Telegram.Bot;
using Microsoft.Extensions.Configuration;
namespace Confuguration.ServicesSending;

public class TelegramSender : IMessageSender
{
    public string Channel => "Telegram";
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<TelegramSender> _logger;

    public TelegramSender(IConfiguration configuration, ILogger<TelegramSender> logger)
    {
        _botClient = new TelegramBotClient(
            configuration["Telegram:BotToken"]);
            _logger = logger;
    }

    public async Task<bool> SendAsync(string recipient, string content)
    {
        try
        {
            await _botClient.SendMessage(
                chatId: recipient,
                text: content);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки сообщение телеграмм");
            return false;
        }
    }
}