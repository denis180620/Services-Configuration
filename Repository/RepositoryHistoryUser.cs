using Confuguration.Dbcontext;
using Microsoft.EntityFrameworkCore;

namespace Confuguration.Repository{

public interface IUserHistoryRepository
{
    Task<SentMessage> HistoryMessage(SentMessage historymessage);
    Task<List<SentMessage>> HistoryListMessage(User user);
    Task<SentMessage?> GetHistory(SentMessage message);
    Task<bool> SaveChangesAsync();

    // Новые методы с Guid
    Task<List<SentMessage>> HistoryListMessageByUserId(Guid userId);
    Task<SentMessage?> GetHistoryByIdAndUserId(int messageId, Guid userId);
    Task<int> DeleteOldHistoriesForUser(Guid userId, DateTime olderThan);

    // Старые методы
    Task<int> DeleteOldHistories(DateTime olderThan);
    Task<bool> DeleteHistory(SentMessage message);
    Task<bool> DeleteHistory(int historyId);
    Task<int> DeleteUserHistories(Guid userId);
    Task<int> DeleteHistoriesByStatus(string status);
    Task<bool> DeleteTemplatesByContent(string content);
    Task<int> DeleteTemplatesOlderThan(DateTime date);
}

public class RepositoryHistoryUser : IUserHistoryRepository
{
    private readonly UserDbContext _context;

    public RepositoryHistoryUser(UserDbContext context)
    {
        _context = context;
    }

    public async Task<SentMessage> HistoryMessage(SentMessage historymessage)
    {
        historymessage.SentAt = DateTime.UtcNow;
        await _context.SentMessages.AddAsync(historymessage);
        return historymessage;
    }

    public async Task<List<SentMessage>> HistoryListMessage(User user)
    {
        return await _context.SentMessages
            .Where(item => item.User.Id == user.Id)
            .OrderByDescending(item => item.SentAt)
            .Select(item => new SentMessage
            {
                Id = item.Id,
                UserId = item.UserId,
                SentAt = item.SentAt,
                Status = item.Status,
                RecipientInfo = item.RecipientInfo,
                Content = item.Content,
                Channel = item.Channel,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Получение истории по UserId (Guid)
    /// </summary>
    public async Task<List<SentMessage>> HistoryListMessageByUserId(Guid userId)
    {
        return await _context.SentMessages
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.SentAt)
            .Select(item => new SentMessage
            {
                Id = item.Id,
                UserId = item.UserId,
                SentAt = item.SentAt,
                Status = item.Status,
                RecipientInfo = item.RecipientInfo,
                Content = item.Content,
                Channel = item.Channel,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<SentMessage?> GetHistory(SentMessage message)
    {
        return await _context.SentMessages
            .FirstOrDefaultAsync(item => item.Id == message.Id);
    }

    /// <summary>
    /// Получение сообщения по ID с проверкой UserId
    /// </summary>
    public async Task<SentMessage?> GetHistoryByIdAndUserId(int messageId, Guid userId)
    {
        return await _context.SentMessages
            .FirstOrDefaultAsync(item => item.Id == messageId && item.UserId == userId);
    }

    public async Task<int> DeleteOldHistories(DateTime olderThan)
    {
        return await _context.SentMessages
            .Where(h => h.SentAt < olderThan)
            .ExecuteDeleteAsync();
    }

    /// <summary>
    /// Удаление старых сообщений для конкретного пользователя
    /// </summary>
    public async Task<int> DeleteOldHistoriesForUser(Guid userId, DateTime olderThan)
    {
        return await _context.SentMessages
            .Where(h => h.UserId == userId && h.SentAt < olderThan)
            .ExecuteDeleteAsync();
    }

    public async Task<bool> DeleteHistory(int historyId)
    {
        var deleted = await _context.SentMessages
            .Where(h => h.Id == historyId)
            .ExecuteDeleteAsync();
        return deleted > 0;
    }

    public async Task<bool> DeleteHistory(SentMessage message)
    {
        if (message == null || message.Id == 0)
            return false;

        var deleted = await _context.SentMessages
            .Where(h => h.Id == message.Id)
            .ExecuteDeleteAsync();
        return deleted > 0;
    }

    public async Task<int> DeleteUserHistories(Guid userId)
    {
        return await _context.SentMessages
            .Where(h => h.User.Id == userId)
            .ExecuteDeleteAsync();
    }

    public async Task<int> DeleteHistoriesByStatus(string status)
    {
        return await _context.SentMessages
            .Where(h => h.Status == status)
            .ExecuteDeleteAsync();
    }

    public async Task<bool> DeleteTemplatesByContent(string content)
    {
        var templates = await _context.UserTemplates
            .Where(t => t.Content.Contains(content))
            .ToListAsync();

        _context.UserTemplates.RemoveRange(templates);
        return true;
    }

    public async Task<int> DeleteTemplatesOlderThan(DateTime date)
    {
        var oldTemplates = await _context.UserTemplates
            .Where(t => t.CreatedAt < date)
            .ToListAsync();

        _context.UserTemplates.RemoveRange(oldTemplates);
        return oldTemplates.Count;
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}
}