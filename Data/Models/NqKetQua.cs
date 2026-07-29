using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class NqKetQua
{
    public int Id { get; set; }

    public int? Nddtid { get; set; }

    public int? Xntg { get; set; }

    public int? Xnht { get; set; }

    public int? Xnhtfile { get; set; }

    public int? Idnv { get; set; }

    public DateOnly? NgayTg { get; set; }

    public DateOnly? NgayHt { get; set; }

    public int? TinhTrang { get; set; }
}
