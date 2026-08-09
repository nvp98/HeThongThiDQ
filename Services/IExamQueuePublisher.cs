using HeThongThiDQ.Models;

namespace HeThongThiDQ.Services;

public interface IExamQueuePublisher
{
    Task PublishAsync(ExamSubmitMessage message);
}
