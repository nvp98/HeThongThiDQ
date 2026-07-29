using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtNoiDungQt
{
    public int Idqthd { get; set; }

    public string? MaHieu { get; set; }

    public string? TenQthd { get; set; }

    public int? IdloaiQthd { get; set; }

    public int? IdphongBan { get; set; }

    public int? Idlvdt { get; set; }

    public DateOnly? NgayHieuLuc { get; set; }

    public DateOnly? NgayHetHieuLuc { get; set; }

    public double? DiemChuan { get; set; }

    public int? LanCapNhat { get; set; }

    public string? NoiDungCapNhat { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public int? TinhTrang { get; set; }

    public virtual QtLoaiQt? IdloaiQthdNavigation { get; set; }

    public virtual ICollection<QtBaiKiemTra> QtBaiKiemTras { get; set; } = new List<QtBaiKiemTra>();

    public virtual ICollection<QtCauHoiQt> QtCauHoiQts { get; set; } = new List<QtCauHoiQt>();

    public virtual ICollection<QtFileQt> QtFileQts { get; set; } = new List<QtFileQt>();

    public virtual ICollection<QtPhanQuyen> QtPhanQuyens { get; set; } = new List<QtPhanQuyen>();

    public virtual ICollection<QtVanBanLq> QtVanBanLqs { get; set; } = new List<QtVanBanLq>();
}
