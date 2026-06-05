
using System.Runtime.CompilerServices;
using Configuration.DTOs;
using Confuguration.Dbcontext;
using Microsoft.EntityFrameworkCore;

namespace Confuguration.Repository
{
    public interface IUserRepository
    {
        Task<User> CreateUser(User user);
        Task<bool> UpdateUser(User user);
        Task<bool> DeleteUser(User user);
        Task<User> ExistsUser(string Email);
        Task<User> ExistsUserName(string UserName);
        Task<List<User>> GetUserByAsync(string Password, string Email);
        Task<User> GetUserByUserId(Guid UserId);
        Task SaveChangesAsync();
    }    
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(UserDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<User> CreateUser(User user)
        {
            if(user.UserId == Guid.Empty)
            {
                user.UserId = Guid.NewGuid();
            }

            await _context.Users.AddAsync(user);
            return user;
        }
        public async Task<User> ExistsUser(string Email)
        {
            var result = await _context.Users
                        .Where(item => item.Email == Email)
                        .FirstOrDefaultAsync();
            return result;
        }
        public  Task<bool> UpdateUser(User user)
        {
            _context.Users.Update(user);
            return Task.FromResult(true);
        }
        public  Task<User> ExistsUserName(string UserName)
        {
            var result = _context.Users
                        .Where(item => item.UserName == UserName)
                        .FirstOrDefaultAsync();
            return result;
        }
        public  Task<bool> DeleteUser(User user)
        {
             _context.Users.Remove(user);
            return Task.FromResult(true);
        }
        public async Task SaveChangesAsync()
        {
             await _context.SaveChangesAsync();
        }
        public async Task<User> GetUserByUserId(Guid UserId)
        {
            return await _context.Users.FirstOrDefaultAsync(item => item.UserId == UserId);
        }
        public async Task<List<User>> GetUserByAsync(string Password, string Email)
        {
            return await _context.Users
                    .AsNoTracking()
                    .Where(item => item.Email == Email && item.PasswordHash == Password)
                    .Include(i => i.Tamplates)
                    .Include(i => i.Contacts)
                    .ToListAsync();
        }
    }
}