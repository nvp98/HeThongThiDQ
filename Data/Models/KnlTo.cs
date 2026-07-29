using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KnlTo
{
    public int Idto { get; set; }

    public string? TenTo { get; set; }

    public string? MaTo { get; set; }

    public int? IdphongBan { get; set; }

    public int? IdphanXuong { get; set; }

    public int? Idkhoi { get; set; }
}
