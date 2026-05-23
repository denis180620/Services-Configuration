using VkNet;
using VkNet.Model;
using Microsoft.Extensions.Configuration;

namespace Confuguration.ServicesSending;

public class VkSender : IMessageSender
{
    // Идентификатор канала для вашего DI
    public string Channel => "VKontakte";

    private readonly VkApi _vkApi;
    private readonly ILogger<VkSender> _logger;

    public VkSender(IConfiguration configuration, ILogger<VkSender> logger)
    {
        _logger = logger;
        _vkApi = new VkApi();

        // Авторизация через токен сообщества (рекомендуемый способ)
        // Токен нужно получить в настройках вашего сообщества ВК -> "Работа с API"
        var accessToken = configuration["VK:AccessToken"];

        _vkApi.Authorize(new ApiAuthParams
        {
            AccessToken = accessToken
        });
    }

    public async Task<bool> SendAsync(string recipient, string content)
    {
        try
        {
            // Параметр recipient здесь — это числовой идентификатор пользователя (PeerId)
            // ВАЖНО: PeerId для личного сообщения совпадает с UserId получателя.
            if (!long.TryParse(recipient, out long peerId))
            {
                _logger.LogWarning("Некорректный идентификатор получателя: {Recipient}", recipient);
                return false;
            }

            // Формируем параметры отправки
            // RandomId — критически важный параметр для предотвращения дублирования сообщений [citation:2][citation:5]
            var sendParams = new MessagesSendParams
            {
                PeerId = peerId,
                Message = content,
                RandomId = new Random().Next() // Генерируем случайное число
            };

            // Отправляем сообщение
            var messageId = await _vkApi.Messages.SendAsync(sendParams);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке сообщения ВК для {PeerId}", recipient);
            return false;
        }
    }
}