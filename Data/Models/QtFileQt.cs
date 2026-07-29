using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtFileQt
{
    public int Idfile { get; set; }

    public string? TenFile { get; set; }

    public int? Qthdid { get; set; }

    public string? FilePdf { get; set; }

    public int? OrderById { get; set; }

    public virtual QtNoiDungQt? Qthd { get; set; }
}
