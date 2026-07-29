using System;

namespace HeThongThiDQ.Models
{
    public class TestValidation
    {
        public int IDCH { get; set; }
        public string? NoiDungCH { get; set; }
        public int IDLH { get; set; }
        public int IDDeThi { get; set; }
        public int IDND { get; set; }
        public int IDNV { get; set; }
        public int Answer { get; set; }
        public string? DapAnA { get; set; }
        public string? DapAnB { get; set; }
        public string? DapAnC { get; set; }
        public string? DapAnD { get; set; }
        public int IDDADung { get; set; }
        public string? DapAnDung { get; set; }
        public double Diem { get; set; }
        public bool IsDao { get; set; }
        public DateTime GioBatDau { get; set; }
        public DateTime GioKetThuc { get; set; }
        public double ThoiGianThi { get; set; }
    }
}
