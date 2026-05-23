using Configuration.Repository;
using Confuguration.Dbcontext;
using Confuguration.ServicesSending;
using DTOResponseSending;
using Telegram.Bot.Types.Passport;

namespace Confuguration.Services;


public class ServicesTemplateUser
{
    private readonly UserTemplateRepository _repository;
    private readonly IMessageSender _message;
    private readonly ILogger<ServicesTemplateUser> _logger;

    public ServicesTemplateUser(UserTemplateRepository repository, IMessageSender message, ILogger<ServicesTemplateUser> logger)
    {
        _repository = repository;
        _message = message;
        _logger = logger;
    }

    public async Task<Result<UserTamplate>> CreateTamplate(UserTamplate user)
    {
        _logger.LogInformation("Принят запрос на создание нового шаблона {content} ", user.Content);

        if (string.IsNullOrWhiteSpace(user.Content))
        {
            _logger.LogWarning("Попытка создания шаблона с пустым содержимым");
            return Result<UserTamplate>.Failure("Пустой шаблон");
        }
        if (string.IsNullOrWhiteSpace(user.Name))
        {
            _logger.LogWarning("Попытка создания шаблона с пустым именем");
            return Result<UserTamplate>.Failure("Пустое имя шаблона");
        }

        var tamplateUser = await _repository.GetTamplates(user.Name, user.Content, user.UserId);
        if (tamplateUser != null)
        {
            _logger.LogWarning("Шаблон с именем {Name} или содержимым {Content} уже существует", user.Name, user.Content);
            return Result<UserTamplate>.Failure("Такой шаблон или имя шаблона уже существует");
        }

        var resultCreate = await _repository.CreateTamplases(user);
        if (resultCreate == null)
        {
            _logger.LogError("Ошибка при создании шаблона в репозитории. Имя: {Name}, Содержимое: {Content}", user.Name, user.Content);
            return Result<UserTamplate>.Failure("Ошибка создания шаблона");
        }

        var result = new UserTamplate
        {
            Content = resultCreate.Content,
            Name = resultCreate.Name,
        };

        await _repository.SaveChangesAsync();
        _logger.LogInformation("Шаблон успешно создан. Id: {Id}, Имя: {Name}, Содержимое: {Content}",
            result.Id, result.Name, result.Content);

        return Result<UserTamplate>.Success(result);
    }

    public async Task<Result<List<UserTamplate>>> ListTamplate(User user)
    {
        _logger.LogInformation("Принят запрос на получение списка шаблонов для пользователя {Username}", user.Username);

        if (string.IsNullOrWhiteSpace(user.Username))
        {
            _logger.LogWarning("Попытка получить список шаблонов с пустым именем пользователя");
            return Result<List<UserTamplate>>.Failure("Пустое имя");
        }

        var Listresult = await _repository.ListTemplate(user);

        if (Listresult == null || Listresult.Count == 0)
        {
            _logger.LogInformation("Шаблоны не найдены для пользователя {Username}", user.Username);
            return Result<List<UserTamplate>>.Issuccess("Шаблоны не найдены, сначала создайте их");
        }

        _logger.LogInformation("Успешно получено {Count} шаблонов для пользователя {Username}", Listresult.Count, user.Username);
        return Result<List<UserTamplate>>.Success(Listresult);
    }

    public async Task<Result<bool>> DeleteTamplate(UserTamplate user)
    {
        _logger.LogInformation("Принят запрос на удаление шаблона. Имя: {Name}, Содержимое: {Content}", user.Name, user.Content);

        if (string.IsNullOrWhiteSpace(user.Content))
        {
            _logger.LogWarning("Попытка удаления шаблона с пустым содержимым");
            return Result<bool>.Failure("Пустой шаблон");
        }
        if (string.IsNullOrWhiteSpace(user.Name))
        {
            _logger.LogWarning("Попытка удаления шаблона с пустым именем");
            return Result<bool>.Failure("Пустое имя шаблона");
        }

        var GetresultDelete = await _repository.GetTamplates(user.Name, user.Content, user.UserId);
        if (GetresultDelete == null)
        {
            _logger.LogWarning("Попытка удаления несуществующего шаблона. Имя: {Name}, Содержимое: {Content}", user.Name, user.Content);
            return Result<bool>.Failure("Такого шаблона не существует");
        }

        var result = await _repository.DeleteTemplatesByContent(user.Id, user.UserId );

        if (result == false || result == null)
        {
            _logger.LogError("Ошибка при удалении шаблона. Имя: {Name}, Содержимое: {Content}", user.Name, user.Content);
            return Result<bool>.Failure("Ошибка удаления шаблона");
        }

        _logger.LogInformation("Шаблон успешно удален. Имя: {Name}, Содержимое: {Content}", user.Name, user.Content);
        return Result<bool>.Success(result);
    }
}