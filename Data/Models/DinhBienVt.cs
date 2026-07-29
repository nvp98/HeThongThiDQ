using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class DinhBienVt
{
    public int Id { get; set; }

    public int? Idvt { get; set; }

    public int? SldinhBien { get; set; }

    public int? NstapSu { get; set; }

    public int? Nstrong { get; set; }

    public DateOnly? TgboNhiem { get; set; }

    public int? Nsvtkhac { get; set; }

    public string? GhiChu { get; set; }

    public DateTime? NgayTao { get; set; }
}
