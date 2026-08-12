using HeThongThiDQ.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace HeThongThiDQ.Services;

public class RabbitMqPublisher : IExamQueuePublisher, IDisposable
{
    public const string QueueName = "exam.submit";

    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly object _lock = new();

    private IConnection? _conn;
    private IModel?      _channel;

    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _logger  = logger;
        _factory = new ConnectionFactory
        {
            Uri = new Uri(config.GetConnectionString("RabbitMQ")!),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval  = TimeSpan.FromSeconds(5),
            DispatchConsumersAsync   = true,
        };
    }

    // Kết nối lười — chỉ tạo khi PublishAsync được gọi lần đầu
    private IModel GetChannel()
    {
        lock (_lock)
        {
            if (_channel is { IsOpen: true }) return _channel;

            _conn?.Dispose();
            _conn    = _factory.CreateConnection("publisher");
            _channel = _conn.CreateModel();
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
            _logger.LogInformation("[RabbitMQ] Publisher đã kết nối");
            return _channel;
        }
    }

    public Task PublishAsync(ExamSubmitMessage message)
    {
        var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = GetChannel().CreateBasicProperties();
        props.Persistent = true;
        props.MessageId  = message.MessageId.ToString();

        lock (_lock)
        {
            GetChannel().BasicPublish("", QueueName, props, body);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _conn?.Close();    } catch { }
    }
}
