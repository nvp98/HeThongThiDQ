using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class ThongBao
{
    public int Id { get; set; }

    public string? NoiDungTb { get; set; }

    public DateOnly? NgayTb { get; set; }

    public int? TinhTrang { get; set; }
}
