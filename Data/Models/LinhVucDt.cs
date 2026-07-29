using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class LinhVucDt
{
    public int Idlvdt { get; set; }

    public string? TenLvdt { get; set; }

    public virtual ICollection<Ctlvdt> Ctlvdts { get; set; } = new List<Ctlvdt>();

    public virtual ICollection<NoiDungDt> NoiDungDts { get; set; } = new List<NoiDungDt>();
}
