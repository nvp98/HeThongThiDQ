using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KhungNangLuc
{
    public int Idnl { get; set; }

    public string? TenNl { get; set; }

    public int? IdloaiNl { get; set; }

    public int? Idvt { get; set; }

    public int? Idpb { get; set; }

    public int? DinhMuc { get; set; }

    public int? IsDanhGia { get; set; }

    public int? OrderBy { get; set; }

    public int? IsDuyet { get; set; }
}
