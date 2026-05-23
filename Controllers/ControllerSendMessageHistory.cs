using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Confuguration.Services;
using Confuguration.Dbcontext;
using System.Security.Claims;

namespace CongratulationService.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ServicesHistory _services;
        private readonly ILogger<MessageController> _logger;

        public MessageController(ServicesHistory services, ILogger<MessageController> logger)
        {
            _services = services;
            _logger = logger;
        }

        /// <summary>
        /// Получить UserId из JWT токена
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("UserId не найден в токене");

            return Guid.Parse(userIdClaim);
        }

        /// <summary>
        /// Отправка нового сообщения
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ResponseMessage messageuser)
        {
            _logger.LogInformation("Принят запрос на отправление сообщения");

            if (messageuser == null)
            {
                return BadRequest(new { error = "Тело запроса не может быть пустым" });
            }

            try
            {
                var result = await _services.HistoryMessage(new SentMessage
                {
                    UserId = GetCurrentUserId(),
                    RecipientInfo = messageuser.RecipientInfo,
                    Channel = messageuser.Channel,
                    Content = messageuser.Content
                });

                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        success = true,
                        messageId = result.Data?.Id,
                        status = result.Data?.Status,
                        sentAt = result.Data?.SentAt,
                        message = result.Message
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сообщения");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Повторная отправка неудачного сообщения
        /// </summary>
        [HttpPost("retry/{messageId}")]
        public async Task<IActionResult> RetryFailedMessage(int messageId)
        {
            _logger.LogInformation("Запрос на повторную отправку сообщения {MessageId}", messageId);

            try
            {
                var result = await _services.RetryFailedMessage(messageId, GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    return BadRequest(new
                    {
                        success = false,
                        originalMessageId = messageId,
                        error = result.ErrorMessage
                    });
                }

                return Ok(new
                {
                    success = true,
                    originalMessageId = messageId,
                    newMessageId = result.Data?.Id,
                    status = result.Data?.Status,
                    message = result.Message ?? "Сообщение успешно отправлено повторно"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при повторной отправке сообщения {MessageId}", messageId);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получение статуса сообщения
        /// </summary>
        [HttpGet("status/{messageId}")]
        public async Task<IActionResult> GetMessageStatus(int messageId)
        {
            try
            {
                var result = await _services.GetMessageByIdAndUserId(messageId, GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                var message = result.Data;

                return Ok(new
                {
                    messageId = message.Id,
                    status = message.Status,
                    sentAt = message.SentAt,
                    recipient = message.RecipientInfo,
                    channel = message.Channel,
                    content = message.Content
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статуса сообщения {MessageId}", messageId);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получение истории сообщений
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetUserHistory([FromQuery] string status = null, [FromQuery] int? limit = null)
        {
            try
            {
                var result = await _services.GetUserHistory(GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                var messages = result.Data ?? new List<SentMessage>();

                if (!string.IsNullOrEmpty(status))
                {
                    messages = messages.Where(m => m.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (limit.HasValue && limit.Value > 0)
                {
                    messages = messages.Take(limit.Value).ToList();
                }

                return Ok(new
                {
                    success = true,
                    total = messages.Count,
                    statusFilter = status ?? "all",
                    messages = messages.Select(m => new
                    {
                        m.Id,
                        m.Status,
                        m.SentAt,
                        m.RecipientInfo,
                        m.Channel,
                        contentPreview = m.Content?.Length > 50 ? m.Content.Substring(0, 50) + "..." : m.Content,
                        canRetry = m.Status == "Failed"
                    })
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении истории");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получение статистики
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var result = await _services.GetStatistics(GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                var statistics = result.Data;

                return Ok(new
                {
                    success = true,
                    statistics = new
                    {
                        statistics.TotalMessages,
                        statistics.SentCount,
                        statistics.FailedCount,
                        statistics.PendingCount,
                        successRate = statistics.TotalMessages > 0
                            ? (double)statistics.SentCount / statistics.TotalMessages * 100
                            : 0,
                        byChannel = statistics.ByChannel
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статистики");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Очистка старых сообщений
        /// </summary>
        [HttpDelete("clean")]
        public async Task<IActionResult> CleanOldMessages([FromQuery] int daysToKeep = 30)
        {
            try
            {
                _logger.LogInformation("Запрос на очистку сообщений старше {Days} дней", daysToKeep);

                var result = await _services.CleanOldMessagesForUser(GetCurrentUserId(), daysToKeep);

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                return Ok(new
                {
                    success = true,
                    deletedCount = result.Data,
                    daysKept = daysToKeep,
                    message = result.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке старых сообщений");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получение всех неудачных сообщений
        /// </summary>
        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedMessages()
        {
            try
            {
                _logger.LogInformation("Запрос неудачных сообщений");
                var result = await _services.GetFailedMessages(GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                var failedMessages = result.Data ?? new List<SentMessage>();

                return Ok(new
                {
                    success = true,
                    failedCount = failedMessages.Count,
                    messages = failedMessages.Select(m => new
                    {
                        m.Id,
                        m.SentAt,
                        m.RecipientInfo,
                        m.Channel,
                        contentPreview = m.Content?.Length > 50 ? m.Content.Substring(0, 50) + "..." : m.Content,
                        canRetry = true
                    })
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения неудачных сообщений");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Массовая повторная отправка всех неудачных сообщений
        /// </summary>
        [HttpPost("retry-all")]
        public async Task<IActionResult> RetryAllFailedMessages()
        {
            try
            {
                _logger.LogInformation("Запрос на массовую повторную отправку");

                var currentUserId = GetCurrentUserId();
                var failedResult = await _services.GetFailedMessages(currentUserId);

                if (!failedResult.IsSuccess)
                {
                    return BadRequest(new { error = failedResult.ErrorMessage });
                }

                var failedMessages = failedResult.Data ?? new List<SentMessage>();

                if (!failedMessages.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Нет неудачных сообщений для повторной отправки",
                        retriedCount = 0
                    });
                }

                var results = new List<object>();
                var successCount = 0;

                foreach (var failedMessage in failedMessages)
                {
                    var retryResult = await _services.RetryFailedMessage(failedMessage.Id, currentUserId);

                    if (retryResult.IsSuccess)
                    {
                        successCount++;
                        results.Add(new
                        {
                            originalId = failedMessage.Id,
                            newId = retryResult.Data?.Id,
                            status = retryResult.Data?.Status,
                            success = true
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            originalId = failedMessage.Id,
                            error = retryResult.ErrorMessage,
                            success = false
                        });
                    }
                }

                return Ok(new
                {
                    success = true,
                    totalFailed = failedMessages.Count,
                    successCount = successCount,
                    failedCount = failedMessages.Count - successCount,
                    results = results
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при массовой повторной отправке");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получение подробной информации о сообщении
        /// </summary>
        [HttpGet("details/{messageId}")]
        public async Task<IActionResult> GetMessageDetails(int messageId)
        {
            try
            {
                var result = await _services.GetMessageByIdAndUserId(messageId, GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    return NotFound(new { error = result.ErrorMessage });
                }

                var message = result.Data;

                return Ok(new
                {
                    messageId = message.Id,
                    recipient = message.RecipientInfo,
                    channel = message.Channel,
                    content = message.Content,
                    status = message.Status,
                    sentAt = message.SentAt,
                    updatedAt = message.UpdatedAt,
                    canRetry = message.Status == "Failed",
                    errorInfo = message.Status == "Failed" ? "Сообщение не было доставлено. Вы можете повторить отправку." : null
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении деталей сообщения {MessageId}", messageId);
                return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
            }
        }
    }
}