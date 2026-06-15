using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CongratulationService.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly ServicesContact _services;
        private readonly ILogger<ContactController> _logger;

        public ContactController(ServicesContact services, ILogger<ContactController> logger)
        {
            _services = services;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateContact([FromBody] Contact contact)
        {
            _logger.LogInformation("Принят запрос на создание контакта");
            try{

            var currentUserId = GetUserIdFromToken();
            contact.UserId = currentUserId;
            var result = await _services.CreateContact(contact);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    result.Data.Name,
                    result.Data.Email,
                    result.Data.NikNameTelegram,
                    result.Data.IdVk,
                    result.Data.Phone
                });
            }
            return BadRequest(new
            {
                success = false,
                message = result.ErrorMessage
            });

            
        }
        catch (UnauthorizedAccessException ex)
        {
                return Unauthorized(new { error = ex.Message });
        }
            catch (Exception ex)
        {
                _logger.LogError(ex,"Ошибка содания контакта");
                return StatusCode (500, new { message ="Внутренняя ошибка сервера"});
        }

        }
    [HttpGet("getcontacts")]
    public async Task<IActionResult> GetContacts()
        {
            _logger.LogInformation("Принят запрос на создание на получение всех контактов");
            try{
                var currentUserId = GetUserIdFromToken();
            var result = await _services.GetContacts(currentUserId);
            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    result.Data
                });
            }
            return BadRequest(new
            {
                success = false,
                result.ErrorMessage
            });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка получения контактов");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }

        }
        [HttpGet("contact")]
        public async Task<IActionResult> GetContact(string name)
        {
            _logger.LogInformation("Принят запрос наполчение контакта {Name}", name);
            try{
                var currentUserId = GetUserIdFromToken();
            var result = await _services.GetContact(name, currentUserId);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    result.Data.Name,
                    result.Data.Email,
                    result.Data.NikNameTelegram,
                    result.Data.IdVk
                });
            }
                return BadRequest(new
                {
                    success = false,
                    result.ErrorMessage
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка получения контакта, {Name}", name);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
        [HttpDelete("deletecontact")]
        public async Task<IActionResult> DeleteContact(int id,string name)
        {
            _logger.LogInformation("Принят запрос на удаления контакта");
            try{
                var currentUserId = GetUserIdFromToken();
            var result = await _services.DeleteContact(id,name, currentUserId);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    success = true,
                    result.Data
                });
            }
                return BadRequest(new
                {
                    success = false,
                    result.ErrorMessage
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка получения контакта, {Name}", name);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }
        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if(string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("UserId not found is token");
            }
            return userId;
        }
    }
}
