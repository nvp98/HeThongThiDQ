using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtLoaiQt
{
    public int Idloai { get; set; }

    public string? TenLoai { get; set; }

    public virtual ICollection<QtNoiDungQt> QtNoiDungQts { get; set; } = new List<QtNoiDungQt>();
}
