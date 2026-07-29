using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QtVanBanLq
{
    public int Idvb { get; set; }

    public int? Qthdid { get; set; }

    public int? IdqtLienQuan { get; set; }

    public virtual QtNoiDungQt? Qthd { get; set; }
}
