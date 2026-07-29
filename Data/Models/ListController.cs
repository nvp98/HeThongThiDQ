using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class ListController
{
    public int Id { get; set; }

    public string? Controller { get; set; }

    public string? Mota { get; set; }

    public int? IsActive { get; set; }

    public string? DsquyenCn { get; set; }
}
