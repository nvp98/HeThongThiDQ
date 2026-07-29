using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtCtbaiKiemTra
{
    public int Idctkt { get; set; }

    public int? IdkiemTra { get; set; }

    public int? IdcauHoi { get; set; }

    public int? DapAnHv { get; set; }

    public string? Iddađung { get; set; }

    public double? Diem { get; set; }

    public virtual QtBaiKiemTra? IdkiemTraNavigation { get; set; }
}
