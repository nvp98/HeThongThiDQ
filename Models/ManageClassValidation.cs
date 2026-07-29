using System;
using System.ComponentModel.DataAnnotations;

namespace HeThongThiDQ.Models
{
    public class ManageClassValidation
    {
        public int IDLH { get; set; }

        [Required(ErrorMessage = "Nhập mã lớp học")]
        public string? MaLH { get; set; }

        [Required(ErrorMessage = "Nhập tên lớp học")]
        public string? TenLH { get; set; }

        public int NDID { get; set; }
        public int QuyDT { get; set; }
        public int NamDT { get; set; }
        public DateTime TGBDLH { get; set; }
        public DateTime TGKTLH { get; set; }
        public string? MaND { get; set; }
        public string? TenND { get; set; }
        public string? FileCTDT { get; set; }
        public int LVDTID { get; set; }
        public string? LinhVuc { get; set; }
        public string? BPLNC { get; set; }
        public string? VideoLH { get; set; }
        public string? ImageLH { get; set; }
        public int GVID { get; set; }
        public string? MaGV { get; set; }
        public string? TenGV { get; set; }
        public int? IDPB { get; set; }
        public string? TenPhongBan { get; set; }
        public int SLHT { get; set; }
        public int TSLHV { get; set; }
        public int IDNV { get; set; }
        public bool? IsGV { get; set; }
        public int IDDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public bool IsThiNhieuLan { get; set; }

        // dùng cho file upload
        public IFormFile? FileUpload { get; set; }
    }
}
