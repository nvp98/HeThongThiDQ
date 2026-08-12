using HeThongThiDQ.Models;

namespace HeThongThiDQ.Services;

// Dùng khi RabbitMQ không được cấu hình (môi trường dev local)
public class NullExamQueuePublisher : IExamQueuePublisher
{
    private readonly ILogger<NullExamQueuePublisher> _logger;

    public NullExamQueuePublisher(ILogger<NullExamQueuePublisher> logger)
        => _logger = logger;

    public Task PublishAsync(ExamSubmitMessage message)
    {
        _logger.LogWarning(
            "[NullQueue] RabbitMQ không được cấu hình — bỏ qua nộp bài IDNV={IDNV} IDLH={IDLH}",
            message.IDNV, message.IDLH);
        return Task.CompletedTask;
    }
}
