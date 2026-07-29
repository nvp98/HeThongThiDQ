using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KccdCauHoi
{
    public int Idch { get; set; }

    public string? NoiDungCh { get; set; }

    public string? DapAnA { get; set; }

    public string? DapAnB { get; set; }

    public string? DapAnC { get; set; }

    public string? DapAnD { get; set; }

    public int? Iddađung { get; set; }

    public int? Kccdid { get; set; }

    public int? DeThiId { get; set; }

    public virtual KccdDeThi? DeThi { get; set; }

    public virtual ICollection<KccdCtbaiThi> KccdCtbaiThis { get; set; } = new List<KccdCtbaiThi>();
}
