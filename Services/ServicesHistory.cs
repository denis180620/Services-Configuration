using Confuguration.Repository;
using Confuguration.Dbcontext;
using Confuguration.ServicesSending;
using DTOResponseSending;
using Configuration.DTOs;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Confuguration.Services;

public class ServicesHistory
{
    private readonly IUserHistoryRepository _repository;
    private readonly IMessageSender _message;
    private readonly ILogger<ServicesHistory> _logger;
    private readonly MessageSenderFactory _factory;

    public ServicesHistory(IUserHistoryRepository repository, IMessageSender message, ILogger<ServicesHistory> logger, MessageSenderFactory factory)
    {
        _repository = repository;
        _message = message;
        _logger = logger;
        _factory = factory;
    }

    /// <summary>
    /// Создание и отправка сообщения
    /// </summary>
    public async Task<Result<SentMessage>> HistoryMessage(SentMessage message)
    {
        if (message == null)
        {
            return Result<SentMessage>.Failure("Сообщение не может быть null");
        }

        if (message.UserId == Guid.Empty)
        {
            return Result<SentMessage>.Failure("UserId не может быть пустым");
        }

        if (string.IsNullOrEmpty(message.RecipientInfo))
        {
            return Result<SentMessage>.Failure("Получатель не указан");
        }

        if (string.IsNullOrEmpty(message.Content))
        {
            return Result<SentMessage>.Failure("Содержимое сообщения не может быть пустым");
        }

        // Устанавливаем начальный статус
        message.Status = "Pending";
        message.SentAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        // Сохраняем запись в истории до отправки
        await _repository.HistoryMessage(message);
        var saveResult = await _repository.SaveChangesAsync();

        if (!saveResult)
        {
            _logger.LogError("Не удалось сохранить запись истории для сообщения получателю {Recipient}", message.RecipientInfo);
            return Result<SentMessage>.Failure("Не удалось сохранить историю сообщения");
        }

        _logger.LogInformation("Создана запись истории с ID {MessageId} для получателя {Recipient}", message.Id, message.RecipientInfo);

        // Отправляем сообщение
        try
        {
            // Обновляем статус на "Sending"
            message.Status = "Sending";
            message.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            var sendResult = await _factory.SendAsync(message);

            // Обновляем статус на основе результата отправки
            if (sendResult.IsSuccess && sendResult.Data?.Success == true)
            {
                message.Status = "Sent";
                _logger.LogInformation("Сообщение {MessageId} успешно отправлено получателю {Recipient}",
                    message.Id, message.RecipientInfo);

                message.UpdatedAt = DateTime.UtcNow;
                await _repository.SaveChangesAsync();

                return Result<SentMessage>.Success(message, "Сообщение успешно отправлено");
            }
            else
            {
                message.Status = "Failed";
                var error = sendResult.Data?.ErrorMessage ?? "Неизвестная ошибка";
                _logger.LogWarning("Не удалось отправить сообщение {MessageId} получателю {Recipient}. Ошибка: {Error}",
                    message.Id, message.RecipientInfo, error);

                message.UpdatedAt = DateTime.UtcNow;
                await _repository.SaveChangesAsync();

                return Result<SentMessage>.Failure($"Ошибка отправки: {error}");
            }
        }
        catch (Exception ex)
        {
            // Обработка ошибок
            message.Status = "Failed";
            message.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();

            _logger.LogError(ex, "Ошибка при отправке сообщения {MessageId} получателю {Recipient}",
                message.Id, message.RecipientInfo);

            return Result<SentMessage>.Failure($"Ошибка отправки: {ex.Message}");
        }
    }

    /// <summary>
    /// Повторная отправка неудачного сообщения
    /// </summary>
    public async Task<Result<SentMessage>> RetryFailedMessage(int messageId, Guid userId)
    {
        // Получаем failed сообщение с проверкой userId
        var failedMessage = await _repository.GetHistoryByIdAndUserId(messageId, userId);

        if (failedMessage == null)
        {
            return Result<SentMessage>.Failure("Сообщение не найдено или не принадлежит пользователю");
        }

        if (failedMessage.Status != "Failed")
        {
            return Result<SentMessage>.Failure($"Нельзя отправить повторно сообщение со статусом {failedMessage.Status}");
        }

        _logger.LogInformation("Повторная отправка сообщения {MessageId} для пользователя {UserId}", messageId, userId);

        // Создаем новое сообщение для повторной отправки
        var retryMessage = new SentMessage
        {
            UserId = failedMessage.UserId,
            RecipientInfo = failedMessage.RecipientInfo,
            Content = failedMessage.Content,
            Channel = failedMessage.Channel,
            Status = "Pending"
        };

        return await HistoryMessage(retryMessage);
    }

    /// <summary>
    /// Получение сообщения по ID с проверкой принадлежности пользователю
    /// </summary>
    public async Task<Result<SentMessage>> GetMessageByIdAndUserId(int messageId, Guid userId)
    {
        try
        {
            var message = await _repository.GetHistoryByIdAndUserId(messageId, userId);

            if (message == null)
            {
                return Result<SentMessage>.Failure($"Сообщение с ID {messageId} не найдено");
            }

            return Result<SentMessage>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения сообщения {MessageId} для пользователя {UserId}", messageId, userId);
            return Result<SentMessage>.Failure("Ошибка получения сообщения");
        }
    }

    /// <summary>
    /// Получение истории сообщений пользователя
    /// </summary>
    public async Task<Result<List<SentMessage>>> GetUserHistory(Guid userId)
    {
        try
        {
            var history = await _repository.HistoryListMessageByUserId(userId);
            return Result<List<SentMessage>>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения истории для пользователя {UserId}", userId);
            return Result<List<SentMessage>>.Failure("Ошибка получения истории");
        }
    }

    /// <summary>
    /// Получение статистики по сообщениям
    /// </summary>
    public async Task<Result<MessageStatistics>> GetStatistics(Guid userId)
    {
        try
        {
            var messages = await _repository.HistoryListMessageByUserId(userId);

            var statistics = new MessageStatistics
            {
                TotalMessages = messages.Count,
                SentCount = messages.Count(m => m.Status == "Sent"),
                FailedCount = messages.Count(m => m.Status == "Failed"),
                PendingCount = messages.Count(m => m.Status == "Pending"),
                ByChannel = messages
                    .GroupBy(m => m.Channel ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<MessageStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения статистики для пользователя {UserId}", userId);
            return Result<MessageStatistics>.Failure("Ошибка получения статистики");
        }
    }

    /// <summary>
    /// Очистка старых сообщений для пользователя
    /// </summary>
    public async Task<Result<int>> CleanOldMessagesForUser(Guid userId, int daysToKeep)
    {
        try
        {
            if (daysToKeep < 1)
            {
                return Result<int>.Failure("Количество дней для хранения должно быть больше 0");
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var deletedCount = await _repository.DeleteOldHistoriesForUser(userId, cutoffDate);

            _logger.LogInformation("Удалено {Count} старых сообщений пользователя {UserId} старше {Days} дней",
                deletedCount, userId, daysToKeep);

            return Result<int>.Success(deletedCount, $"Удалено {deletedCount} сообщений старше {daysToKeep} дней");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при очистке старых сообщений для пользователя {UserId}", userId);
            return Result<int>.Failure("Ошибка при очистке старых сообщений");
        }
    }

    /// <summary>
    /// Получение всех неудачных сообщений пользователя
    /// </summary>
    public async Task<Result<List<SentMessage>>> GetFailedMessages(Guid userId)
    {
        try
        {
            var messages = await _repository.HistoryListMessageByUserId(userId);
            var failedMessages = messages.Where(m => m.Status == "Failed").ToList();

            return Result<List<SentMessage>>.Success(failedMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения неудачных сообщений для пользователя {UserId}", userId);
            return Result<List<SentMessage>>.Failure("Ошибка получения неудачных сообщений");
        }
    }
}

