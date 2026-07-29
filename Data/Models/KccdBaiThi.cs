using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KccdBaiThi
{
    public int Id { get; set; }

    public int? Idnv { get; set; }

    public int? Kccdid { get; set; }

    public double? Diem { get; set; }

    public DateOnly? NgayThi { get; set; }

    public int? TinhTrang { get; set; }

    public int? DeThiId { get; set; }

    public int? LanThi { get; set; }

    public int? DeNghiId { get; set; }

    public virtual ICollection<KccdCtbaiThi> KccdCtbaiThis { get; set; } = new List<KccdCtbaiThi>();
}
