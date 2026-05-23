
Конченые точки создание отправление сообщений и истории
Метод	Эндпоинт	Описание
POST	/api/message/send	Отправка нового сообщения
POST	/api/message/retry/{messageId}	Повторная отправка конкретного сообщения
POST	/api/message/retry-all/{userId}	Массовая повторная отправка всех неудачных сообщений
GET	/api/message/status/{messageId}	Получение статуса сообщения
GET	/api/message/history/{userId}	История сообщений с фильтрацией
GET	/api/message/failed/{userId}	Список неудачных сообщений
GET	/api/message/statistics/{userId}	Статистика по сообщениям
GET	/api/message/details/{messageId}	Детальная информация о сообщении
DELETE	/api/message/clean?daysToKeep=30	Очистка старых сообщений

Конечные точки создание и удаление шаблонов 

Операция	HTTP метод	Эндпоинт	Тело запроса
Создание шаблона	POST	/api/template	UserTamplate
Получение списка шаблонов	GET	/api/template/{userId}	Нет (параметр в URL)
Удаление шаблона	DELETE	/api/template/{templateId}	Нет (параметр в URL)
