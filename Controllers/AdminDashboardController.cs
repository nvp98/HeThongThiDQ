using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class AdminDashboardController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IConnectionMultiplexer _mux;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpFactory;

        private const int OnlineWindowMs = 5 * 60 * 1000;

        // CPU delta tracking (shared across requests)
        private static DateTime _lastCpuCheck = DateTime.MinValue;
        private static TimeSpan _lastProcCpu  = TimeSpan.Zero;
        private static double   _cpuPct       = 0;
        private static readonly object _cpuLock = new();

        public AdminDashboardController(ELEARNINGEntities db, MyAuthentication auth,
                                        IConnectionMultiplexer mux, IConfiguration config,
                                        IHttpClientFactory httpFactory)
        {
            _db          = db;
            _auth        = auth;
            _mux         = mux;
            _config      = config;
            _httpFactory = httpFactory;
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

        [HttpGet]
        public async Task<IActionResult> RabbitStats()
        {
            if (_auth.IDQuyen != 1) return Json(new { });

            long   published = 0, processed = 0, failed = 0, pending = 0;
            long   queueMessages = -1, queueReady = -1, queueUnacked = -1;
            double publishRate = -1, deliverRate = -1;
            bool   mgmtAvailable = false;
            string mgmtError = "";

            // --- Redis counters ---
            try
            {
                var rdb = _mux.GetDatabase();
                var t1  = rdb.StringGetAsync(RabbitMqPublisher.KeyPublished);
                var t2  = rdb.StringGetAsync(RabbitMqPublisher.KeyProcessed);
                var t3  = rdb.StringGetAsync(RabbitMqPublisher.KeyFailed);
                await Task.WhenAll(t1, t2, t3);
                published = (long)t1.Result;
                processed = (long)t2.Result;
                failed    = (long)t3.Result;
                pending   = Math.Max(0, published - processed - failed);
            }
            catch { }

            // --- RabbitMQ Management API ---
            try
            {
                var amqpUri = _config.GetConnectionString("RabbitMQ") ?? "";
                var amqpU   = new Uri(amqpUri);
                var parts   = amqpU.UserInfo.Split(':');
                var mgmtUrl = $"http://{amqpU.Host}:15672/api/queues/%2F/{RabbitMqPublisher.QueueName}";
                var authB64 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        $"{Uri.UnescapeDataString(parts[0])}:{(parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "")}"));

                using var http = _httpFactory.CreateClient();
                http.Timeout = TimeSpan.FromMilliseconds(800);
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authB64);

                var resp = await http.GetAsync(mgmtUrl);
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    queueMessages = root.TryGetProperty("messages",               out var m)  ? m.GetInt64()  : 0;
                    queueReady    = root.TryGetProperty("messages_ready",          out var mr) ? mr.GetInt64() : 0;
                    queueUnacked  = root.TryGetProperty("messages_unacknowledged", out var mu) ? mu.GetInt64() : 0;
                    if (root.TryGetProperty("message_stats", out var ms))
                    {
                        if (ms.TryGetProperty("publish_details",     out var pd) && pd.TryGetProperty("rate", out var pr)) publishRate = pr.GetDouble();
                        if (ms.TryGetProperty("deliver_get_details", out var dd) && dd.TryGetProperty("rate", out var dr)) deliverRate = dr.GetDouble();
                    }
                    mgmtAvailable = true;
                }
                else { mgmtError = $"HTTP {(int)resp.StatusCode}"; }
            }
            catch (Exception ex) { mgmtError = ex.Message.Length > 60 ? ex.Message[..60] : ex.Message; }

            // Trạng thái toàn vẹn
            bool integrityOk = !mgmtAvailable || Math.Abs(pending - queueMessages) <= 2;
            string status, statusMsg;
            if      (failed > 0)     { status = "error";   statusMsg = $"Có {failed:N0} message lỗi — kiểm tra dead-letter queue"; }
            else if (published == 0) { status = "idle";    statusMsg = "Chưa có dữ liệu — counter bắt đầu đếm khi có người nộp bài"; }
            else if (pending > 0)    { status = "warning"; statusMsg = $"Còn {pending:N0} message đang chờ xử lý"; }
            else                     { status = "ok";      statusMsg = "Toàn vẹn dữ liệu — tất cả bài thi đã lưu DB"; }

            return Json(new
            {
                Published     = published,
                Processed     = processed,
                Failed        = failed,
                Pending       = pending,
                QueueMessages = queueMessages,
                QueueReady    = queueReady,
                QueueUnacked  = queueUnacked,
                PublishRate   = publishRate >= 0 ? Math.Round(publishRate, 1) : (double?)null,
                DeliverRate   = deliverRate >= 0 ? Math.Round(deliverRate, 1) : (double?)null,
                MgmtAvailable = mgmtAvailable,
                MgmtError     = mgmtError,
                IntegrityOk   = integrityOk,
                Status        = status,
                StatusMsg     = statusMsg,
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetMqStats()
        {
            if (_auth.IDQuyen != 1) return Json(new { ok = false });
            var rdb = _mux.GetDatabase();
            await rdb.KeyDeleteAsync(new RedisKey[]
            {
                RabbitMqPublisher.KeyPublished,
                RabbitMqPublisher.KeyProcessed,
                RabbitMqPublisher.KeyFailed,
            });
            return Json(new { ok = true });
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
