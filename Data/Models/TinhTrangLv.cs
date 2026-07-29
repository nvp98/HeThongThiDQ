using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class TinhTrangLv
{
    public int Id { get; set; }

    public string? TinhTrangLv1 { get; set; }

    public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
}
