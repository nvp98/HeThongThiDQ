using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class NoiDungDt
{
    public int Idnd { get; set; }

    public string? MaNd { get; set; }

    public string? NoiDung { get; set; }

    public string? VideoNd { get; set; }

    public string? ImageNd { get; set; }

    public int? Bplid { get; set; }

    public int? Lvdtid { get; set; }

    public int? Idctlvdt { get; set; }

    public int? ThoiLuongDt { get; set; }

    public string? FileDinhKem { get; set; }

    public DateOnly? NgayTao { get; set; }

    public int? IsOrder { get; set; }

    public int? IsNq { get; set; }

    public int? IdnguonGv { get; set; }

    public int? IdhoatDongDt { get; set; }

    public int? IdphuongPhapDt { get; set; }

    public int? IdnhomNl { get; set; }

    public int? IdphanLoaiDt { get; set; }

    public int? LoaiNcdtId { get; set; }

    public bool? IsDelete { get; set; }

    public virtual ICollection<DeThi> DeThis { get; set; } = new List<DeThi>();

    public virtual NhomNlkccd? IdnhomNlNavigation { get; set; }

    public virtual ICollection<LopHoc> LopHocs { get; set; } = new List<LopHoc>();

    public virtual LinhVucDt? Lvdt { get; set; }
}
