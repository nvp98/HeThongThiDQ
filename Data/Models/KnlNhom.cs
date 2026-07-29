using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KnlNhom
{
    public int Idnhom { get; set; }

    public string? TenNhom { get; set; }

    public string? MaNhom { get; set; }

    public int? IdphongBan { get; set; }

    public int? IdphanXuong { get; set; }

    public int? Idkhoi { get; set; }
}
