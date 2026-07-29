using System;

namespace HeThongThiDQ.Models
{
    public class NotificationView
    {
        public int ID { get; set; }
        public string? NoiDungTB { get; set; }
        public DateTime NgayTB { get; set; }
        public int? TinhTrang { get; set; }
    }
}
