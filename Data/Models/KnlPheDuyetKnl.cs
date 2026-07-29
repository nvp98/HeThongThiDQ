using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KnlPheDuyetKnl
{
    public int Id { get; set; }

    public int? Idvt { get; set; }

    public DateTime? NgayTrinhKy { get; set; }

    public int? IdNguoiTao { get; set; }

    public int? IdNguoiDuyet { get; set; }

    public DateTime? NgayDuyet { get; set; }

    public string? FileKnl { get; set; }

    public int? TinhTrang { get; set; }

    public virtual NhanVien? IdNguoiDuyetNavigation { get; set; }

    public virtual NhanVien? IdNguoiTaoNavigation { get; set; }
}
