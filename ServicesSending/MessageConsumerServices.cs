using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Confuguration.ServicesSending;
using Confuguration.Repository;
using Confuguration.Dbcontext;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
public class MessageConsumerServices : BackgroundService, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _queueName;
    private readonly IServiceProvider _servicesProvider;
    
    private readonly ILogger<MessageConsumerServices> _logger;

    public MessageConsumerServices(IConfiguration config, IServiceProvider serviceProvider, ILogger<MessageConsumerServices> logger)
    {
        _servicesProvider = serviceProvider;
        _logger = logger;
       
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"],
            Port = int.Parse(config["RabbitMQ:Port"]),
            UserName = config["RabbitMQ:UserName"],
            Password = config["RabbitMQ:Password"],
            VirtualHost = config["RabbitMQ:VirtualHost"]
        };
        _connection =  factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel =  _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _queueName = config["RabbitMQ:QueueName"];

        _channel.QueueDeclareAsync(queue: _queueName,
        durable: true,
        exclusive: false,
        autoDelete: false);

        _channel.BasicQosAsync(0,1,false).GetAwaiter().GetResult();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            var dto = JsonSerializer.Deserialize<MessagePublicationDto>(messageJson);

            _logger.LogInformation("Получено сообщение для MessageId {MessageId}", dto.MessageId);

            using (var scope = _servicesProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IUserHistoryRepository>();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<MessageConsumerServices>>();

                var dispather = scope.ServiceProvider.GetRequiredService<MessageDispatcher>();
            

            try
            {
                var id = new SentMessage {Id = dto.MessageId};
                var history = await repository.GetHistory(id);
                if(history == null)
                {
                        _logger.LogWarning("Запись с ID {MessageId} не найдена в БД", dto.MessageId);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }
                    history.Status = "Sending";
                    history.UpdatedAt = DateTime.UtcNow;
                    await repository.SaveChangesAsync();

                    var sendResult = await dispather.SendAsync(history.Channel, history.RecipientInfo, history.Content);

                    if (sendResult.IsSuccess)
                    {
                        history.Status = "Sent";
                        await repository.SaveChangesAsync();
                        _logger.LogInformation("Сообщение {MessageId} успешно отправлено", history.Id);

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else
                    {
                        var error = sendResult.Data?.ErrorMessage ?? "Неизвестная ошибка";
                        history.Status = "Failed";
                        history.UpdatedAt = DateTime.UtcNow;
                        await repository.SaveChangesAsync();

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        _logger.LogWarning("Сообщение {MessageId} не удалось отправить: {Error}", history.Id, error);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке сообщения {MessageId}", dto.MessageId);
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            }
        };

        await _channel.BasicConsumeAsync(queue: _queueName, autoAck: false, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        _channel.Dispose();
        _connection.Dispose();
    }
} 