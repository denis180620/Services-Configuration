using Configuration.Repository;
using Confuguration.Dbcontext;
using DTOResponseSending;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Bcpg;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Configuration.DTOs;

namespace Confuguration.Services;

public class ServiceAuthorization : IServiceAuthorization
{
    private readonly ILogger<ServiceAuthorization> _logger;
    private readonly UserRepository _repositoryUser;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly SessionUser _session;
    

    public ServiceAuthorization(ILogger<ServiceAuthorization> logger, UserRepository userRepository, UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration, SessionUser session)
    {
        _logger = logger;
        _repositoryUser = userRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _session = session;
        
    }
    public async Task<Result<UserSession>> CreateUser(string Name, string Password, string Email)
    {
        _logger.LogInformation("Принят запрос на создание пользователя: {Email}", Email);

        // Валидация
        if (string.IsNullOrWhiteSpace(Name))
        {
            _logger.LogWarning("Пустое имя");
            return Result<UserSession>.Failure("Пустое имя");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            _logger.LogWarning("Пустой пароль");
            return Result<UserSession>.Failure("Пустой пароль");
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            _logger.LogWarning("Пустой Email");
            return Result<UserSession>.Failure("Пустой email");
        }

        // Проверка существования по вашему UserId не нужна при создании
        // Но проверяем email и имя через репозиторий
        var resultEmail = await _repositoryUser.ExistsUser(Email);
        if (resultEmail != null)
        {
            _logger.LogWarning("Email существует, добавьте другой или войдите");
            return Result<UserSession>.Failure("Email существует, добавьте другой или войдите");
        }

        var resultName = await _repositoryUser.ExistsUserName(Name);
        if (resultName != null)
        {
            _logger.LogWarning("Такое имя существует, придумайте другое");
            return Result<UserSession>.Failure("Такое имя существует, придумайте другое");
        }

        // Генерируем UserId заранее
        var userId = Guid.NewGuid();

        var user = new User
        {
            UserId = userId, // Ваш кастомный идентификатор
            UserName = Name,
            Email = Email,
            Role = "User",   // Ваше кастомное поле роли
            CreatedAt = DateTime.UtcNow
        };

        // Создаем пользователя через Identity
        var result = await _userManager.CreateAsync(user, Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Ошибка регистрации: {Errors}", errors);
            return Result<UserSession>.Failure($"Ошибка регистрации: {errors}");
        }

        // Создаем роль если её нет (для Identity)
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            var roleResult = await _roleManager.CreateAsync(new Role
            {
                Name = "User",
                Description = "Обычный пользователь"
            });
        
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            _logger.LogError("Ошибка создания роли User: {Errors}", errors);
            await _userManager.DeleteAsync(user); // Откатываем создание пользователя
            return Result<UserSession>.Failure("Ошибка настройки прав доступа");
        }
        }
        // Добавляем пользователя в роль Identity
        await _userManager.AddToRoleAsync(user, "User");

        _logger.LogInformation("Пользователь зарегистрирован {Email} с UserId: {UserId}", Email, userId);

        // Генерируем токены (передаем ваш userId)
        var tokens = await GenerateTokensAsync(user, userId);

        return Result<UserSession>.Success(new UserSession
        {
            JwtToken = tokens.JwtToken,
            RefreshToken = tokens.RefreshToken,
            UserId = userId // Возвращаем ваш кастомный UserId
        });
    }

    public async Task<Result<UserSession>> LoginUser(string Email, string Password)
    {
        _logger.LogInformation("Попытка входа, {Email}", Email);

        var user = await _userManager.FindByEmailAsync(Email);
        if(user == null)
        {
            _logger.LogWarning("Пользователь не найден");
            return Result<UserSession>.Failure("Неверный email или пароль");
        }
        var isPassword = await _userManager.CheckPasswordAsync(user, Password);
        if (!isPassword)
        {
            _logger.LogWarning("Неверный пароль");
            return Result<UserSession>.Failure("Неверный email или пароль");
        }
        await _userManager.ResetAccessFailedCountAsync(user);

        var userId = user.UserId;

        var tokens = await GenerateTokensAsync(user, userId);

        _logger.LogInformation("Пользователь успешно вход");
        return Result<UserSession>.Success(tokens);
        
    }

    public async Task<Result<bool>> LogOutUser(string refreshToken)
    {
        _logger.LogInformation("Выход из приложения");

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<bool>.Failure("Отсутствует токен");
        }
        var session = await _session.GetSessionByRefreshToken(refreshToken);
        if (session != null)
        {
            session.IsActive = false;
            await _session.UpdateSession(session);
            _logger.LogInformation("Сессия деактивирована для UserId: {UserId}", session.UserId);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<UserDto>> GetCurrentUser(Guid userId)
    {
        _logger.LogInformation("Получение информации о пользователе: {UserId}", userId);

        var user = await _repositoryUser.GetUserByUserId(userId);
        if (user == null)
        {
            return Result<UserDto>.Failure("Пользователь не найден");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var userDto = new UserDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            Email = user.Email,
            Role = roles.FirstOrDefault() ?? "User",
            CreatedAt = user.CreatedAt
        };

        return Result<UserDto>.Success(userDto);
    }

    public async Task<Result<UserSession>> RefreshToken(string refreshToken)
    {
        _logger.LogInformation("ПРинят запрос на обновление токена");

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<UserSession>.Failure("Отсутствует токен авторизации");
        }

        var session = await _session.GetSessionByRefreshToken(refreshToken);
        if(session == null)
        {
            _logger.LogWarning("Сессия не найдена");
            return Result<UserSession>.Failure("Сессия не найдена");
        }
        if (!session.IsActive)
        {
            _logger.LogWarning("Сессия не активна");
            return Result<UserSession>.Failure("Сессия не активна");
        }
        if (session.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token истек");
            session.IsActive = false;
            await _session.UpdateSession(session);
            return Result<UserSession>.Failure("Истек срок токена");
        }

        var user = await _repositoryUser.GetUserByUserId(session.UserId);
        if(user == null)
        {
            _logger.LogWarning("Пользователь не найден для UserId: {UserId}", session.UserId);
            return Result<UserSession>.Failure("Пользователя не существет");
        }

        session.IsActive = false;
        await _session.UpdateSession(session);

        var newToken =  await GenerateTokensAsync(user, user.UserId);

        return Result<UserSession>.Success(newToken);
    }
    private async Task<UserSession> GenerateTokensAsync(User user, Guid userId)
    {
        var accessToken = await GenerateAccessTokenAsync(user, userId);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenExpirationDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);
        var accessTokenExpirationMinutes = _configuration.GetValue<int>("JwtSettings:ExpirationMinutes", 60);

        var userSession = new UserSession
        {
            UserId = userId, // Ваш кастомный UserId
            JwtToken = accessToken,
            RefreshToken = refreshToken,
            CreatedAt = DateTime.UtcNow,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
            IsActive = true,
            ExpiresIn = accessTokenExpirationMinutes * 60,
            User = user
        };

        await _session.CreateSession(userSession);
        await _session.SaveChangesAsync();

        _logger.LogInformation("Создана новая сессия для пользователя с UserId: {UserId}", userId);

        return userSession;
    }

    private async Task<string> GenerateAccessTokenAsync(User user, Guid userId)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

        var claims = new List<Claim>
    {
        // Кладем в токен ваш кастомный UserId
        new Claim("UserId", userId.ToString()), // Кастомный claim
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Identity Id
        new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
    };

        // Получаем роли из Identity
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(secretKey);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtSettings.GetValue<int>("ExpirationMinutes", 60)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}