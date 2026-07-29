using System;

namespace HeThongThiDQ.Models
{
    public class ConfirmEStudyValidation
    {
        public int IDHT { get; set; }
        public int NVID { get; set; }
        public int PBID { get; set; }
        public int VTID { get; set; }
        public string? MaNV { get; set; }
        public string? HoTenHV { get; set; }
        public string? TenPB { get; set; }
        public string? TenVT { get; set; }
        public int LHID { get; set; }
        public string? TenLH { get; set; }
        public DateTime TGBDLH { get; set; }
        public DateTime TGKTLH { get; set; }
        public string? TenND { get; set; }
        public string? LinhVuc { get; set; }
        public int TLDT { get; set; }
        public DateTime NgayTG { get; set; }
        public DateTime NgayHT { get; set; }
        public bool XNTG { get; set; }
        public bool XNHT { get; set; }
        public int IDBaiThi { get; set; }
        public string? TenBaiThi { get; set; }
        public string? PPDaoTao { get; set; }
        public int? IDPPDaoTao { get; set; }
        public string? KetQuaThi { get; set; }
        public string? DiemText { get; set; }
        public int SoLanThi { get; set; }
        public string? TongCongTy { get; set; }
        public string? CongTyCapC2 { get; set; }
        public string? DonViToChucC4 { get; set; }
        public string? Email { get; set; }
        public string? DienThoai { get; set; }
        public DateTime? NgaySinhNV { get; set; }
    }
}
