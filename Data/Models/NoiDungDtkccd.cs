using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class NoiDungDtkccd
{
    public int Id { get; set; }

    public string? TenNd { get; set; }

    public int? NhomNlid { get; set; }

    public int? Lvdtid { get; set; }

    public int? PhongBanId { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<DeNghiKccd> DeNghiKccds { get; set; } = new List<DeNghiKccd>();

    public virtual NhomNlkccd? NhomNl { get; set; }
}
