using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class CauHoi
{
    public int Idch { get; set; }

    public string? NoiDungCh { get; set; }

    public string? DapAnA { get; set; }

    public string? DapAnB { get; set; }

    public string? DapAnC { get; set; }

    public string? DapAnD { get; set; }

    public int? Iddađung { get; set; }

    public int? Idnd { get; set; }

    public int? Gvid { get; set; }

    public string? MaCh { get; set; }

    public bool? IsDao { get; set; }

    public virtual ICollection<CauHoiDeThi> CauHoiDeThis { get; set; } = new List<CauHoiDeThi>();
}
