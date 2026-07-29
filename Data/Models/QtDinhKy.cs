using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtDinhKy
{
    public int Iddk { get; set; }

    public int? MaDinhKy { get; set; }

    public string? TenDinhKy { get; set; }

    public virtual ICollection<QtPhanQuyen> QtPhanQuyens { get; set; } = new List<QtPhanQuyen>();
}
