using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtBaiKiemTra
{
    public int Idkt { get; set; }

    public int? Idnv { get; set; }

    public int? Qthdid { get; set; }

    public double? Diem { get; set; }

    public DateOnly? NgayHt { get; set; }

    public DateOnly? NgayKttt { get; set; }

    public int? LanKt { get; set; }

    public int? TinhTrang { get; set; }

    public int? LuotKiemTra { get; set; }

    public DateOnly? NgayKt { get; set; }

    public virtual ICollection<QtCtbaiKiemTra> QtCtbaiKiemTras { get; set; } = new List<QtCtbaiKiemTra>();

    public virtual QtNoiDungQt? Qthd { get; set; }
}
