using HeThongThiDQ.Services;
using HeThongThiDQ.Common;
using Microsoft.Data.SqlClient;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class STestController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _mux;

        private static readonly DistributedCacheEntryOptions _sessionOpts =
            new() { SlidingExpiration = TimeSpan.FromMinutes(30) };

        private readonly IExamQueuePublisher _queue;

        public STestController(ELEARNINGEntities db, MyAuthentication auth,
                               IDistributedCache cache, IConnectionMultiplexer mux,
                               IExamQueuePublisher queue)
        {
            _db    = db;
            _auth  = auth;
            _cache = cache;
            _mux   = mux;
            _queue = queue;
        }

        private string SessionKey(int idlh) => $"exam:session:{_auth.ID}:{idlh}";

        private static readonly DistributedCacheEntryOptions _qCacheOpts =
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) };

        private static readonly DistributedCacheEntryOptions _lhOpts =
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };

        private record LopHocDto(
            int Idlh, int? IddeThi, int? Ndid, int? IsCoCtdt,
            DateTime? Tgbdlh, DateTime? Tgktlh, double? ThoiGianLamBai);

        private async Task<LopHocDto?> GetLopHocCachedAsync(int lhid)
        {
            var key = $"lophoc:{lhid}";
            try
            {
                var cached = await _cache.GetStringAsync(key);
                if (cached != null) return JsonSerializer.Deserialize<LopHocDto>(cached);
            }
            catch { }

            var lh = await _db.LopHocs.AsNoTracking().FirstOrDefaultAsync(x => x.Idlh == lhid);
            if (lh == null) return null;

            var dethi = lh.IddeThi.HasValue
                ? await _db.DeThis.AsNoTracking()
                    .Where(x => x.IddeThi == lh.IddeThi)
                    .Select(x => new { x.ThoiGianLamBai })
                    .FirstOrDefaultAsync()
                : null;

            var dto = new LopHocDto(
                lh.Idlh, lh.IddeThi, lh.Ndid, lh.IsCoCtdt,
                lh.Tgbdlh, lh.Tgktlh, dethi?.ThoiGianLamBai);

            try { await _cache.SetStringAsync(key, JsonSerializer.Serialize(dto), _lhOpts); }
            catch { }

            return dto;
        }

        public async Task<IActionResult> Index(int LHID)
        {
            var random = new Random();

            var lh = await GetLopHocCachedAsync(LHID);
            if (lh == null || lh.IddeThi == null) return RedirectToAction("Index", "EClassroom");
            int totalTimeSec = (int)((lh.ThoiGianLamBai ?? 0) * 60);

            // Try restore existing session from Redis
            ExamSession? session = null;
            try
            {
                var cached = await _cache.GetStringAsync(SessionKey(LHID));
                if (cached != null)
                    session = JsonSerializer.Deserialize<ExamSession>(cached);
            }
            catch { }

            // Invalidate if belongs to different exam or time ran out
            if (session != null &&
                (session.IDDeThi != (lh.IddeThi ?? 0) ||
                 DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= session.EndTimestamp))
            {
                try { await _cache.RemoveAsync(SessionKey(LHID)); } catch { }
                session = null;
            }

            // Cache câu hỏi theo LHID — tránh query DB mỗi lần load đề
            var qKey = $"exam:q:{LHID}";
            List<TestValidation>? allQuestions = null;
            try
            {
                var qCached = await _cache.GetStringAsync(qKey);
                if (qCached != null)
                    allQuestions = JsonSerializer.Deserialize<List<TestValidation>>(qCached);
            }
            catch { }

            if (allQuestions == null)
            {
                allQuestions = await (
                    from cd in _db.CauHoiDeThis.Where(x => x.IddeThi == lh.IddeThi)
                    join ch in _db.CauHois on cd.IdcauHoi equals ch.Idch
                    join da in _db.DanhSachDa on ch.Iddađung equals da.Iddsđa
                    select new TestValidation
                    {
                        IDCH      = ch.Idch,
                        NoiDungCH = ch.NoiDungCh,
                        DapAnA    = ch.DapAnA,
                        DapAnB    = ch.DapAnB,
                        DapAnC    = ch.DapAnC,
                        DapAnD    = ch.DapAnD,
                        IDDADung  = ch.Iddađung ?? 0,
                        DapAnDung = da.TenĐa,
                        Diem      = cd.Diem ?? 0,
                        IDLH      = LHID,
                        IDDeThi   = lh.IddeThi ?? 0,
                        IDND      = lh.Ndid ?? 0,
                        IsDao     = ch.IsDao ?? false,
                        GioBatDau = DateTime.Now,
                    }).ToListAsync();

                try
                {
                    await _cache.SetStringAsync(qKey, JsonSerializer.Serialize(allQuestions), _qCacheOpts);
                }
                catch { }
            }

            List<TestValidation> res;

            if (session != null)
            {
                // Restore saved question order
                var map = allQuestions.ToDictionary(q => q.IDCH);
                res = session.QuestionOrder
                    .Where(id => map.ContainsKey(id))
                    .Select(id => map[id])
                    .ToList();

                // Restore saved answers so view can pre-select them
                foreach (var q in res)
                    if (session.Answers.TryGetValue(q.IDCH, out var sa) && sa.AnswerId.HasValue)
                        q.Answer = sa.AnswerId.Value;

                // Refresh sliding TTL
                try
                {
                    await _cache.SetStringAsync(SessionKey(LHID),
                        JsonSerializer.Serialize(session), _sessionOpts);
                }
                catch { }
            }
            else
            {
                // New session — shuffle and save to Redis
                res = allQuestions.OrderBy(_ => random.Next()).ToList();

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                session = new ExamSession
                {
                    IDDeThi        = lh.IddeThi ?? 0,
                    IDND           = lh.Ndid ?? 0,
                    IDLH           = LHID,
                    IDNV           = _auth.ID,
                    TotalTimeSec   = totalTimeSec,
                    StartTimestamp = now,
                    EndTimestamp   = now + (long)totalTimeSec * 1000,
                    SavedAt        = now,
                    QuestionOrder  = res.Select(q => q.IDCH).ToList(),
                    Answers        = res.ToDictionary(q => q.IDCH, _ => new SessionAnswer()),
                };

                try
                {
                    await _cache.SetStringAsync(SessionKey(LHID),
                        JsonSerializer.Serialize(session), _sessionOpts);
                }
                catch { }
            }

            ViewBag.ThoiGianLamBai = DateTime.Now.AddMinutes(lh.ThoiGianLamBai ?? 0);
            ViewBag.ThoiGianThi    = lh.ThoiGianLamBai ?? 0;
            ViewBag.IDNV           = _auth.ID;
            ViewBag.IDLH           = LHID;
            ViewBag.ExamSession    = JsonSerializer.Serialize(session);

            // Ghi nhận thí sinh đang thi vào Redis
            try
            {
                await _mux.GetDatabase().SortedSetAddAsync(
                    "HPDQ:online:exams",
                    $"{_auth.ID}:{LHID}",
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            catch { }

            return View(res);
        }

        public async Task<IActionResult> Confirm(List<TestValidation> ListQ, string? yourParamName, string? TGBDLamBaiThi)
        {
            if (ListQ == null || ListQ.Count == 0)
                return RedirectToAction("Index", "EClassroom");

            int IDBaiThi = 0;
            int IDDeThi  = 0;
            int i        = 0;

            foreach (var Q in ListQ)
            {
                if (i == 0)
                {
                    var lanthi = await _db.BaiThis
                        .Where(x => x.Idnv == _auth.ID && x.IddeThi == Q.IDDeThi && x.Idlh == Q.IDLH)
                        .ToListAsync();
                    var lophoc = await _db.LopHocs.FirstOrDefaultAsync(x => x.Idlh == Q.IDLH);

                    if (lanthi.Count >= 1 && lophoc?.IsCoCtdt == 0)
                    {
                        TempData["msgSuccess"] = "<script>alert('Bạn đã hoàn thành bài thi');</script>";
                        return RedirectToAction("Index", "EClassroom");
                    }

                    var baiThi = new BaiThi
                    {
                        Idlh        = Q.IDLH,
                        IddeThi     = Q.IDDeThi,
                        Idnd        = Q.IDND,
                        Idnv        = _auth.ID,
                        IdphongBan  = _auth.IDPhongban,
                        IdviTri     = _auth.IDViTri,
                        DiemSo      = 0,
                        NgayThi     = DateOnly.FromDateTime(DateTime.Now),
                        TinhTrang   = false,
                        LanThi      = lanthi.Count + 1,
                        GioBatDau   = lophoc?.Tgbdlh,
                        GioKetThuc  = lophoc?.Tgktlh
                    };

                    int.TryParse(yourParamName, out int thoiGianSec);
                    if (thoiGianSec > 0) baiThi.ThoiGianThi = thoiGianSec;

                    if (long.TryParse(TGBDLamBaiThi, out long milliseconds))
                    {
                        baiThi.GioBatDau  = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime;
                        baiThi.GioKetThuc = DateTime.Now;
                    }

                    _db.BaiThis.Add(baiThi);
                    await _db.SaveChangesAsync();
                    IDBaiThi = baiThi.IdbaiThi;
                    IDDeThi  = Q.IDDeThi;

                    _db.CtbaiThis.Add(new CtbaiThi
                    {
                        IdbaiThi    = IDBaiThi,
                        IdcauHoi    = Q.IDCH,
                        IddapAnDung = Q.IDDADung,
                        IddapAnNv   = Q.Answer,
                        Diem        = Q.IDDADung == Q.Answer ? Q.Diem : 0
                    });
                    i++;
                }
                else
                {
                    _db.CtbaiThis.Add(new CtbaiThi
                    {
                        IdbaiThi    = IDBaiThi,
                        IdcauHoi    = Q.IDCH,
                        IddapAnDung = Q.IDDADung,
                        IddapAnNv   = Q.Answer,
                        Diem        = Q.IDDADung == Q.Answer ? Q.Diem : 0
                    });
                }
            }
            await _db.SaveChangesAsync();

            double diemSo = await _db.CtbaiThis.Where(x => x.IdbaiThi == IDBaiThi).SumAsync(x => x.Diem) ?? 0;

            var bt = await _db.BaiThis.Where(x => x.IdbaiThi == IDBaiThi).FirstOrDefaultAsync();
            if (bt != null)
            {
                bt.DiemSo    = diemSo;
                bt.TinhTrang = true;
                await _db.SaveChangesAsync();
            }

            TempData["Message"] = $"Với số điểm là: {diemSo}/100";

            return RedirectToAction("ViewResult", "EClassroom",
                new { IDLH = ListQ[0].IDLH, IDBaiThi });
        }

        [HttpPost]
        public async Task<IActionResult> AutoSave([FromBody] AutoSaveDto dto)
        {
            if (dto == null) return Json(new { success = false });
            try
            {
                var key  = SessionKey(dto.IDLH);
                var json = await _cache.GetStringAsync(key);
                if (json == null) return Json(new { success = false, expired = true });

                var session = JsonSerializer.Deserialize<ExamSession>(json);
                if (session == null) return Json(new { success = false });

                session.Answers[dto.IDCH] = new SessionAnswer
                {
                    AnswerId = dto.AnswerId,
                    ChosenAt = dto.ChosenAt,
                };
                session.SavedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                await _cache.SetStringAsync(key, JsonSerializer.Serialize(session), _sessionOpts);

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = true }); // Redis down — don't fail the exam
            }
        }

        [HttpGet]
        public async Task<IActionResult> PingSession(int idlh = 0)
        {
            if (idlh > 0)
            {
                try
                {
                    await _mux.GetDatabase().SortedSetAddAsync(
                        "HPDQ:online:exams",
                        $"{_auth.ID}:{idlh}",
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }
                catch { }
            }
            return Json(new { valid = true });
        }

        public JsonResult AddEvent()
        {
            return Json(true);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitExamAjax([FromBody] ExamSubmitDto dto)
        {
            if (dto.Answers == null || dto.Answers.Count == 0)
                return Json(new { success = false, message = "Không có câu trả lời" });

            var lophoc = await GetLopHocCachedAsync(dto.IDLH);

            // Dùng Redis SET NX để chặn duplicate submit atomic (nhanh hơn DB query)
            var rdbCheck = _mux.GetDatabase();
            var submitKey = $"exam:submitted:{_auth.ID}:{dto.IDLH}";
            if (lophoc?.IsCoCtdt == 0)
            {
                var isNew = await rdbCheck.StringSetAsync(submitKey, "1", TimeSpan.FromDays(1), When.NotExists);
                if (!isNew)
                    return Json(new { success = false, message = "Bạn đã hoàn thành bài thi", redirectUrl = Url.Action("Index", "EClassroom") });
            }

            var rdbLT     = _mux.GetDatabase();
            var lanThiKey = $"exam:lanthi:{_auth.ID}:{dto.IDLH}";
            if (!await rdbLT.KeyExistsAsync(lanThiKey))
            {
                var dbCount = await _db.BaiThis.CountAsync(x => x.Idnv == _auth.ID && x.Idlh == dto.IDLH);
                await rdbLT.StringSetAsync(lanThiKey, dbCount, TimeSpan.FromDays(30), When.NotExists);
            }
            var lanthi = (int)await rdbLT.StringIncrementAsync(lanThiKey);
            await rdbLT.KeyExpireAsync(lanThiKey, TimeSpan.FromDays(30));

            // Load correct answers — ưu tiên cache, fallback DB
            Dictionary<int, (int? Iddađung, double? Diem)> cauHoiMap;
            try
            {
                var qCached = await _cache.GetStringAsync($"exam:q:{dto.IDLH}");
                if (qCached != null)
                {
                    var qs = JsonSerializer.Deserialize<List<TestValidation>>(qCached);
                    if (qs != null && qs.Count > 0)
                    {
                        cauHoiMap = qs.ToDictionary(q => q.IDCH, q => ((int?)q.IDDADung, (double?)q.Diem));
                        goto answersReady;
                    }
                }
            }
            catch { }

            {
                var idCHList = dto.Answers.Select(a => a.IDCH).ToList();
                cauHoiMap = await (
                    from cd in _db.CauHoiDeThis.Where(x => x.IddeThi == dto.IDDeThi && x.IdcauHoi.HasValue && idCHList.Contains(x.IdcauHoi!.Value))
                    join ch in _db.CauHois on cd.IdcauHoi equals ch.Idch
                    select new { IdcauHoi = cd.IdcauHoi!.Value, ch.Iddađung, cd.Diem }
                ).ToDictionaryAsync(x => x.IdcauHoi, x => (x.Iddađung, x.Diem));
            }

            answersReady:

            // Lấy timestamp từng câu từ Redis session
            Dictionary<int, long?> chosenAtMap = new();
            try
            {
                var json = await _cache.GetStringAsync(SessionKey(dto.IDLH));
                if (json != null)
                {
                    var session = JsonSerializer.Deserialize<ExamSession>(json);
                    if (session?.Answers != null)
                        foreach (var kv in session.Answers)
                            chosenAtMap[kv.Key] = kv.Value.ChosenAt;
                }
            }
            catch { }

            // Tính điểm + build danh sách câu trả lời — hoàn toàn in-memory, không cần DB
            double diemSo = 0;
            var answerRecords = new List<AnswerRecord>();
            foreach (var ans in dto.Answers)
            {
                if (!cauHoiMap.TryGetValue(ans.IDCH, out var info)) continue;

                DateTime? chosenAt = null;
                if (chosenAtMap.TryGetValue(ans.IDCH, out var ts) && ts.HasValue)
                    chosenAt = DateTimeOffset.FromUnixTimeMilliseconds(ts.Value).LocalDateTime;

                var diem = info.Iddađung == ans.Answer ? (info.Diem ?? 0) : 0;
                diemSo  += diem;
                answerRecords.Add(new AnswerRecord
                {
                    IDCH         = ans.IDCH,
                    DapAnDung    = info.Iddađung,
                    DapAnNv      = ans.Answer,
                    Diem         = diem,
                    ThoiGianChon = chosenAt,
                });
            }

            // Publish vào queue — HTTP request trả về ngay, không đợi DB
            await _queue.PublishAsync(new ExamSubmitMessage
            {
                IDLH          = dto.IDLH,
                IDDeThi       = dto.IDDeThi,
                IDND          = dto.IDND,
                IDNV          = _auth.ID,
                IDPhongBan    = _auth.IDPhongban,
                IDViTri       = _auth.IDViTri,
                LanThi        = lanthi + 1,
                ThoiGianSec   = dto.ThoiGianSec,
                TGBDLamBaiThi = dto.TGBDLamBaiThi,
                DiemSo        = diemSo,
                Answers       = answerRecords,
            });

            // Dọn Redis session
            try { await _cache.RemoveAsync(SessionKey(dto.IDLH)); } catch { }
            try { await _mux.GetDatabase().SortedSetRemoveAsync("HPDQ:online:exams", $"{_auth.ID}:{dto.IDLH}"); } catch { }

            // Trả về điểm ngay, client chuyển sang trang chờ DB ghi xong
            return Json(new
            {
                success     = true,
                score       = diemSo,
                redirectUrl = Url.Action("WaitResult", "STest",
                    new { IDLH = dto.IDLH, score = diemSo }),
            });
        }

        [HttpGet]
        public IActionResult WaitResult(int IDLH, double score)
        {
            ViewBag.IDLH  = IDLH;
            ViewBag.Score = score;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResultReady(int IDLH)
        {
            var rdb = _mux.GetDatabase();
            var val = await rdb.StringGetAsync($"exam:result:{_auth.ID}:{IDLH}");
            if (val.HasValue && int.TryParse(val.ToString(), out int idbaithi))
                return Json(new { ready = true, idbaithi, redirectUrl = Url.Action("ViewResult", "EClassroom", new { IDLH, IDBaiThi = idbaithi }) });

            return Json(new { ready = false });
        }

        public class AutoSaveDto
        {
            public int  IDLH     { get; set; }
            public int  IDCH     { get; set; }
            public int? AnswerId { get; set; }
            public long ChosenAt { get; set; }
        }

        public class ExamAnswerDto
        {
            public int  IDCH   { get; set; }
            public int? Answer { get; set; }
        }

        public class ExamSubmitDto
        {
            public int  IDLH          { get; set; }
            public int  IDDeThi       { get; set; }
            public int  IDND          { get; set; }
            public int  ThoiGianSec   { get; set; }
            public long TGBDLamBaiThi { get; set; }
            public List<ExamAnswerDto> Answers { get; set; } = new();
        }
    }
}
