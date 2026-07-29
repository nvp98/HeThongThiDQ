using System;
using System.ComponentModel.DataAnnotations;

namespace HeThongThiDQ.Models
{
    public class ManageETContentValidation
    {
        public int IDND { get; set; }

        [Required(ErrorMessage = "Nhập mã Nội dung Đào tạo")]
        public string? MaND { get; set; }

        [Required(ErrorMessage = "Nhập Nội dung Đào tạo")]
        public string? NoiDung { get; set; }

        public string? VideoND { get; set; }
        public string? ImageND { get; set; }
        public string? LinhVuc { get; set; }
        public int BPLID { get; set; }
        public string? BPLNC { get; set; }
        public int LVDTID { get; set; }
        public int IDCTLVDT { get; set; }
        public string? LVChiTiet { get; set; }
        public int ThoiLuongDT { get; set; }
        public int? SLLH { get; set; }
        public string? FileDinhKem { get; set; }
        public DateTime? NgayTao { get; set; }

        public IFormFile? PDFEduFile { get; set; }
    }
}
