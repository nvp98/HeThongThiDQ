namespace HeThongThiDQ.Models;

public class ExamSubmitMessage
{
    public Guid   MessageId     { get; set; } = Guid.NewGuid();
    public int    IDLH          { get; set; }
    public int    IDDeThi       { get; set; }
    public int    IDND          { get; set; }
    public int    IDNV          { get; set; }
    public int    IDPhongBan    { get; set; }
    public int    IDViTri       { get; set; }
    public int    LanThi        { get; set; }
    public int    ThoiGianSec   { get; set; }
    public long   TGBDLamBaiThi { get; set; }
    public double DiemSo        { get; set; }
    public List<AnswerRecord> Answers { get; set; } = new();
}

public class AnswerRecord
{
    public int      IDCH         { get; set; }
    public int?     DapAnDung    { get; set; }
    public int?     DapAnNv      { get; set; }
    public double   Diem         { get; set; }
    public DateTime? ThoiGianChon { get; set; }
}
