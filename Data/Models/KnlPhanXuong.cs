using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KnlPhanXuong
{
    public int Id { get; set; }

    public string? MaPx { get; set; }

    public string? TenPx { get; set; }

    public int? IdphongBan { get; set; }

    public int? Idkhoi { get; set; }
}
