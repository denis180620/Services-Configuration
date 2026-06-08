using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmsAero; // Пространство имён для SmsAeroClient
using DTOResponseSending;

namespace Confuguration.ServicesSending;

public class SmsSender : IMessageSender
{
    public string Channel => "SMS";
    private readonly SmsAeroClient _client;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IConfiguration configuration, ILogger<SmsSender> logger)
    {
        // Читаем учётные данные из конфигурации
        var email = configuration["SmsAero:Email"];
        var apiKey = configuration["SmsAero:ApiKey"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "SmsAero credentials are missing. " +
                "Please set SmsAero:Email and SmsAero:ApiKey in configuration.");
        }

        // Инициализируем клиент SmsAero
        _client = new SmsAeroClient(email, apiKey);
        _logger = logger;
    }

    public async Task<Result<ResponseSender>> SendAsync(string recipient, string content)
    {
        try
        {
            // Отправляем SMS: метод ожидает номер и текст сообщения
            string response = await _client.SmsSend(content, recipient);

            if(response != null){
            return Result<ResponseSender>.Success(new  ResponseSender {Success = true});
            }
            else
            {
                return Result<ResponseSender>.Failure(errorMessage: "Ошибка оправки сообщения");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки SMS через SmsAero");
            return Result<ResponseSender>.Failure(ex.Message);
        }
    }
}