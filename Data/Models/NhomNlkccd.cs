using System;
using System.Collections.Generic;

namespace HeThongThiDQ.Data.Models;

public partial class NhomNlkccd
{
    public int Id { get; set; }

    public string? NoiDung { get; set; }

    public virtual ICollection<NoiDungDtkccd> NoiDungDtkccds { get; set; } = new List<NoiDungDtkccd>();

    public virtual ICollection<NoiDungDt> NoiDungDts { get; set; } = new List<NoiDungDt>();
}
