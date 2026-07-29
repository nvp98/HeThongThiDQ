using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtCauHoiQt
{
    public int Idch { get; set; }

    public string? NoiDungCh { get; set; }

    public string? DapAnA { get; set; }

    public string? DapAnB { get; set; }

    public string? DapAnC { get; set; }

    public string? DapAnD { get; set; }

    public int? Iddađung { get; set; }

    public int? Qthdid { get; set; }

    public virtual QtNoiDungQt? Qthd { get; set; }
}
