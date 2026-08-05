namespace HeThongThiDQ.Models;

public class ExamSession
{
    public int IDDeThi { get; set; }
    public int IDND { get; set; }
    public int IDLH { get; set; }
    public int IDNV { get; set; }
    public int TotalTimeSec { get; set; }
    public long StartTimestamp { get; set; }
    public long EndTimestamp { get; set; }
    public long SavedAt { get; set; }
    public string DeviceToken { get; set; } = "";
    public List<int> QuestionOrder { get; set; } = new();
    public Dictionary<int, SessionAnswer> Answers { get; set; } = new();
}

public class SessionAnswer
{
    public int? AnswerId { get; set; }
    public long? ChosenAt { get; set; }
}
