using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class DeThi
{
    public int IddeThi { get; set; }

    public string? MaDe { get; set; }

    public string? TenDe { get; set; }

    public double? DiemChuan { get; set; }

    public int? ThoiGianLamBai { get; set; }

    public int? Idnd { get; set; }

    public int? Gvid { get; set; }

    public int? TongSoCau { get; set; }

    public int? CtdtId { get; set; }

    public string? FileDeThi { get; set; }

    public virtual ICollection<CauHoiDeThi> CauHoiDeThis { get; set; } = new List<CauHoiDeThi>();

    public virtual NoiDungDt? IdndNavigation { get; set; }
}
