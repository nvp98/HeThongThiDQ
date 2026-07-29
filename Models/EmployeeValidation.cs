using System;

namespace HeThongThiDQ.Models
{
    public class EmployeeValidation
    {
        public int ID { get; set; }
        public string? MaNV { get; set; }
        public string? HoTen { get; set; }
        public int IDPhongBan { get; set; }
        public string? PhongBan { get; set; }
        public string? TenQuyen { get; set; }
        public string? ChucVu { get; set; }
        public string? Email { get; set; }
        public bool IsGV { get; set; }
        public string? ChuDeThi { get; set; }
        public string? TongCongTy { get; set; }
        public string? DonViC2 { get; set; }
        public string? DoViC4 { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? NgaySinhStr { get; set; }
        public string? SoDienThoai { get; set; }
    }

    public class JqueryDatatableParam
    {
        public string? sEcho { get; set; }
        public string? sSearch { get; set; }
        public int iDisplayLength { get; set; }
        public int iDisplayStart { get; set; }
        public int iColumns { get; set; }
        public int iSortCol_0 { get; set; }
        public string? sSortDir_0 { get; set; }
        public int iSortingCols { get; set; }
        public string? sColumns { get; set; }
    }
}
