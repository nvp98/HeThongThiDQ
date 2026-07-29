using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class NqChuong
{
    public int Id { get; set; }

    public string? MaChuong { get; set; }

    public string? TenChuong { get; set; }

    public int? IsOrder { get; set; }

    public int? Sltg { get; set; }

    public int? Slht { get; set; }

    public int? Slhtfile { get; set; }
}
