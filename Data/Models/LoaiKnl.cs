using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class LoaiKnl
{
    public int Idloai { get; set; }

    public string? TenLoai { get; set; }

    public int? Idvt { get; set; }

    public int? OrderBy { get; set; }
}
