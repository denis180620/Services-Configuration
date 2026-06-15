using Confuguration.Dbcontext;
using Microsoft.EntityFrameworkCore;

namespace Confuguration.Repository{

public interface IUserTemplateRepository
{
    Task<UserTamplate> CreateTamplases(UserTamplate tamplate);
    Task<List<UserTamplate>> ListTemplate(Guid userId);
    Task<UserTamplate?> GetTamplates(string Name, string Content, Guid UserId);

    Task<bool> SaveChangesAsync();

    Task<bool> DeleteTemplatesByContent(int historyId, Guid userid);
    Task<int> DeleteTemplatesOlderThan(string Name, Guid userid);
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

    public async Task<List<UserTamplate>> ListTemplate(Guid userId)
    {

        return await _context.UserTemplates
            .Where(item => item.UserId == userId)
            .Select(item => new UserTamplate
            {
                Id = item.Id,
                UserId = item.UserId,
                Name = item.Name,
                Content = item.Content,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserTamplate> GetTamplates(string Name, string Content, Guid UserId)
    {

        return await _context.UserTemplates
            .FirstOrDefaultAsync(item => item.UserId == UserId && item.Name == Name);
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
    /// Удалить шаблон 
    /// </summary>
    public async Task<int> DeleteTemplatesOlderThan(string Name, Guid userid)
    {
        var oldTemplates = await _context.UserTemplates
            .Where(item => item.UserId == userid)
            .Where(t => t.Name == Name)
            .ToListAsync();

        _context.UserTemplates.RemoveRange(oldTemplates);
        await _context.SaveChangesAsync();
        return oldTemplates.Count;
    }
}
}