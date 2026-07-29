using System;

namespace HeThongThiDQ.Models
{
    public class EClassroomValidation
    {
        public int IDHT { get; set; }
        public int PBID { get; set; }
        public int NVID { get; set; }
        public string? MaNV { get; set; }
        public string? HoTenHV { get; set; }
        public string? TenPB { get; set; }
        public string? TenVT { get; set; }
        public int LHID { get; set; }
        public string? TenLH { get; set; }
        public string? MaLH { get; set; }
        public string? TenND { get; set; }
        public string? LinhVuc { get; set; }
        public string? VideoLH { get; set; }
        public string? ImageLH { get; set; }
        public string? FileCTDT { get; set; }
        public DateTime TGBDLH { get; set; }
        public DateTime TGKTLH { get; set; }
        public DateTime NgayTG { get; set; }
        public DateTime NgayHT { get; set; }
        public bool XNTG { get; set; }
        public bool XNHT { get; set; }
        public int IDBaiThi { get; set; }
        public string? TenBaiThi { get; set; }
        public bool ToChucThi { get; set; }
        public int? IDDeThi { get; set; }
        public int ThiNhieuLan { get; set; }
        public int BaiThiCount { get; set; }
        public int LastIDBaiThi { get; set; }
        public double? LastDiemSo { get; set; }
        public double? TongDiem { get; set; }
        public int SoCauDung { get; set; }
        public int TongSoCau { get; set; }
    }

    public class HistoryTestView
    {
        public int IDNV { get; set; }
        public string? HoTen { get; set; }
        public int IDLH { get; set; }
        public double? DiemThi { get; set; }
        public int ThoiGianThi { get; set; }
        public DateTime? NgayThi { get; set; }
        public int IDBaiThi { get; set; }
    }
}
