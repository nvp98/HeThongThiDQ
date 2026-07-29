using System.ComponentModel.DataAnnotations;

namespace HeThongThiDQ.Models
{
    public class LoginValidation
    {
        public int ID { get; set; }

        [RegularExpression(@"[A-Za-z0-9]*$", ErrorMessage = "Tài khoản chứa kí tự đặc biệt")]
        [MaxLength(15, ErrorMessage = "Vượt quá số kí tự 15")]
        public string? MaNV { get; set; }

        [RegularExpression(@"[A-Za-z0-9]*$", ErrorMessage = "Tài khoản chứa kí tự đặc biệt")]
        [MaxLength(15, ErrorMessage = "Vượt quá số kí tự 15")]
        public string? SoDienThoai { get; set; }

        [DataType(DataType.Password)]
        [MaxLength(50, ErrorMessage = "Vượt quá số kí tự 50")]
        public string? MatKhau { get; set; }

        [DataType(DataType.Password)]
        [MaxLength(50, ErrorMessage = "Vượt quá số kí tự 50")]
        [Compare("MatKhau", ErrorMessage = "Xác nhận không khớp")]
        public string? NhapLaiMatKhau { get; set; }

        public string? MatKhauCu { get; set; }
        public string? HoTen { get; set; }
        public int IDQuyen { get; set; }
        public int IDQuyenKNL { get; set; }
        public int IDPhongBan { get; set; }
        public string? Email { get; set; }
    }
}
