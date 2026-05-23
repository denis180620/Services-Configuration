using Confuguration.Dbcontext;
using Microsoft.EntityFrameworkCore;

namespace Configuration.Repository;

public interface IContact
{
    Task<Contact> CreateContact(Contact contact);
    Task<bool> DeleteContact(int id, Guid userid);
    Task<Contact> GetContact(string name, Guid userid);
    Task<List<Contact>> GetContacts(Guid userid);
    Task SaveChangesAsync();
    Task<Contact> GetContactById(string name, Guid userid);
}

public class Contacts : IContact
{
    private readonly UserDbContext _context;

    public Contacts(UserDbContext context)
    {
        _context = context;
    }
    public async Task<Contact> CreateContact(Contact contact)
    {
        await _context.Contacts.AddAsync(contact);
        return contact;
    }
    public async Task<Contact> GetContact(string name, Guid userid)
    {
        var result = await _context.Contacts
                .Where(item => item.UserId == userid)
                .Where(item => item.Name == name)
                .FirstOrDefaultAsync();

        return result;
    }
    public async Task<List<Contact>> GetContacts(Guid userid)
    {
        var result = await _context.Contacts
                    .Where(item => item.UserId == userid)
                    .ToListAsync();

        return result;
    }
    public async Task<bool> DeleteContact(int id, Guid userid)
    {
        var result = await _context.Contacts
                    .Where(item => item.UserId == userid)
                    .Where(item => item.Id == id)
                    .ToListAsync();

         _context.Contacts.RemoveRange(result);
        return true;
    }
    public async Task<Contact> GetContactById(string name, Guid userid)
    {
        var result = await _context.Contacts
                    .Where(item => item.UserId == userid)
                    .Where(item => item.Name == name)
                    .FirstOrDefaultAsync();
        return result;
    }
    public async Task SaveChangesAsync()
    {
        await SaveChangesAsync();
    }
}