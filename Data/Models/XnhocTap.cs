using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class XnhocTap
{
    public int Idht { get; set; }

    public int? Nvid { get; set; }

    public int? Lhid { get; set; }

    public DateOnly? NgayTg { get; set; }

    public DateOnly? NgayHt { get; set; }

    public bool? Xntg { get; set; }

    public bool? Xnht { get; set; }

    public int? Pbid { get; set; }

    public int? Vtid { get; set; }

    public double? DiemOnline { get; set; }

    public double? DiemLyThuyet { get; set; }

    public double? DiemThucHanh { get; set; }

    public double? DiemVanDap { get; set; }

    public int? KetLuan { get; set; }

    public int? Idnd { get; set; }

    public int? IdPhuongPhapDt { get; set; }

    public string? LyDoKhongTgia { get; set; }

    public int? TinhTrang { get; set; }

    public virtual LopHoc? Lh { get; set; }

    public virtual NhanVien? Nv { get; set; }
}
