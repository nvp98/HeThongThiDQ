using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KccdDeThi
{
    public int Id { get; set; }

    public string? MaDe { get; set; }

    public string? TenDe { get; set; }

    public double? DiemChuan { get; set; }

    public int? Kccdid { get; set; }

    public virtual ICollection<KccdCauHoi> KccdCauHois { get; set; } = new List<KccdCauHoi>();
}
