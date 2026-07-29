using System.Collections.Generic;

namespace HeThongThiDQ.Models
{
    public class StatisticValidation
    {
        public int SLHV { get; set; }
        public int SLLH { get; set; }
        public int SLNDDT { get; set; }
        public float TLHT { get; set; }
        public int SLVTKNL { get; set; }
        public int SLDG { get; set; }
        public int SLDT { get; set; }
        public int SLTGDT { get; set; }
        public int SLCH { get; set; }
        public int SLuotDT { get; set; }
        public int SLGV { get; set; }
    }

    public class ChartLVDT
    {
        public string? TenLV { get; set; }
        public float GiaTri { get; set; }
        public float TLGiaTri { get; set; }
    }

    public class ChartNDDT
    {
        public List<string>? ListTenBP { get; set; }
        public List<LVDT>? ListLVDT { get; set; }
    }

    public class LVDT
    {
        public string? name { get; set; }
        public List<int>? data { get; set; }
    }

    public class ChartKQHT
    {
        public int Dat { get; set; }
        public int KhongDat { get; set; }
        public List<KQDT>? ListKQD { get; set; }
        public List<KQDT>? ListKQKD { get; set; }
    }

    public class KQDT
    {
        public string? name { get; set; }
        public int data { get; set; }
    }
}
