using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class NhanVien
{
    public int Id { get; set; }

    public string? MaNv { get; set; }

    public string? MatKhau { get; set; }

    public string? HoTen { get; set; }

    public string? HoTenKhongDau { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? DiaChi { get; set; }

    public string? DienThoai { get; set; }

    public DateOnly? NgayVaoLam { get; set; }

    public int? IdphongBan { get; set; }

    public int? IdtinhTrangLv { get; set; }

    public int? IdviTri { get; set; }

    public bool? IsGv { get; set; }

    public int? Idquyen { get; set; }

    public int? Idvtknl { get; set; }

    public int? IdquyenKnl { get; set; }

    public string? MaViTri { get; set; }

    public int? Idkip { get; set; }

    public string? ChuKy { get; set; }

    public string? Email { get; set; }

    public string? TongCongTy { get; set; }

    public string? ChuDeThi { get; set; }

    public string? CongTyCapC2 { get; set; }

    public string? DonViToChucC4 { get; set; }

    public virtual PhongBan? IdphongBanNavigation { get; set; }

    public virtual TinhTrangLv? IdtinhTrangLvNavigation { get; set; }

    public virtual Vitri? IdviTriNavigation { get; set; }

    public virtual ICollection<KnlPheDuyetKnl> KnlPheDuyetKnlIdNguoiDuyetNavigations { get; set; } = new List<KnlPheDuyetKnl>();

    public virtual ICollection<KnlPheDuyetKnl> KnlPheDuyetKnlIdNguoiTaoNavigations { get; set; } = new List<KnlPheDuyetKnl>();

    public virtual ICollection<XnhocTap> XnhocTaps { get; set; } = new List<XnhocTap>();
}
