using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Confuguration.ServicesSending;
using DTOResponseSending;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
public interface IMessagePublisher
{
    Task<bool> PublishMessageAsync(MessagePublicationDto dto);
}

public class RabbitMqMessageSender :  IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName;
    private readonly string _routingKey;
    private readonly ILogger<RabbitMqMessageSender> _logger;

    private RabbitMqMessageSender(IConnection connetion, IChannel channel, string exchangeName, string routingKey, ILogger<RabbitMqMessageSender> logger)
    {
        _connection = connetion;
        _channel = channel;
        _exchangeName = exchangeName;
        _routingKey = routingKey;
        _logger = logger;
    }

    public static async Task<RabbitMqMessageSender> CreateAsync(IConfiguration config, ILogger<RabbitMqMessageSender> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:HostName"],
            Port = int.Parse(config["RabbitMQ:Port"]),
            UserName = config["RabbitMQ:UserName"],
            Password = config["RabbitMQ:Password"],
            VirtualHost = config["RabbitMQ:VirtualHost"]
        };

        var connection = await factory.CreateConnectionAsync();

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true,
            outstandingPublisherConfirmationsRateLimiter: new ThrottlingRateLimiter(100)
        );
        var channel = await connection.CreateChannelAsync(channelOptions);
        var exchangeName = config["RabbitMQ:ExchangeName"];
        var routingKey = config["RabbitMQ:RoutingKey"];

        await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true);
        await channel.QueueDeclareAsync(queue: config["RabbitMQ:QueueName"],
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false);
        await channel.QueueBindAsync(queue: config["RabbitMQ:QueueName"],
                                        exchange: exchangeName,
                                        routingKey: routingKey);


        return new RabbitMqMessageSender(connection, channel, exchangeName, routingKey, logger);
        }
    
    
    public async Task<bool> PublishMessageAsync(MessagePublicationDto dto)
    {
        try
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(dto);
            var props = new BasicProperties
            {
                Persistent = true,
                DeliveryMode = DeliveryModes.Persistent
            };

           

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: _routingKey,
                mandatory: true,
                basicProperties: props,
                body: body
            );
            
            
            _logger.LogInformation("Сообщение для MessageId {MessageId} опубликовано в RabbitMQ", dto.MessageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка публикации сообщения для MessageId {MessageId}", dto.MessageId);
            return false;
        }
    }
        public void Dispose()
    {
        _channel?.CloseAsync();
        _connection?.CloseAsync();
        
    }
    
}