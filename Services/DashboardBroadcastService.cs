using HeThongThiDQ.Data;
using HeThongThiDQ.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace HeThongThiDQ.Services
{
    public class DashboardBroadcastService : BackgroundService
    {
        private readonly IHubContext<DashboardHub> _hub;
        private readonly IConnectionMultiplexer _mux;
        private readonly IServiceScopeFactory _scopeFactory;
        private const int OnlineWindowMs = 5 * 60 * 1000;

        public DashboardBroadcastService(
            IHubContext<DashboardHub> hub,
            IConnectionMultiplexer mux,
            IServiceScopeFactory scopeFactory)
        {
            _hub = hub;
            _mux = mux;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var data = await BuildSnapshot();
                    await _hub.Clients.Group("admins").SendAsync("DashboardUpdate", data, ct);
                }
                catch { }

                await Task.Delay(TimeSpan.FromSeconds(15), ct);
            }
        }

        private async Task<object> BuildSnapshot()
        {
            var rdb     = _mux.GetDatabase();
            var nowMs   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var staleMs = nowMs - OnlineWindowMs;
            var today   = DateTime.Now.ToString("yyyyMMdd");

            await rdb.SortedSetRemoveRangeByScoreAsync("HPDQ:online:users", 0, staleMs);
            await rdb.SortedSetRemoveRangeByScoreAsync("HPDQ:online:exams", 0, staleMs);

            var onlineCount = (long)await rdb.SortedSetLengthAsync("HPDQ:online:users");
            var examCount   = (long)await rdb.SortedSetLengthAsync("HPDQ:online:exams");
            var todayLogins = (long)await rdb.StringGetAsync($"HPDQ:stats:logins:daily:{today}");
            var totalLogins = (long)await rdb.StringGetAsync("HPDQ:stats:logins:total");

            var hourTasks    = Enumerable.Range(0, 24)
                .Select(h => rdb.StringGetAsync($"HPDQ:stats:logins:hourly:{today}:{h:D2}"))
                .ToArray();
            var hourValues   = await Task.WhenAll(hourTasks);
            var hourlyLogins = hourValues.Select(v => (int)(long)v).ToArray();

            // Trend snapshot
            const string trendKey = "HPDQ:trend:snapshots";
            var snap = JsonSerializer.Serialize(new { T = nowMs, O = onlineCount, E = examCount });
            await rdb.ListRightPushAsync(trendKey, snap);
            await rdb.ListTrimAsync(trendKey, -120, -1);
            var trendRaw = await rdb.ListRangeAsync(trendKey, -60, -1);
            var trendPoints = trendRaw
                .Select(e =>
                {
                    try { return JsonSerializer.Deserialize<TrendPoint>(e.ToString()); }
                    catch { return null; }
                })
                .Where(p => p != null).Select(p => p!).ToList();

            // Online users
            var onlineEntries = await rdb.SortedSetRangeByScoreWithScoresAsync(
                "HPDQ:online:users", staleMs, double.PositiveInfinity);

            var idList = onlineEntries
                .Select(e => { int.TryParse(e.Element.ToString(), out int id); return id; })
                .Where(id => id > 0).ToList();

            var onlineUsers = new List<OnlineUser>();
            if (idList.Count > 0)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ELEARNINGEntities>();

                var users = await (
                    from nv in db.NhanViens.Where(x => idList.Contains(x.Id))
                    join pb in db.PhongBans on nv.IdphongBan equals pb.IdphongBan into pbj
                    from pb in pbj.DefaultIfEmpty()
                    select new { nv.Id, nv.HoTen, nv.MaNv, TenPB = pb != null ? pb.TenPhongBan : "" }
                ).ToListAsync();

                var scoreMap = onlineEntries.ToDictionary(e => e.Element.ToString(), e => e.Score);
                onlineUsers = idList
                    .Select(id =>
                    {
                        var u = users.FirstOrDefault(x => x.Id == id);
                        scoreMap.TryGetValue(id.ToString(), out double score);
                        var elapsed = (nowMs - (long)score) / 1000;
                        return new OnlineUser
                        {
                            IDNV       = id,
                            HoTen      = u?.HoTen ?? "",
                            MaNV       = u?.MaNv  ?? "",
                            PhongBan   = u?.TenPB ?? "",
                            LastActive = elapsed < 60 ? "Vừa xong" : $"{elapsed / 60} phút trước",
                        };
                    })
                    .OrderBy(x => x.PhongBan).ThenBy(x => x.HoTen)
                    .ToList();
            }

            return new
            {
                OnlineCount  = onlineCount,
                ExamCount    = examCount,
                TodayLogins  = todayLogins,
                TotalLogins  = totalLogins,
                HourlyLogins = hourlyLogins,
                TrendPoints  = trendPoints,
                OnlineUsers  = onlineUsers,
            };
        }

        private record TrendPoint
        {
            public long T { get; init; }
            public long O { get; init; }
            public long E { get; init; }
        }

        private class OnlineUser
        {
            public int    IDNV       { get; set; }
            public string HoTen      { get; set; } = "";
            public string MaNV       { get; set; } = "";
            public string PhongBan   { get; set; } = "";
            public string LastActive { get; set; } = "";
        }
    }
}
