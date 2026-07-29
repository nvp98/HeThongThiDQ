using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class Ctlvdt
{
    public int Idctlvdt { get; set; }

    public string? TenCtlvdt { get; set; }

    public int? Lvdtid { get; set; }

    public virtual LinhVucDt? Lvdt { get; set; }
}
