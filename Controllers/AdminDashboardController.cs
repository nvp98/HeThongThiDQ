using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class AdminDashboardController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IConnectionMultiplexer _mux;

        private const int OnlineWindowMs = 5 * 60 * 1000;

        // CPU delta tracking (shared across requests)
        private static DateTime _lastCpuCheck = DateTime.MinValue;
        private static TimeSpan _lastProcCpu  = TimeSpan.Zero;
        private static double   _cpuPct       = 0;
        private static readonly object _cpuLock = new();

        public AdminDashboardController(ELEARNINGEntities db, MyAuthentication auth, IConnectionMultiplexer mux)
        {
            _db   = db;
            _auth = auth;
            _mux  = mux;
        }

        public async Task<IActionResult> Index()
        {
            if (_auth.IDQuyen != 1) return RedirectToAction("Index", "EClassroom");
            return View(await GetDashboardData());
        }

        [HttpGet]
        public async Task<IActionResult> Data()
        {
            if (_auth.IDQuyen != 1) return Json(new { });
            return Json(await GetDashboardData());
        }

        [HttpGet]
        public IActionResult SysInfo()
        {
            if (_auth.IDQuyen != 1) return Json(new { });

            var proc = Process.GetCurrentProcess();

            lock (_cpuLock)
            {
                var now    = DateTime.UtcNow;
                var wallMs = (_lastCpuCheck == DateTime.MinValue)
                    ? 0
                    : (now - _lastCpuCheck).TotalMilliseconds;

                if (wallMs >= 500)
                {
                    var cpuMs = (proc.TotalProcessorTime - _lastProcCpu).TotalMilliseconds;
                    _cpuPct = Math.Round(cpuMs / wallMs / Environment.ProcessorCount * 100, 1);
                }
                _lastCpuCheck = now;
                _lastProcCpu  = proc.TotalProcessorTime;
            }

            var ramMb  = Math.Round(proc.WorkingSet64 / 1024.0 / 1024.0, 1);
            var gcMb   = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 1);
            var gcInfo = GC.GetGCMemoryInfo();
            var totalRamMb = Math.Round(gcInfo.TotalAvailableMemoryBytes / 1024.0 / 1024.0, 0);

            string uptimeStr;
            try
            {
                var uptime = DateTime.Now - proc.StartTime;
                uptimeStr = uptime.TotalHours >= 1
                    ? $"{(int)uptime.TotalHours}h {uptime.Minutes:D2}m"
                    : $"{uptime.Minutes}m {uptime.Seconds}s";
            }
            catch { uptimeStr = "--"; }

            return Json(new
            {
                CpuPct         = _cpuPct,
                RamMb          = ramMb,
                TotalRamMb     = totalRamMb,
                GcMb           = gcMb,
                Threads        = proc.Threads.Count,
                Uptime         = uptimeStr,
                ProcessorCount = Environment.ProcessorCount,
            });
        }

        private async Task<AdminDashboardData> GetDashboardData()
        {
            var data = new AdminDashboardData();
            try
            {
                var rdb      = _mux.GetDatabase();
                var nowMs    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var staleMs  = nowMs - OnlineWindowMs;
                var today    = DateTime.Now.ToString("yyyyMMdd");

                // Dọn stale entries trước khi đọc
                await rdb.SortedSetRemoveRangeByScoreAsync("HPDQ:online:users", 0, staleMs);
                await rdb.SortedSetRemoveRangeByScoreAsync("HPDQ:online:exams", 0, staleMs);

                // Tổng quan
                data.OnlineCount  = (long)await rdb.SortedSetLengthAsync("HPDQ:online:users");
                data.ExamCount    = (long)await rdb.SortedSetLengthAsync("HPDQ:online:exams");
                data.TodayLogins  = (long)await rdb.StringGetAsync($"HPDQ:stats:logins:daily:{today}");
                data.TotalLogins  = (long)await rdb.StringGetAsync("HPDQ:stats:logins:total");

                // Dữ liệu biểu đồ theo giờ hôm nay
                var hourTasks = Enumerable.Range(0, 24)
                    .Select(h => rdb.StringGetAsync($"HPDQ:stats:logins:hourly:{today}:{h:D2}"))
                    .ToArray();
                var hourValues = await Task.WhenAll(hourTasks);
                data.HourlyLogins = hourValues.Select(v => (int)(long)v).ToArray();

                // Trend — push snapshot, giữ 120 điểm (~60 phút với 30s/lần)
                const string trendKey = "HPDQ:trend:snapshots";
                var snap = System.Text.Json.JsonSerializer.Serialize(
                    new TrendPoint { T = nowMs, O = data.OnlineCount, E = data.ExamCount });
                await rdb.ListRightPushAsync(trendKey, snap);
                await rdb.ListTrimAsync(trendKey, -120, -1);
                var trendRaw = await rdb.ListRangeAsync(trendKey, -60, -1);
                data.TrendPoints = trendRaw
                    .Select(e => { try { return System.Text.Json.JsonSerializer.Deserialize<TrendPoint>(e.ToString()); } catch { return null; } })
                    .Where(p => p != null).Select(p => p!).ToList();

                // Danh sách người đang online kèm thông tin
                var onlineEntries = await rdb.SortedSetRangeByScoreWithScoresAsync(
                    "HPDQ:online:users", staleMs, double.PositiveInfinity);

                var idList = onlineEntries
                    .Select(e => { int.TryParse(e.Element.ToString(), out int id); return id; })
                    .Where(id => id > 0).ToList();

                if (idList.Any())
                {
                    var users = await (
                        from nv in _db.NhanViens.Where(x => idList.Contains(x.Id))
                        join pb in _db.PhongBans on nv.IdphongBan equals pb.IdphongBan into pbj
                        from pb in pbj.DefaultIfEmpty()
                        select new { nv.Id, nv.HoTen, nv.MaNv, TenPB = pb != null ? pb.TenPhongBan : "" }
                    ).ToListAsync();

                    var scoreMap = onlineEntries.ToDictionary(
                        e => e.Element.ToString(), e => e.Score);

                    data.OnlineUsers = idList
                        .Select(id =>
                        {
                            var u = users.FirstOrDefault(x => x.Id == id);
                            scoreMap.TryGetValue(id.ToString(), out double score);
                            var lastMs  = (long)score;
                            var elapsed = (nowMs - lastMs) / 1000;
                            var lastStr = elapsed < 60
                                ? "Vừa xong"
                                : $"{elapsed / 60} phút trước";
                            return new OnlineUserInfo
                            {
                                IDNV      = id,
                                HoTen     = u?.HoTen ?? "",
                                MaNV      = u?.MaNv  ?? "",
                                PhongBan  = u?.TenPB ?? "",
                                LastActive = lastStr,
                            };
                        })
                        .OrderBy(x => x.PhongBan).ThenBy(x => x.HoTen)
                        .ToList();
                }
            }
            catch { }

            return data;
        }

        public class AdminDashboardData
        {
            public long OnlineCount  { get; set; }
            public long ExamCount    { get; set; }
            public long TodayLogins  { get; set; }
            public long TotalLogins  { get; set; }
            public int[] HourlyLogins { get; set; } = new int[24];
            public List<OnlineUserInfo> OnlineUsers { get; set; } = new();
            public List<TrendPoint> TrendPoints { get; set; } = new();
        }

        public class TrendPoint
        {
            public long T { get; set; }  // unix ms
            public long O { get; set; }  // online users
            public long E { get; set; }  // đang thi
        }

        public class OnlineUserInfo
        {
            public int    IDNV       { get; set; }
            public string HoTen      { get; set; } = "";
            public string MaNV       { get; set; } = "";
            public string PhongBan   { get; set; } = "";
            public string LastActive { get; set; } = "";
        }
    }
}
