using Configuration.DTOs;
using Confuguration.Dbcontext;
using DTOResponseSending;

namespace Confuguration.Services;

public interface IServiceAuthorization
{
    Task<Result<UserSession>> CreateUser(string Name, string Password, string Email);
    Task<Result<UserSession>> LoginUser(string Email, string Password);
    Task<Result<bool>> LogOutUser(string refreshToken);
    Task<Result<UserDto>> GetCurrentUser(Guid userId);
    Task<Result<UserSession>> RefreshToken(string refreshToken);
}
