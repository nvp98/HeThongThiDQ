using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KnlDocBangKnl
{
    public int Id { get; set; }

    public int? Idnv { get; set; }

    public int? IdViTriKnl { get; set; }

    public int? IdNangLuc { get; set; }

    public int? TinhTrang { get; set; }

    public DateTime? NgayTao { get; set; }

    public bool? IsDelete { get; set; }
}
