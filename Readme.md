## Запуск проекта
Установите PostgreSQL любой версии, включите PgAdmin4.

Скачайте и установите последнюю версию .NET.

Создайте сервер базы данных и базу данных CongratulationDB.

В файле appsettings.json в строке ConnectionStrings пропишите свои Username и Password:

json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=CongratulationDB;Username=postgres;Password=postgres"
}
Если при запуске возникнет ошибка отсутствия миграций, выполните обновление базы данных (например, dotnet ef database update).

К данному API написан клиент:
https://github.com/denis180620/Services-Configuration-client-

Важно
В API используется JWT-авторизация. Во всех эндпоинтах (кроме авторизации) требуется передавать токен, полученный при входе.
Временно в эндпоинтах (кроме авторизации) также передаётся userId. В следующих версиях userId будет автоматически определяться из токена.



## API Endpoints

Авторизация Jwt

POST /api/auth/register - Регистрация
POST /api/auth/login - Вход
POST /api/auth/refresh - запрос нового токена авторизации
POST /api/auth/logout - выход
GET /api/auth/me - запрос данных себя

Сообщения

POST	/api/message/send - Отправка сообщения
DELETE	/api/message/clean?daysToKeep=30 - Очистка истории сообщений
GET	/api/message/history/{userId}	История сообщений с фильтрацией
GET	/api/message/details/{messageId}	Детальная информация о сообщении


Контакты

POST /api/contact/create - Создание контакта
GET /api/contact/getContacts - Получение всех контактов пользователя
GET /api/contact/contact - Получение одного контакта пользователя
DELETE /api/contact/deletecontact - Удаление одного контакта

Шаблоны

POST /api/template/create - Создание шаблона
GET /api/template/list - Получение списка шаблонов пользователя
DELETE /api/template/delete - Удаление одного шаблона пользователя
