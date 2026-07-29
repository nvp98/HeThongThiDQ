using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtPhanQuyen
{
    public int IdphanQuyen { get; set; }

    public int? Qthdid { get; set; }

    public int? Idvtknl { get; set; }

    public int? Dkid { get; set; }

    public virtual QtDinhKy? Dk { get; set; }

    public virtual QtNoiDungQt? Qthd { get; set; }
}
