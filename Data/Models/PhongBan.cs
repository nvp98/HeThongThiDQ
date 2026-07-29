using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class PhongBan
{
    public int IdphongBan { get; set; }

    public string? TenPhongBan { get; set; }

    public string? MaPb { get; set; }

    public string? ApiMaPb { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
