using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class CtbaiThi
{
    public int Idctbt { get; set; }

    public int? IdbaiThi { get; set; }

    public int? IdcauHoi { get; set; }

    public int? IddapAnDung { get; set; }

    public int? IddapAnNv { get; set; }

    public double? Diem { get; set; }
}
