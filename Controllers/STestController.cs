using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class STestController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IDistributedCache _cache;

        private static readonly DistributedCacheEntryOptions _sessionOpts =
            new() { SlidingExpiration = TimeSpan.FromMinutes(30) };

        public STestController(ELEARNINGEntities db, MyAuthentication auth, IDistributedCache cache)
        {
            _db   = db;
            _auth = auth;
            _cache = cache;
        }

        private string SessionKey(int idlh) => $"exam:session:{_auth.ID}:{idlh}";

        public async Task<IActionResult> Index(int LHID)
        {
            var random = new Random();

            var lh = await _db.LopHocs.AsNoTracking().FirstOrDefaultAsync(x => x.Idlh == LHID);
            if (lh == null || lh.IddeThi == null) return RedirectToAction("Index", "EClassroom");

            var dethi = await _db.DeThis.AsNoTracking().FirstOrDefaultAsync(x => x.IddeThi == lh.IddeThi);
            int totalTimeSec = (int)((dethi?.ThoiGianLamBai ?? 0) * 60);

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

            var allQuestions = await (
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

            ViewBag.ThoiGianLamBai = DateTime.Now.AddMinutes(dethi?.ThoiGianLamBai ?? 0);
            ViewBag.ThoiGianThi    = dethi?.ThoiGianLamBai ?? 0;
            ViewBag.IDNV           = _auth.ID;
            ViewBag.IDLH           = LHID;
            ViewBag.ExamSession    = JsonSerializer.Serialize(session);

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

        public JsonResult AddEvent()
        {
            return Json(true);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitExamAjax([FromBody] ExamSubmitDto dto)
        {
            if (dto.Answers == null || dto.Answers.Count == 0)
                return Json(new { success = false, message = "Không có câu trả lời" });

            var lanthi = await _db.BaiThis
                .Where(x => x.Idnv == _auth.ID && x.IddeThi == dto.IDDeThi && x.Idlh == dto.IDLH)
                .ToListAsync();
            var lophoc = await _db.LopHocs.FirstOrDefaultAsync(x => x.Idlh == dto.IDLH);

            if (lanthi.Count >= 1 && lophoc?.IsCoCtdt == 0)
                return Json(new { success = false, message = "Bạn đã hoàn thành bài thi", redirectUrl = Url.Action("Index", "EClassroom") });

            var baiThi = new BaiThi
            {
                Idlh       = dto.IDLH,
                IddeThi    = dto.IDDeThi,
                Idnd       = dto.IDND,
                Idnv       = _auth.ID,
                IdphongBan = _auth.IDPhongban,
                IdviTri    = _auth.IDViTri,
                DiemSo     = 0,
                NgayThi    = DateOnly.FromDateTime(DateTime.Now),
                TinhTrang  = false,
                LanThi     = lanthi.Count + 1,
                GioBatDau  = lophoc?.Tgbdlh,
                GioKetThuc = lophoc?.Tgktlh
            };

            if (dto.ThoiGianSec > 0) baiThi.ThoiGianThi = dto.ThoiGianSec;
            if (dto.TGBDLamBaiThi > 0)
            {
                baiThi.GioBatDau  = DateTimeOffset.FromUnixTimeMilliseconds(dto.TGBDLamBaiThi).LocalDateTime;
                baiThi.GioKetThuc = DateTime.Now;
            }

            _db.BaiThis.Add(baiThi);
            await _db.SaveChangesAsync();

            // Load correct answers from DB
            var idCHList = dto.Answers.Select(a => a.IDCH).ToList();
            var cauHoiMap = await (
                from cd in _db.CauHoiDeThis.Where(x => x.IddeThi == dto.IDDeThi && x.IdcauHoi.HasValue && idCHList.Contains(x.IdcauHoi!.Value))
                join ch in _db.CauHois on cd.IdcauHoi equals ch.Idch
                select new { IdcauHoi = cd.IdcauHoi!.Value, ch.Iddađung, cd.Diem }
            ).ToDictionaryAsync(x => x.IdcauHoi);

            // Read per-answer timestamps from Redis
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

            foreach (var ans in dto.Answers)
            {
                if (!cauHoiMap.TryGetValue(ans.IDCH, out var info)) continue;

                DateTime? chosenAt = null;
                if (chosenAtMap.TryGetValue(ans.IDCH, out var ts) && ts.HasValue)
                    chosenAt = DateTimeOffset.FromUnixTimeMilliseconds(ts.Value).LocalDateTime;

                _db.CtbaiThis.Add(new CtbaiThi
                {
                    IdbaiThi     = baiThi.IdbaiThi,
                    IdcauHoi     = ans.IDCH,
                    IddapAnDung  = info.Iddađung,
                    IddapAnNv    = ans.Answer,
                    Diem         = info.Iddađung == ans.Answer ? (info.Diem ?? 0) : 0,
                    ThoiGianChon = chosenAt,
                });
            }
            await _db.SaveChangesAsync();

            double diemSo = await _db.CtbaiThis
                .Where(x => x.IdbaiThi == baiThi.IdbaiThi)
                .SumAsync(x => x.Diem) ?? 0;

            baiThi.DiemSo    = diemSo;
            baiThi.TinhTrang = true;
            await _db.SaveChangesAsync();

            // Clean up Redis session
            try { await _cache.RemoveAsync(SessionKey(dto.IDLH)); } catch { }

            TempData["Message"] = $"Với số điểm là: {diemSo}/100";

            return Json(new
            {
                success     = true,
                redirectUrl = Url.Action("ViewResult", "EClassroom", new { IDLH = dto.IDLH, IDBaiThi = baiThi.IdbaiThi })
            });
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
