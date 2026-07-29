using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KnlKq
{
    public int Idkq { get; set; }

    public int? Idnv { get; set; }

    public int? Idnl { get; set; }

    public int? Idnvdg { get; set; }

    public int? DiemDg { get; set; }

    public DateOnly? ThangDg { get; set; }

    public DateOnly? NgayDg { get; set; }

    public string? Note { get; set; }

    public int? DiemDm { get; set; }

    public int? Kqid { get; set; }

    public int? Vtid { get; set; }

    public int? DiemTuDg { get; set; }

    public DateOnly? NgayTuDg { get; set; }

    public int? IdnguoiDgLan1 { get; set; }

    public int? DiemDgLan1 { get; set; }

    public DateOnly? NgayDgLan1 { get; set; }
}
