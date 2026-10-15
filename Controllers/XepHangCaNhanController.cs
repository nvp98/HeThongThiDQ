using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class XepHangCaNhanController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IDistributedCache _cache;
        private readonly HomeController _home;
        private const string ControllerName = "XepHangCaNhan";

        private static readonly DistributedCacheEntryOptions _bxhOpts =
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) };

        public XepHangCaNhanController(ELEARNINGEntities db, MyAuthentication auth, IDistributedCache cache, HomeController home)
        {
            _db = db;
            _auth = auth;
            _cache = cache;
            _home = home;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.MyNVID = _auth.ID;
            return View();
        }

        // Danh sách lớp thi chính thức (có thể có lớp thi lại), mới nhất trước
        [HttpGet]
        public async Task<IActionResult> GetLopThi()
        {
            var list = await _db.LopHocs
                .Where(l => l.IsCoCtdt != 1)
                .OrderByDescending(l => l.Tgbdlh).ThenByDescending(l => l.Idlh)
                .Select(l => new { l.Idlh, TenLH = l.TenLh ?? ("Lớp thi " + l.Idlh) })
                .ToListAsync();
            return Json(list);
        }

        // loai = "thi"           → xếp hạng thi chính thức theo Điều 4 thể lệ, lọc theo lớp thi (idlh)
        // loai = "onluyen"       → xếp hạng ôn luyện tổng hợp (DiemOnLuyen desc, SoDe desc)
        // loai = "onluyen-ngay"  → ôn luyện trong ngày (tham số ngay = "yyyy-MM-dd")
        // loai = "onluyen-tuan"  → ôn luyện trong tuần (tham số tuan = "yyyy-Www")
        [HttpGet]
        public async Task<IActionResult> GetBangXepHang(string? loai, int? idlh, string? ngay, string? tuan)
        {
            string key = loai switch
            {
                "onluyen"      => "xephangcanhan:onluyen:v4",
                "onluyen-ngay" => $"xephangcanhan:onluyen-ngay:{ngay ?? DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd")}:v1",
                "onluyen-tuan" => $"xephangcanhan:onluyen-tuan:{tuan ?? GetCurrentWeekStr()}:v1",
                _              => $"xephangcanhan:thi:{idlh ?? 0}:v3"
            };

            try
            {
                var cached = await _cache.GetStringAsync(key);
                if (cached != null) return Content(cached, "application/json");
            }
            catch { }

            List<BxhRow> rows = loai switch
            {
                "onluyen"      => await BuildOnLuyenRankingAsync(),
                "onluyen-ngay" => await BuildOnLuyenNgayRankingAsync(
                                      DateOnly.TryParse(ngay, out var d) ? d : DateOnly.FromDateTime(DateTime.Today)),
                "onluyen-tuan" => await BuildOnLuyenTuanRankingAsync(ParseWeekStart(tuan)),
                _              => await BuildThiRankingAsync(idlh)
            };

            var json = JsonSerializer.Serialize(rows);
            try { await _cache.SetStringAsync(key, json, _bxhOpts); } catch { }
            return Content(json, "application/json");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private async Task<List<int>> GetOnLuyenLhIdsAsync()
            => await _db.LopHocs.Where(l => l.IsCoCtdt == 1).Select(l => l.Idlh).ToListAsync();

        private async Task<Dictionary<int, (string HoTen, string MaNV, string TenPB)>> GetNvInfoAsync(List<int> nvIds)
            => await (from n in _db.NhanViens
                      join pb in _db.PhongBans on n.IdphongBan equals pb.IdphongBan into pbj
                      from pb in pbj.DefaultIfEmpty()
                      where nvIds.Contains(n.Id)
                      select new { n.Id, n.HoTen, n.MaNv, TenPB = pb != null ? pb.TenPhongBan : null })
                     .ToDictionaryAsync(x => x.Id, x => (x.HoTen ?? "", x.MaNv ?? "", x.TenPB ?? ""));

        private static DateTime TruncateToSecond(DateTime d) => d.AddTicks(-(d.Ticks % TimeSpan.TicksPerSecond));

        // Tính stats ôn luyện tổng hợp (tất cả thời gian) cho mỗi NV trong các lớp olLhIds
        private async Task<Dictionary<int, (DateTime? HoanThanh, double Diem, int SoDe)>> GetOnLuyenStatsAsync(List<int> olLhIds)
        {
            if (olLhIds.Count == 0) return new();
            return (await _db.BaiThis.AsNoTracking()
                    .Where(b => b.Idnv != null && olLhIds.Contains(b.Idlh ?? 0))
                    .Select(b => new { Idnv = b.Idnv!.Value, Idlh = b.Idlh ?? 0, b.GioKetThuc, b.NgayThi, b.DiemSo })
                    .ToListAsync())
                .GroupBy(b => b.Idnv)
                .ToDictionary(g => g.Key, g =>
                {
                    var nopDauTheoDe = g.GroupBy(b => b.Idlh)
                        .Select(d => d.Min(b => b.GioKetThuc ?? b.NgayThi?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue))
                        .ToList();
                    DateTime? hoanThanh = olLhIds.Count > 0 && nopDauTheoDe.Count == olLhIds.Count
                        ? TruncateToSecond(nopDauTheoDe.Max())
                        : null;
                    return (HoanThanh: hoanThanh, Diem: g.Sum(b => b.DiemSo ?? 0), SoDe: nopDauTheoDe.Count);
                });
        }

        // ── Thi chính thức ──────────────────────────────────────────────────────

        private async Task<List<BxhRow>> BuildThiRankingAsync(int? idlh)
            => await XepHangThiChinhThuc.TinhAsync(_db, idlh ?? await XepHangThiChinhThuc.GetLopThiMoiNhatAsync(_db));

        // ── Ôn luyện tổng hợp ───────────────────────────────────────────────────

        private async Task<List<BxhRow>> BuildOnLuyenRankingAsync()
        {
            var olLhIds = await GetOnLuyenLhIdsAsync();
            if (olLhIds.Count == 0) return new();

            var enrolledNvIds = await _db.XnhocTaps
                .Where(x => x.Nvid != null && olLhIds.Contains(x.Lhid ?? 0))
                .Select(x => x.Nvid!.Value)
                .Distinct()
                .ToListAsync();
            if (enrolledNvIds.Count == 0) return new();

            var olPerNv = await GetOnLuyenStatsAsync(olLhIds);
            var nvMap = await GetNvInfoAsync(enrolledNvIds);

            return enrolledNvIds
                .Select(id =>
                {
                    olPerNv.TryGetValue(id, out var ol);
                    nvMap.TryGetValue(id, out var nv);
                    return new BxhRow
                    {
                        NVID = id,
                        HoTen = nv.HoTen ?? "",
                        MaNV = nv.MaNV ?? "",
                        TenPB = nv.TenPB ?? "",
                        HoanThanhOnLuyen = ol.HoanThanh,
                        DiemOnLuyen = ol.Diem,
                        SoDeOnLuyenHoanThanh = ol.SoDe
                    };
                })
                .OrderByDescending(x => x.DiemOnLuyen)
                .ThenByDescending(x => x.SoDeOnLuyenHoanThanh)
                .Select((x, i) => { x.Rank = i + 1; return x; })
                .ToList();
        }

        // ── Ôn luyện theo ngày ──────────────────────────────────────────────────

        private async Task<List<BxhRow>> BuildOnLuyenNgayRankingAsync(DateOnly ngay)
        {
            var olLhIds = await GetOnLuyenLhIdsAsync();
            if (olLhIds.Count == 0) return new();

            var ngayDt     = ngay.ToDateTime(TimeOnly.MinValue);
            var ngayDtNext = ngay.AddDays(1).ToDateTime(TimeOnly.MinValue);

            var data = (await _db.BaiThis.AsNoTracking()
                    .Where(b => b.Idnv != null && b.DiemSo != null && olLhIds.Contains(b.Idlh ?? 0)
                        && (b.NgayThi == ngay
                            || (b.NgayThi == null && b.GioKetThuc >= ngayDt && b.GioKetThuc < ngayDtNext)))
                    .Select(b => new { Idnv = b.Idnv!.Value, b.Idlh, b.DiemSo })
                    .ToListAsync())
                .GroupBy(b => b.Idnv)
                .ToDictionary(
                    g => g.Key,
                    g => (Diem: g.Sum(x => x.DiemSo ?? 0), SoDe: g.Select(x => x.Idlh).Distinct().Count()));

            if (data.Count == 0) return new();

            var nvMap = await GetNvInfoAsync(data.Keys.ToList());

            return data
                .Select(kvp =>
                {
                    nvMap.TryGetValue(kvp.Key, out var nv);
                    return new BxhRow
                    {
                        NVID = kvp.Key,
                        HoTen = nv.HoTen ?? "",
                        MaNV = nv.MaNV ?? "",
                        TenPB = nv.TenPB ?? "",
                        DiemOnLuyen = kvp.Value.Diem,
                        SoDeOnLuyenHoanThanh = kvp.Value.SoDe
                    };
                })
                .OrderByDescending(x => x.DiemOnLuyen)
                .ThenByDescending(x => x.SoDeOnLuyenHoanThanh)
                .Select((x, i) => { x.Rank = i + 1; return x; })
                .ToList();
        }

        // ── Ôn luyện theo tuần ──────────────────────────────────────────────────

        private async Task<List<BxhRow>> BuildOnLuyenTuanRankingAsync(DateOnly weekStart)
        {
            var olLhIds = await GetOnLuyenLhIdsAsync();
            if (olLhIds.Count == 0) return new();

            var weekEnd    = weekStart.AddDays(7);
            var weekStartDt = weekStart.ToDateTime(TimeOnly.MinValue);
            var weekEndDt   = weekEnd.ToDateTime(TimeOnly.MinValue);

            var data = (await _db.BaiThis.AsNoTracking()
                    .Where(b => b.Idnv != null && b.DiemSo != null && olLhIds.Contains(b.Idlh ?? 0)
                        && (b.NgayThi >= weekStart && b.NgayThi < weekEnd
                            || (b.NgayThi == null && b.GioKetThuc >= weekStartDt && b.GioKetThuc < weekEndDt)))
                    .Select(b => new { Idnv = b.Idnv!.Value, b.Idlh, b.DiemSo })
                    .ToListAsync())
                .GroupBy(b => b.Idnv)
                .ToDictionary(
                    g => g.Key,
                    g => (Diem: g.Sum(x => x.DiemSo ?? 0), SoDe: g.Select(x => x.Idlh).Distinct().Count()));

            if (data.Count == 0) return new();

            var nvMap = await GetNvInfoAsync(data.Keys.ToList());

            return data
                .Select(kvp =>
                {
                    nvMap.TryGetValue(kvp.Key, out var nv);
                    return new BxhRow
                    {
                        NVID = kvp.Key,
                        HoTen = nv.HoTen ?? "",
                        MaNV = nv.MaNV ?? "",
                        TenPB = nv.TenPB ?? "",
                        DiemOnLuyen = kvp.Value.Diem,
                        SoDeOnLuyenHoanThanh = kvp.Value.SoDe
                    };
                })
                .OrderByDescending(x => x.DiemOnLuyen)
                .ThenByDescending(x => x.SoDeOnLuyenHoanThanh)
                .Select((x, i) => { x.Rank = i + 1; return x; })
                .ToList();
        }

        // ── Week helpers ────────────────────────────────────────────────────────

        // Tính ngày thứ Hai đầu tuần từ chuỗi "yyyy-Www" (ISO 8601)
        private static DateOnly ParseWeekStart(string? tuan)
        {
            if (!string.IsNullOrEmpty(tuan) && System.Text.RegularExpressions.Regex.IsMatch(tuan, @"^\d{4}-W\d{1,2}$"))
            {
                int year = int.Parse(tuan[..4]);
                int week = int.Parse(tuan[6..]);
                var jan4 = new DateTime(year, 1, 4);
                int dow = (int)jan4.DayOfWeek; if (dow == 0) dow = 7;
                var monday = jan4.AddDays(1 - dow).AddDays((week - 1) * 7);
                return DateOnly.FromDateTime(monday);
            }
            return GetCurrentWeekStart();
        }

        private static DateOnly GetCurrentWeekStart()
        {
            var today = DateTime.Today;
            int dow = (int)today.DayOfWeek; if (dow == 0) dow = 7;
            return DateOnly.FromDateTime(today.AddDays(1 - dow));
        }

        private static string GetCurrentWeekStr()
        {
            var ws = GetCurrentWeekStart();
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            int weekNum = cal.GetWeekOfYear(ws.ToDateTime(TimeOnly.MinValue),
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
            return $"{ws.Year}-W{weekNum:D2}";
        }
    }
}
