using HeThongThiDQ.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace HeThongThiDQ.Services;

public class RabbitMqPublisher : IExamQueuePublisher, IDisposable
{
    public const string QueueName = "exam.submit";

    private readonly IConnection _conn;
    private readonly IModel      _channel;
    private readonly object      _lock = new();

    public RabbitMqPublisher(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(config.GetConnectionString("RabbitMQ")!),
            AutomaticRecoveryEnabled    = true,
            NetworkRecoveryInterval     = TimeSpan.FromSeconds(5),
            DispatchConsumersAsync      = true,
        };
        _conn    = factory.CreateConnection("publisher");
        _channel = _conn.CreateModel();
        _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
    }

    public Task PublishAsync(ExamSubmitMessage message)
    {
        var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        props.MessageId  = message.MessageId.ToString();

        lock (_lock)
        {
            _channel.BasicPublish("", QueueName, props, body);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _conn?.Close();    } catch { }
    }
}
