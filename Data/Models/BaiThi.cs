using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class BaiThi
{
    public int IdbaiThi { get; set; }

    public int? Idlh { get; set; }

    public int? IddeThi { get; set; }

    public int? Idnd { get; set; }

    public int? Idnv { get; set; }

    public int? IdphongBan { get; set; }

    public int? IdviTri { get; set; }

    public double? DiemSo { get; set; }

    public DateOnly? NgayThi { get; set; }

    public bool? TinhTrang { get; set; }

    public int? LanThi { get; set; }

    public bool? Flag { get; set; }

    public int? ThoiGianThi { get; set; }

    public DateTime? GioBatDau { get; set; }

    public DateTime? GioKetThuc { get; set; }
}
