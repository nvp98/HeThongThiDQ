using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class DeNghiKccd
{
    public int Id { get; set; }

    public int? NoiDungKccdid { get; set; }

    public int? LinhVucId { get; set; }

    public int? NhomNangLucId { get; set; }

    public int? PhongBanId { get; set; }

    public int? HuongDan1 { get; set; }

    public int? ViTriId1 { get; set; }

    public int? HuongDan2 { get; set; }

    public int? ViTriId2 { get; set; }

    public DateTime? NgayTao { get; set; }

    public int? TinhTrang { get; set; }

    public DateOnly? TuNgay { get; set; }

    public DateOnly? DenNgay { get; set; }

    public DateTime? NgayXn { get; set; }

    public int? IsKiemTra { get; set; }

    public int? DeThiId { get; set; }

    public virtual NoiDungDtkccd? NoiDungKccd { get; set; }

    public virtual ICollection<PhieuXacNhanKccd> PhieuXacNhanKccds { get; set; } = new List<PhieuXacNhanKccd>();
}
