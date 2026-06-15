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
            var currentUserId = GetUserIdFromToken();
            if (template == null)
            {
                return BadRequest(new { error = "Тело запроса не может быть пустым" });
            }
            template.UserId = currentUserId;
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
        [HttpGet("list")]
        public async Task<IActionResult> GetUserTemplates( [FromQuery] string username = null)
        {
            _logger.LogInformation("Запрос списка шаблонов для пользователя");

            var currentUserId = GetUserIdFromToken();

            var result = await _services.ListTamplate(currentUserId);

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
        public async Task<IActionResult> DeleteTemplate([FromQuery] string Name, [FromQuery] string Content)
        {
            _logger.LogInformation("Запрос на удаление шаблона. Имя: {Name}", Name);

            var currentUserId = GetUserIdFromToken();

            var result = await _services.DeleteTamplate(Name, Content, currentUserId);

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
        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("UserId not found is token");
            }
            return userId;
        }
    }
}