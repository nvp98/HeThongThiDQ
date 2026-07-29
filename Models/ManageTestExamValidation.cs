using System;

namespace HeThongThiDQ.Models
{
    public class ManageTestExamValidation
    {
        public int IDDeThi { get; set; }
        public string? MaDe { get; set; }
        public string? TenDe { get; set; }
        public double DiemChuan { get; set; }
        public int TongSoCau { get; set; }
        public int ThoiGianLamBai { get; set; }
        public int IDND { get; set; }
        public string? TenND { get; set; }
        public int IDLH { get; set; }
        public string? TenLH { get; set; }
        public string? NoiDung { get; set; }
        public string? HoTen { get; set; }
        public int GVID { get; set; }
        public DateTime TGBDLH { get; set; }
        public DateTime TGKTLH { get; set; }
        public bool IsGV { get; set; }
    }

    public class ManageQuestionValidation
    {
        public int IDCH { get; set; }
        public string? MaCH { get; set; }
        public string? NoiDungCH { get; set; }
        public string? DapAnA { get; set; }
        public string? DapAnB { get; set; }
        public string? DapAnC { get; set; }
        public string? DapAnD { get; set; }
        public int IDDADung { get; set; }
        public int? IDDAHocVien { get; set; }
        public string? DapAnDung { get; set; }
        public string? DapAnHV { get; set; }
        public int IDLVDT { get; set; }
        public int IDCTLVDT { get; set; }
        public double Diem { get; set; }
        public int IDCauHoiDeThi { get; set; }
        public int IDND { get; set; }
        public int GVID { get; set; }
        public bool IsDao { get; set; }
    }

    public class CauHoiDeThiValidation
    {
        public int IDCauHoi { get; set; }
        public double Diem { get; set; }
    }
}
