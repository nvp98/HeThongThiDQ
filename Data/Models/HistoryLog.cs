using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class HistoryLog
{
    public int Id { get; set; }

    public int? NhanVienId { get; set; }

    public string? DeviceId { get; set; }

    public DateTime? ExpireTime { get; set; }
}
