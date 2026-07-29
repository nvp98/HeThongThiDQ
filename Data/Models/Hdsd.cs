using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class Hdsd
{
    public int Id { get; set; }

    public string? MoTa { get; set; }

    public string? FilePath { get; set; }

    public int? OrderBy { get; set; }
}
