using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class EClassroomController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _mux;

        public EClassroomController(ELEARNINGEntities db, MyAuthentication auth,
                                    IDistributedCache cache, IConnectionMultiplexer mux)
        {
            _db    = db;
            _auth  = auth;
            _cache = cache;
            _mux   = mux;
        }

        private static readonly DistributedCacheEntryOptions _classListOpts =
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };

        // CauHoiDeThis: dữ liệu tĩnh (trọng số câu hỏi), dùng chung cho mọi user cùng đề thi
        private static readonly DistributedCacheEntryOptions _cauhoidethiOpts =
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };

        // BaiThis: cache ngắn để giảm tải lúc đỉnh, đủ mới để hiện điểm gần nhất
        private static readonly DistributedCacheEntryOptions _baithiOpts =
            new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) };

        public async Task<IActionResult> Index(int? page)
        {
            int id = _auth.ID;

            // Tầng 1: 7-bảng JOIN — static trong kỳ thi, cache 10 phút per-user
            List<EClassroomValidation> model;
            var classKey   = $"eclassroom:classes:{id}";
            string? cached = null;
            try { cached = await _cache.GetStringAsync(classKey); } catch { }

            if (cached != null)
            {
                model = JsonSerializer.Deserialize<List<EClassroomValidation>>(cached) ?? new();
            }
            else
            {
                var res = await (from h in _db.XnhocTaps
                                 join l in _db.LopHocs on h.Lhid equals l.Idlh
                                 join n in _db.NhanViens on h.Nvid equals n.Id
                                 join p in _db.PhongBans on n.IdphongBan equals p.IdphongBan
                                 join nd in _db.NoiDungDts on l.Ndid equals nd.Idnd into ndj
                                 from nd in ndj.DefaultIfEmpty()
                                 join lv in _db.LinhVucDts on nd.Lvdtid equals lv.Idlvdt into lvj
                                 from lv in lvj.DefaultIfEmpty()
                                 join v in _db.Vitris on n.IdviTri equals v.IdviTri into vj
                                 from v in vj.DefaultIfEmpty()
                                 where n.Id == id
                                 select new
                                 {
                                     h.Idht,
                                     p.IdphongBan,
                                     p.TenPhongBan,
                                     n.Id,
                                     MaNv          = n.MaNv,
                                     n.HoTen,
                                     TenViTri      = v != null ? v.TenViTri : null,
                                     l.Idlh,
                                     l.MaLh,
                                     l.TenLh,
                                     NoiDung       = nd != null ? nd.NoiDung : null,
                                     TenLvdt       = lv != null ? lv.TenLvdt : null,
                                     VideoNd       = nd != null ? nd.VideoNd : null,
                                     ImageNd       = nd != null ? nd.ImageNd : null,
                                     l.Tgbdlh,
                                     l.Tgktlh,
                                     h.NgayTg,
                                     h.NgayHt,
                                     h.Xntg,
                                     h.Xnht,
                                     l.ToChucThi,
                                     l.IddeThi,
                                     l.IsCoCtdt
                                 }).ToListAsync();

                model = res.Select(x => new EClassroomValidation
                {
                    IDHT        = x.Idht,
                    PBID        = x.IdphongBan,
                    TenPB       = x.TenPhongBan,
                    NVID        = x.Id,
                    MaNV        = x.MaNv,
                    HoTenHV    = x.HoTen,
                    TenVT       = x.TenViTri,
                    LHID        = x.Idlh,
                    MaLH        = x.MaLh,
                    TenLH       = x.TenLh,
                    TenND       = x.NoiDung,
                    LinhVuc     = x.TenLvdt,
                    VideoLH     = x.VideoNd,
                    ImageLH     = x.ImageNd,
                    TGBDLH      = x.Tgbdlh ?? default,
                    TGKTLH      = x.Tgktlh ?? default,
                    NgayTG      = x.NgayTg.HasValue ? x.NgayTg.Value.ToDateTime(TimeOnly.MinValue) : default,
                    NgayHT      = x.NgayHt.HasValue ? x.NgayHt.Value.ToDateTime(TimeOnly.MinValue) : default,
                    XNTG        = x.Xntg ?? false,
                    XNHT        = x.Xnht ?? false,
                    ToChucThi   = x.ToChucThi ?? false,
                    IDDeThi     = x.IddeThi,
                    ThiNhieuLan = x.IsCoCtdt ?? 0
                }).OrderBy(x => x.LHID).ToList();

                try { await _cache.SetStringAsync(classKey, JsonSerializer.Serialize(model), _classListOpts); } catch { }
            }

            // Tầng 2: điểm thi
            var lhIds    = model.Select(x => x.LHID).ToList();
            var deThiIds = model.Where(x => x.IDDeThi.HasValue)
                                .Select(x => x.IDDeThi!.Value).Distinct().ToList();

            // BaiThis: cache 30s — giảm tải đỉnh, chấp nhận trễ 30s so với điểm mới nhất
            List<BaiThi> baiThis;
            var baithiKey  = $"eclassroom:baithi:{id}";
            string? btJson = null;
            try { btJson = await _cache.GetStringAsync(baithiKey); } catch { }
            if (btJson != null)
            {
                baiThis = JsonSerializer.Deserialize<List<BaiThi>>(btJson) ?? new();
            }
            else
            {
                baiThis = await _db.BaiThis.AsNoTracking()
                    .Where(x => x.Idnv == id && lhIds.Contains(x.Idlh ?? 0))
                    .OrderBy(x => x.IdbaiThi).ToListAsync();
                try { await _cache.SetStringAsync(baithiKey, JsonSerializer.Serialize(baiThis), _baithiOpts); } catch { }
            }

            var btIds = baiThis.Select(b => b.IdbaiThi).ToList();

            // CtbaiThis: cache 30s cùng TTL với BaiThis — chỉ fetch khi có lịch sử thi
            List<CtbaiThi> ctBaiThis;
            if (btIds.Count == 0)
            {
                ctBaiThis = new List<CtbaiThi>();
            }
            else
            {
                var ctKey  = $"eclassroom:ctbaithi:{id}";
                string? ctJson = null;
                try { ctJson = await _cache.GetStringAsync(ctKey); } catch { }
                if (ctJson != null)
                {
                    ctBaiThis = JsonSerializer.Deserialize<List<CtbaiThi>>(ctJson) ?? new();
                }
                else
                {
                    ctBaiThis = await _db.CtbaiThis.AsNoTracking()
                        .Where(x => btIds.Contains((int)x.IdbaiThi)).ToListAsync();
                    try { await _cache.SetStringAsync(ctKey, JsonSerializer.Serialize(ctBaiThis), _baithiOpts); } catch { }
                }
            }

            // CauHoiDeThis: dữ liệu tĩnh — cache 1h per IDDeThi, dùng chung mọi user
            var cauHoiDeThis = new List<CauHoiDeThi>();
            if (deThiIds.Count > 0)
            {
                var missingIds = new List<int>();
                foreach (var dtId in deThiIds)
                {
                    string? dtJson = null;
                    try { dtJson = await _cache.GetStringAsync($"eclassroom:cauhoidethi:{dtId}"); } catch { }
                    if (dtJson != null)
                    {
                        var sub = JsonSerializer.Deserialize<List<CauHoiDeThi>>(dtJson);
                        if (sub != null) cauHoiDeThis.AddRange(sub);
                    }
                    else { missingIds.Add(dtId); }
                }
                if (missingIds.Count > 0)
                {
                    var fromDb = await _db.CauHoiDeThis.AsNoTracking()
                        .Where(x => missingIds.Contains(x.IddeThi ?? 0)).ToListAsync();
                    cauHoiDeThis.AddRange(fromDb);
                    foreach (var dtId in missingIds)
                    {
                        var subset = fromDb.Where(x => x.IddeThi == dtId).ToList();
                        try { await _cache.SetStringAsync($"eclassroom:cauhoidethi:{dtId}", JsonSerializer.Serialize(subset), _cauhoidethiOpts); } catch { }
                    }
                }
            }

            foreach (var m in model)
            {
                m.BaiThiCount  = 0;
                m.LastIDBaiThi = 0;
                m.LastDiemSo   = null;
                m.SoCauDung    = 0;
                m.TongSoCau    = 0;

                var mBaiThis = baiThis.Where(x => x.Idlh == m.LHID).ToList();
                m.BaiThiCount = mBaiThis.Count;
                if (mBaiThis.Count > 0)
                {
                    var lastBT = mBaiThis.Last();
                    m.LastIDBaiThi = lastBT.IdbaiThi;
                    m.LastDiemSo   = lastBT.DiemSo;
                    var lastCT     = ctBaiThis.Where(x => x.IdbaiThi == lastBT.IdbaiThi).ToList();
                    m.SoCauDung    = lastCT.Count(x => x.Diem != 0);
                    m.TongSoCau    = lastCT.Count;
                }
                m.TongDiem = cauHoiDeThis.Where(x => x.IddeThi == m.IDDeThi).Sum(x => x.Diem);
            }

            return View(model);
        }

        public async Task<IActionResult> PracticeHistory(int IDLH)
        {
            int id = _auth.ID;

            var tenLH = await _db.LopHocs.AsNoTracking()
                .Where(x => x.Idlh == IDLH)
                .Select(x => x.TenLh)
                .FirstOrDefaultAsync() ?? "";

            var res = await (from b in _db.BaiThis
                             join l in _db.LopHocs on b.Idlh equals l.Idlh
                             where b.Idnv == id && b.Idlh == IDLH && l.IsCoCtdt == 1
                             select new PracticeAttemptView
                             {
                                 IDBaiThi    = b.IdbaiThi,
                                 IDNV        = id,
                                 IDLH        = IDLH,
                                 TenLH       = l.TenLh,
                                 LanThi      = b.LanThi ?? 1,
                                 DiemSo      = b.DiemSo,
                                 NgayThi     = b.NgayThi.HasValue
                                     ? b.NgayThi.Value.ToDateTime(TimeOnly.MinValue)
                                     : (DateTime?)null,
                                 ThoiGianThi = b.ThoiGianThi ?? 0
                             })
                             .OrderBy(x => x.LanThi)
                             .ToListAsync();

            ViewBag.TenLH = tenLH;
            return View(res);
        }

        public async Task<IActionResult> HistoryTest(int? page, int? IDLH, int? IDNV)
        {
            var res = await (from h in _db.BaiThis
                                 .Where(x => x.Idnv == IDNV && x.Idlh == IDLH)
                             join l in _db.LopHocs on h.Idlh equals l.Idlh
                             join n in _db.NhanViens on h.Idnv equals n.Id
                             select new HistoryTestView
                             {
                                 IDBaiThi    = h.IdbaiThi,
                                 IDNV        = n.Id,
                                 HoTen       = n.HoTen,
                                 IDLH        = l.Idlh,
                                 DiemThi     = h.DiemSo,
                                 NgayThi     = h.NgayThi.HasValue
                                     ? h.NgayThi.Value.ToDateTime(TimeOnly.MinValue)
                                     : (DateTime?)null,
                                 ThoiGianThi = h.ThoiGianThi ?? 0
                             }).ToListAsync();

            if (page == null) page = 1;
            return View(res.ToPagedList(page.Value, 1000));
        }

        public async Task<IActionResult> ViewResult(int? IDLH, int? IDBaiThi)
        {
            // 1. Thử đọc quickresult từ Redis (RAM — có ngay sau submit)
            QuickResultDto? qr = null;
            try
            {
                var json = await _cache.GetStringAsync($"exam:quickresult:{_auth.ID}:{IDLH}");
                if (json != null)
                    qr = JsonSerializer.Deserialize<QuickResultDto>(json);
            }
            catch { }

            // 2. Resolve IDBaiThi: URL param → Redis (consumer ghi xong) → null
            if (!IDBaiThi.HasValue)
            {
                try
                {
                    var val = await _mux.GetDatabase().StringGetAsync($"exam:result:{_auth.ID}:{IDLH}");
                    if (val.HasValue && int.TryParse(val.ToString(), out int idbt))
                        IDBaiThi = idbt;
                }
                catch { }
            }

            // 3. Redis có quickresult → hiển thị ngay từ RAM
            if (qr != null)
            {
                var gioBatDau = qr.TGBDLamBaiThi > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(qr.TGBDLamBaiThi).LocalDateTime
                    : (DateTime?)null;
                var gioKetThuc = qr.GioKetThucMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(qr.GioKetThucMs).LocalDateTime
                    : (DateTime?)null;

                ViewBag.IDLH            = IDLH;
                ViewBag.IDBaiThi        = IDBaiThi;   // null nếu DB chưa xong → view sẽ poll
                ViewBag.IDNV            = _auth.ID;
                ViewBag.DiemThi         = $"{qr.DiemSo:0.##}/100";
                ViewBag.ThoiGianThiGiay = qr.ThoiGianSec;
                ViewBag.GioBatDau       = gioBatDau;
                ViewBag.GioKetThuc      = gioKetThuc;
                ViewBag.TaiKhoan        = qr.TaiKhoan;
                ViewBag.HoTen           = qr.HoTen;
                ViewBag.TongSoCauHoi    = qr.TongSoCau;
                ViewBag.SoCauDung       = qr.SoCauDung;
                ViewBag.SoCauSai        = qr.SoCauSai;
                ViewBag.SoCauChuaTraLoi = qr.SoCauChuaTraLoi;
                return View();
            }

            // 4. Fallback: Redis hết hạn hoặc vào từ lịch sử thi → load từ DB
            ViewBag.IDLH     = IDLH;
            ViewBag.IDBaiThi = IDBaiThi;
            ViewBag.IDNV     = _auth.ID;

            var baithi = await _db.BaiThis.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdbaiThi == IDBaiThi);

            // TongDiem: reuse cache eclassroom:cauhoidethi đã build ở Index
            double tongdiem = 0;
            if (baithi?.IddeThi is int idDeThi)
            {
                string? dtJson = null;
                try { dtJson = await _cache.GetStringAsync($"eclassroom:cauhoidethi:{idDeThi}"); } catch { }
                if (dtJson != null)
                {
                    var cached = JsonSerializer.Deserialize<List<CauHoiDeThi>>(dtJson);
                    tongdiem = cached?.Sum(x => x.Diem) ?? 0;
                }
                else
                {
                    var cauHois = await _db.CauHoiDeThis.AsNoTracking()
                        .Where(x => x.IddeThi == idDeThi).ToListAsync();
                    tongdiem = cauHois.Sum(x => x.Diem ?? 0);
                    try { await _cache.SetStringAsync($"eclassroom:cauhoidethi:{idDeThi}", JsonSerializer.Serialize(cauHois), _cauhoidethiOpts); } catch { }
                }
            }

            ViewBag.DiemThi         = (baithi?.DiemSo ?? 0) + "/" + tongdiem;
            ViewBag.ThoiGianThiGiay = baithi?.ThoiGianThi ?? 0;
            ViewBag.GioBatDau       = baithi?.GioBatDau;
            ViewBag.GioKetThuc      = baithi?.GioKetThuc;

            var nv = await _db.NhanViens.AsNoTracking()
                .Where(x => x.Id == _auth.ID)
                .Select(x => new { x.MaNv, x.HoTen })
                .FirstOrDefaultAsync();
            ViewBag.TaiKhoan = nv?.MaNv;
            ViewBag.HoTen    = nv?.HoTen;

            var ctBaiThis = await _db.CtbaiThis.AsNoTracking()
                .Where(x => x.IdbaiThi == IDBaiThi)
                .ToListAsync();
            ViewBag.TongSoCauHoi    = ctBaiThis.Count;
            ViewBag.SoCauDung       = ctBaiThis.Count(x => (x.Diem ?? 0) != 0);
            ViewBag.SoCauChuaTraLoi = ctBaiThis.Count(x => x.IddapAnNv == null);
            ViewBag.SoCauSai        = ctBaiThis.Count - (int)ViewBag.SoCauDung - (int)ViewBag.SoCauChuaTraLoi;

            return View();
        }
    }
}
