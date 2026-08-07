using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class AdminDashboardController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IConnectionMultiplexer _mux;

        private const int OnlineWindowMs = 5 * 60 * 1000; // 5 phút

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
