using Confuguration.Dbcontext;
using Microsoft.EntityFrameworkCore;

namespace Configuration.Repository;

public interface IUserTemplateRepository
{
    Task<UserTamplate> CreateTamplases(UserTamplate tamplate);
    Task<List<UserTamplate>> ListTemplate(User user);
    Task<UserTamplate?> GetTamplates(string Name, string Content, Guid UserId);

    Task<bool> SaveChangesAsync();

    Task<bool> DeleteTemplatesByContent(int historyId, Guid userid);
    Task<int> DeleteTemplatesOlderThan(DateTime date, Guid userid);
}

public class UserTemplateRepository : IUserTemplateRepository
{
    private readonly UserDbContext _context;

    public UserTemplateRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<UserTamplate> CreateTamplases(UserTamplate tamplate)
    {
        tamplate.CreatedAt = DateTime.UtcNow;
        await _context.UserTemplates.AddAsync(tamplate);
        return tamplate;
    }

    public async Task<List<UserTamplate>> ListTemplate(User user)
    {

        return await _context.UserTemplates
            .Where(item => item.User.UserId == user.UserId)
            .Select(item => new UserTamplate
            {
                Id = item.Id,
                Content = item.Content,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserTamplate?> GetTamplates(string Name, string Content, Guid UserId)
    {

        return await _context.UserTemplates
                .Where(item => item.UserId == UserId)
            .FirstOrDefaultAsync(item => item.Content == Content || item.Name == Name);
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            return false;
        }
    }

    /// <summary>
    /// Удалить запись истории по ID
    /// </summary>
    public async Task<bool> DeleteTemplatesByContent(int historyId, Guid userid)
    {
        var history = await _context.SentMessages
            .Where(item => item.UserId == userid)
            .FirstOrDefaultAsync(h => h.Id == historyId);

        _context.SentMessages.Remove(history);
        return true;
    }


    /// <summary>
    /// Удалить старые шаблоны (старше указанной даты)
    /// </summary>
    public async Task<int> DeleteTemplatesOlderThan(DateTime date, Guid userid)
    {
        var oldTemplates = await _context.UserTemplates
            .Where(item => item.UserId == userid)
            .Where(t => t.CreatedAt < date)
            .ToListAsync();

        _context.UserTemplates.RemoveRange(oldTemplates);
        return oldTemplates.Count;
    }
}