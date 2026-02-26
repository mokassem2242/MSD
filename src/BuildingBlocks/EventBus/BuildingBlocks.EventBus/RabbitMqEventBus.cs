using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingBlocks.EventBus;

/// <summary>
/// RabbitMQ implementation of IEventBus for distributed publish/subscribe.
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IDisposable
{
    private const string ExchangeType = "topic";

    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;
    private readonly IModel _publishChannel;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly object _publishLock = new();
    private readonly ConcurrentDictionary<string, IModel> _subscriptionChannels = new();
    private bool _disposed;

    public RabbitMqEventBus(RabbitMqOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ValidateOptions(_options);

        var connectionFactory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        _connection = connectionFactory.CreateConnection();
        _publishChannel = _connection.CreateModel();
        _publishChannel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType,
            durable: true,
            autoDelete: false);

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public Task PublishAsync<T>(T integrationEvent) where T : IIntegrationEvent
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        var routingKey = BuildRoutingKey(eventType);
        var payload = JsonSerializer.SerializeToUtf8Bytes(integrationEvent, eventType, _jsonSerializerOptions);

        lock (_publishLock)
        {
            var properties = _publishChannel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2;
            properties.Type = eventType.FullName;
            properties.MessageId = integrationEvent.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(
                new DateTimeOffset(integrationEvent.OccurredOn).ToUnixTimeSeconds());

            _publishChannel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: payload);
        }

        return Task.CompletedTask;
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : IIntegrationEvent
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(T);
        var queueName = BuildQueueName(eventType);

        if (_subscriptionChannels.ContainsKey(queueName))
        {
            return;
        }

        var channel = _connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType,
            durable: true,
            autoDelete: false);
        channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        channel.QueueBind(
            queue: queueName,
            exchange: _options.ExchangeName,
            routingKey: BuildRoutingKey(eventType));
        channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<T>(
                    eventArgs.Body.ToArray(),
                    _jsonSerializerOptions);

                if (message is null)
                {
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await handler(message);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch
            {
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        if (!_subscriptionChannels.TryAdd(queueName, channel))
        {
            channel.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var (_, channel) in _subscriptionChannels)
        {
            try
            {
                channel.Dispose();
            }
            catch
            {
                // Ignore dispose errors while shutting down.
            }
        }

        try
        {
            _publishChannel.Dispose();
        }
        catch
        {
            // Ignore dispose errors while shutting down.
        }

        try
        {
            _connection.Dispose();
        }
        catch
        {
            // Ignore dispose errors while shutting down.
        }

        _disposed = true;
    }

    private static void ValidateOptions(RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.HostName))
        {
            throw new InvalidOperationException("RabbitMq:HostName is required.");
        }

        if (options.Port <= 0)
        {
            throw new InvalidOperationException("RabbitMq:Port must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(options.ExchangeName))
        {
            throw new InvalidOperationException("RabbitMq:ExchangeName is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ServiceName))
        {
            throw new InvalidOperationException("RabbitMq:ServiceName is required.");
        }
    }

    private static string BuildRoutingKey(Type eventType)
    {
        return eventType.Name;
    }

    private string BuildQueueName(Type eventType)
    {
        return $"{Sanitize(_options.ServiceName)}.{Sanitize(eventType.Name)}";
    }

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')
            {
                builder.Append(ch);
            }
            else
            {
                builder.Append('-');
            }
        }

        return builder.ToString();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
