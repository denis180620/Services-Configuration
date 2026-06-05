using Confuguration.Dbcontext;
using DTOResponseSending;
using Microsoft.EntityFrameworkCore;

namespace Confuguration.Repository
{
    public interface ISessionUser
    {
        Task<UserSession> CreateSession(UserSession session);
        Task<UserSession> GetUserSessionAsync(Guid UserId);
        Task<bool> UpdateUserSession(Guid UserId, string newRefreshToken);
        Task<bool> DeleteSession(Guid UserId);
        Task SaveChangesAsync();
    }

    public class SessionUser : ISessionUser
    {
        private readonly UserDbContext _context;

        public  SessionUser(UserDbContext context) 
        {
            _context = context;
        }
        public async Task<UserSession> CreateSession(UserSession session)
        {
            await _context.UserSessions.AddAsync(session);
            await _context.SaveChangesAsync();
            return session;
        }
        public async Task<UserSession> GetUserSessionAsync(Guid UserId)
        {
            return await _context.UserSessions
                            .FirstOrDefaultAsync(item => item.UserId == UserId & item.IsActive);
            
        }
        public async Task<bool> UpdateUserSession(Guid UserId, string newRefreshToken)
        {
             await _context.UserSessions
                            .Where(item => item.UserId == UserId && item.IsActive)
                            .ExecuteUpdateAsync(setters => setters
            .SetProperty(s => s.RefreshToken, newRefreshToken)
            .SetProperty(s => s.RefreshTokenExpiresAt, DateTime.UtcNow.AddDays(7))
        );
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteSession(Guid UserId)
        {
            await _context.UserSessions
               .Where(item => item.UserId == UserId && item.IsActive)
               .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.IsActive, false));
            
            return true;
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<UserSession?> GetSessionByRefreshToken(string refreshToken)
        {
            return await _context.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive);
        }

        public async Task UpdateSession(UserSession session)
        {
            _context.UserSessions.Update(session);
            await _context.SaveChangesAsync();
        }
    }
}