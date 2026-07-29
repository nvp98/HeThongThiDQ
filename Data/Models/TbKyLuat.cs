using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class TbKyLuat
{
    public int Id { get; set; }

    public string? TbTieuDe { get; set; }

    public int? TbThang { get; set; }

    public int? TbNam { get; set; }

    public string? TbFile { get; set; }
}
