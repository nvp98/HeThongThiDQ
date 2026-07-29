using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KccdCtbaiThi
{
    public int Idct { get; set; }

    public int? Idbt { get; set; }

    public int? IdcauHoi { get; set; }

    public int? DapAnHv { get; set; }

    public int? Iddađung { get; set; }

    public double? Diem { get; set; }

    public virtual KccdBaiThi? IdbtNavigation { get; set; }

    public virtual KccdCauHoi? IdcauHoiNavigation { get; set; }
}
