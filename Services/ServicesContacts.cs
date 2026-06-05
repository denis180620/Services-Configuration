using Confuguration.Repository;
using DTOResponseSending;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class ServicesContact
{
    private readonly IContact _repository;
    private readonly ILogger<ServicesContact> _logger;

    public ServicesContact(IContact repository, ILogger<ServicesContact> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Contact>> CreateContact(Contact contact)
    {
        try{
        _logger.LogInformation("Принят запрос на создание нового контакта");

        if (string.IsNullOrWhiteSpace(contact.Name))
        {
            _logger.LogWarning("Пустое имя");
            return Result<Contact>.Failure("Пустое имя контакта");
        }
        if(contact.UserId == Guid.Empty)
        {
            _logger.LogWarning("Пустой ключ");
            return Result<Contact>.Failure("Пусткого ключа не может быть");
        }
            var contacthandler = await _repository.GetContactById(contact.Name, contact.UserId);

            if (contacthandler != null)
            {
                _logger.LogWarning("Такой контакт существует");
                return Result<Contact>.Failure("Такой контакт существует");
            }
            var result = await _repository.CreateContact(contact);

        await _repository.SaveChangesAsync();

        return Result<Contact>.Success(result);
        }
        catch(Exception ex)
        {
            _logger.LogError("Внутренняя ошибка сервера");
            return Result<Contact>.Failure("Внутренняя ошибка сервера");
        }
    }
    public async Task<Result<List<Contact>>> GetContacts(Guid userid)
    {
        try{
        if (userid == Guid.Empty)
        {
            _logger.LogWarning("Пустой ключ");
            return  Result<List<Contact>>.Failure("Пусткого ключа не может быть");
        }
        var result = await _repository.GetContacts(userid);

        if(result == null)
        {
            _logger.LogWarning("Контакты не найдены");
            return Result<List<Contact>>.Failure("Нет контактов");
        }

        return Result<List<Contact>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("Внутренняя ошибка сервера");
            return Result<List<Contact>>.Failure("Внутренняя ошибка сервера");
        }
    }
    public async Task<Result<Contact>> GetContact(string name, Guid userid)
    {
        try{
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Пустое имя");
            return Result<Contact>.Failure("Пустое имя контакта");
        }
        if (userid == Guid.Empty)
        {
            _logger.LogWarning("Пустой ключ");
            return Result<Contact>.Failure("Пусткого ключа не может быть");
        }
        
        var result = await _repository.GetContact(name, userid);

        if (result == null)
        {
            _logger.LogWarning("Контакты не найдены");
            return Result<Contact>.Failure("Нет контактов");
        }
        return Result<Contact>.Success(result);
    }
        catch (Exception ex)
        {
            _logger.LogError("Внутренняя ошибка сервера");
            return Result<Contact>.Failure("Внутренняя ошибка сервера");
        }
    }
    public async Task<Result<bool>> DeleteContact(int id, string name, Guid userid)
    {
        try
        {
            if (id == 0)
            {
                _logger.LogWarning("Id равен ноль");
                return Result<bool>.Failure("Пустой id");
            }
            if (userid == Guid.Empty)
            {
                _logger.LogWarning("Пустой ключ");
                return Result<bool>.Failure("Пусткого ключа не может быть");
            }
            var contacthandler = await _repository.GetContactById(name, userid);

            if (contacthandler == null)
            {
                _logger.LogWarning("Такого контакта не существует");
                return Result<bool>.Failure("Такой контакта не существует");
            }
            var result = await _repository.DeleteContact(id, userid);
            if (result == false)
            {
                _logger.LogWarning("Контакты не найдены");
                return Result<bool>.Failure("Нет контактов");
            }
            await _repository.SaveChangesAsync();
            return Result<bool>.Success(result);

        }
        catch (Exception ex)
        {
            _logger.LogError("Внутренняя ошибка сервера");
            return Result<bool>.Failure("Внутренняя ошибка сервера");
        }
    }
}