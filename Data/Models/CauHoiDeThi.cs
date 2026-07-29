using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class CauHoiDeThi
{
    public int IdcauHoiDeThi { get; set; }

    public int? IdcauHoi { get; set; }

    public int? IddeThi { get; set; }

    public double? Diem { get; set; }

    public virtual CauHoi? IdcauHoiNavigation { get; set; }

    public virtual DeThi? IddeThiNavigation { get; set; }
}
