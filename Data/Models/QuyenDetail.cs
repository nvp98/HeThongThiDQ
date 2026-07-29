using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class QuyenDetail
{
    public int Id { get; set; }

    public int? Idquyen { get; set; }

    public int? Idcontroller { get; set; }

    public int? IdquyenCn { get; set; }

    public int? IsActive { get; set; }
}
