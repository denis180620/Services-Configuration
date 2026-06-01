using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Confuguration.Services;
using Confuguration.Dbcontext;
using DTOResponseSending;

namespace CongratulationService.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly ServicesTemplateUser _services;
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(ServicesTemplateUser services, ILogger<TemplateController> logger)
        {
            _services = services;
            _logger = logger;
        }

        /// <summary>
        /// Создание нового шаблона
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateTemplate([FromBody] UserTamplate template)
        {
            _logger.LogInformation("Принят запрос на создание шаблона");

            if (template == null)
            {
                return BadRequest(new { error = "Тело запроса не может быть пустым" });
            }

            var result = await _services.CreateTamplate(template);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = "Шаблон успешно создан",
                data = result.Data
            });
        }

        /// <summary>
        /// Получение списка шаблонов пользователя
        /// </summary>
        [HttpGet("list/{userId}")]
        public async Task<IActionResult> GetUserTemplates(Guid userId, [FromQuery] string username = null)
        {
            _logger.LogInformation("Запрос списка шаблонов для пользователя {UserId}", userId);

            var user = new User
            {
                UserName = username ?? $"user_{userId}"
            };

            var result = await _services.ListTamplate(user);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        /// <summary>
        /// Удаление шаблона
        /// </summary>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteTemplate([FromBody] UserTamplate template)
        {
            _logger.LogInformation("Запрос на удаление шаблона. Имя: {Name}", template?.Name);

            if (template == null)
            {
                return BadRequest(new { error = "Данные шаблона не могут быть пустыми" });
            }

            var result = await _services.DeleteTamplate(template);

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = "Шаблон успешно удален",
                data = result.Data
            });
        }
    }
}