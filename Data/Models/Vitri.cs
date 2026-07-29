using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class Vitri
{
    public int IdviTri { get; set; }

    public string? TenViTri { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
