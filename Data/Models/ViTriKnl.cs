using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class ViTriKnl
{
    public int Idvt { get; set; }

    public string? TenViTri { get; set; }

    public string? MaViTri { get; set; }

    public int? Idpb { get; set; }

    public int? Idkhoi { get; set; }

    public int? Idpx { get; set; }

    public int? Idnhom { get; set; }

    public int? Idto { get; set; }

    public string? FilePath { get; set; }

    public int? Idvtparent { get; set; }

    public int? TinhTrang { get; set; }
}
