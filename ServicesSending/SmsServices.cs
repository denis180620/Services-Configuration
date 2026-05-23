using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;

namespace Confuguration.ServicesSending;

public class SmsSender : IMessageSender
{
    public string Channel => "SMS";
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsSender> _logger;
    private readonly string _fromNumber;

    public SmsSender(IConfiguration configuration, ILogger<SmsSender> logger)
    {
        _configuration = configuration;
        TwilioClient.Init(
            _configuration["Twilio:AccountSid"],
            _configuration["Twilio:AuthToken"]);
        _logger = logger;
        _fromNumber = configuration["Twilio:FromNumber"];;
    }

    public async Task<bool> SendAsync(string recipient, string content)
    {
        try
        {
            var message = await MessageResource.CreateAsync(
                body: content,
                from: new Twilio.Types.PhoneNumber(_fromNumber),
                to: new Twilio.Types.PhoneNumber(recipient));

            return message.Status != MessageResource.StatusEnum.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки сообщения");
            return false;
        }
    }
}