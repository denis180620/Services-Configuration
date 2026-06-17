using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Configuration.DTOs;
using Confuguration.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CongratulationService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IServiceAuthorization _services;

        public  AuthController(IServiceAuthorization service)
        {
            _services = service;
        }
        [HttpPost("register")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequestDto request)
        {
            var result = await _services.CreateUser(request.Name, request.Password, request.Email);

            if (!result.IsSuccess)
            {
                return BadRequest(new {success = false, message = result.ErrorMessage});
            }

            SetRefreshTokenCookie(result.Data.RefreshToken);

            return Ok(new
            {
                success = true,
                accessToken = result.Data.JwtToken
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> LoginUser( [FromBody] LoginRequestDto request)
        {
            var result = await _services.LoginUser(request.Email, request.Password);

            if (!result.IsSuccess)
            {
                return Unauthorized(new {success = false, message = result.ErrorMessage});
            }

            SetRefreshTokenCookie(result.Data.RefreshToken);

            return Ok(new
            {
                success = true,
                accessToken = result.Data.JwtToken
            });

        }
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized(new {success = false, message = "Отсутствует токен авторизации"});
            }
            var result = await _services.RefreshToken(refreshToken);

            if (!result.IsSuccess)
            {
                Response.Cookies.Delete("refreshToken");
                return Unauthorized(new {success = false, message = result.ErrorMessage});
            }
            SetRefreshTokenCookie(result.Data.RefreshToken);

            return Ok(new
            {
                success = true,
                accessToken = result.Data.JwtToken
            });
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _services.LogOutUser(refreshToken);
                Response.Cookies.Delete("refreshToken");
            }

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = User.Claims.FirstOrDefault(item => item.Type == ClaimTypes.NameIdentifier);
            if (user == null || !Guid.TryParse(user.Value, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid token" });
            }

            var result = await _services.GetCurrentUser(userId);

            if (!result.IsSuccess)
            {
                return NotFound(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, user = result.Data });
        }
        private void SetRefreshTokenCookie(string RefreshToken)
        {
            Response.Cookies.Append("refreshToken", RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddYears(3),
                Path = "/"
            });
        }
    }
}