using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class LopHoc
{
    public int Idlh { get; set; }

    public string? MaLh { get; set; }

    public string? TenLh { get; set; }

    public int? Ndid { get; set; }

    public int? QuyDt { get; set; }

    public int? NamDt { get; set; }

    public DateTime? Tgbdlh { get; set; }

    public DateTime? Tgktlh { get; set; }

    public int? Gvid { get; set; }

    public int? IddeThi { get; set; }

    public bool? ToChucThi { get; set; }

    public int? NcdtId { get; set; }

    public int? CtdtId { get; set; }

    public string? NoiDungTrichYeu { get; set; }

    public string? DiaDiemDt { get; set; }

    public string? ThoiLuongDt { get; set; }

    public int? DonViGv { get; set; }

    public int? BoPhanId { get; set; }

    public int? NguoiTaoId { get; set; }

    public int? NguoiKiemTraId { get; set; }

    public int? TinhTrang { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayKiemTra { get; set; }

    public int? IsCoCtdt { get; set; }

    public virtual NoiDungDt? Nd { get; set; }

    public virtual ICollection<XnhocTap> XnhocTaps { get; set; } = new List<XnhocTap>();
}
