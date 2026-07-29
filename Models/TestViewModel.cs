using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Models
{
    public class TestViewModel
    {
        public int IDBaiThi { get; set; }
        public int IDLH { get; set; }
        public int IDDeThi { get; set; }
        public int IDND { get; set; }
        public int IDNV { get; set; }
        public double DiemSo { get; set; }
        public DateTime NgayThi { get; set; }
        public bool TinhTrang { get; set; }
        public int LanThi { get; set; }
        public string? MaNV { get; set; }
        public string? HoTen { get; set; }
        public string? TenPhongBan { get; set; }
        public string? TenViTri { get; set; }
        public string? NoiDungDT { get; set; }
        public int ThoiGianLamBai { get; set; }
        public string? TenDeThi { get; set; }
        public List<ManageQuestionValidation> CauHois { get; set; } = new();
    }
}
