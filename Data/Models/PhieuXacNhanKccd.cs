using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class PhieuXacNhanKccd
{
    public int Id { get; set; }

    public int? DeNghiDtid { get; set; }

    public int? HocVienId { get; set; }

    public string? HvtruocDatDuoc { get; set; }

    public string? HvtruocCanCaiThien { get; set; }

    public string? HvsauDatDuoc { get; set; }

    public string? HvsauCanCaiThien { get; set; }

    public double? GvlyThuyetTruocDt { get; set; }

    public double? GvthucHanhTruocDt { get; set; }

    public string? GvnhanXetLttruocDt { get; set; }

    public string? GvnhanXetThtruocDt { get; set; }

    public double? GvlyThuyetSauDt { get; set; }

    public double? GvthucHanhSauDt { get; set; }

    public string? GvnhanXetLtsauDt { get; set; }

    public string? GvnhanXetThsauDt { get; set; }

    public int? GvketLuan { get; set; }

    public string? GvketLuanYkienKhac { get; set; }

    public int? HvdeXuat { get; set; }

    public string? HvdeXuatKhac { get; set; }

    public DateTime? HvngayXacNhan { get; set; }

    public int? IdtinhTrang { get; set; }

    public int? TinhTrangThi { get; set; }

    public virtual DeNghiKccd? DeNghiDt { get; set; }
}
