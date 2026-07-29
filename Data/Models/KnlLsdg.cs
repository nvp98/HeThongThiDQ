using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class KnlLsdg
{
    public int Idls { get; set; }

    public int? Nvid { get; set; }

    public int? Vtid { get; set; }

    public DateOnly? ThangDg { get; set; }

    public DateOnly? NgayDggn { get; set; }

    public int? Dat { get; set; }

    public int? Kdat { get; set; }

    public int? Vuot { get; set; }

    public int? Kdgia { get; set; }

    public int? Chuadg { get; set; }

    public int? Tongnl { get; set; }

    public DateOnly? NgayTuDggn { get; set; }

    public int? Dattudg { get; set; }

    public int? Kdattudg { get; set; }

    public int? Vuottudg { get; set; }

    public int? KdgiaTuDg { get; set; }

    public int? ChuadgtuDg { get; set; }

    public int? Dattudglan1 { get; set; }

    public int? Kdattudglan1 { get; set; }

    public int? Vuottudglan1 { get; set; }

    public int? KdgiaTuDglan1 { get; set; }

    public int? ChuadgtuDglan1 { get; set; }

    public DateOnly? NgayDggnlan1 { get; set; }
}
