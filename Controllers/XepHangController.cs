using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class XepHangController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private const string ControllerName = "XepHang";

        public XepHangController(ELEARNINGEntities db, MyAuthentication auth, HomeController home)
        {
            _db   = db;
            _auth = auth;
            _home = home;
        }

        private async Task<bool> CheckView()
        {
            var q = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = q;
            return q.Contains(CONSTKEY.V);
        }

        private async Task LoadDropdowns()
        {
            ViewBag.LopHocList = await _db.LopHocs
                .Where(x => x.ToChucThi == true)
                .OrderByDescending(x => x.Tgbdlh)
                .Select(x => new { x.Idlh, x.TenLh })
                .ToListAsync();

            ViewBag.PhongBanList = await _db.PhongBans
                .Where(x => x.TenPhongBan != null)
                .OrderBy(x => x.TenPhongBan)
                .Select(x => new { x.IdphongBan, x.TenPhongBan })
                .ToListAsync();
        }

        // ── Redirect từ Index → CaNhan ─────────────────────────────────────────
        public IActionResult Index() => RedirectToAction("CaNhan");

        // ── Xếp hạng cá nhân ───────────────────────────────────────────────────
        public async Task<IActionResult> CaNhan()
        {
            if (!await CheckView())
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }
            await LoadDropdowns();
            return View();
        }

        // ── Xếp hạng tập thể ───────────────────────────────────────────────────
        public async Task<IActionResult> TapThe()
        {
            if (!await CheckView())
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }
            await LoadDropdowns();
            return View();
        }

        // ── API: dữ liệu cá nhân ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetCaNhan(int? idLH, int? idPB)
        {
            if (!await CheckView()) return Json("Unauthorized");

            var query = _db.BaiThis.Where(x => x.DiemSo != null);
            if (idLH.HasValue && idLH > 0) query = query.Where(x => x.Idlh == idLH);

            var raw = await (from bt in query
                             join nv in _db.NhanViens on bt.Idnv equals nv.Id
                             join pb in _db.PhongBans on nv.IdphongBan equals pb.IdphongBan into pbj
                             from pb in pbj.DefaultIfEmpty()
                             select new
                             {
                                 IDNV       = nv.Id,
                                 nv.MaNv,
                                 nv.HoTen,
                                 IDPhongBan = nv.IdphongBan,
                                 TenPB      = pb != null ? pb.TenPhongBan : "Chưa phân công",
                                 bt.DiemSo,
                                 bt.TinhTrang
                             }).ToListAsync();

            if (idPB.HasValue && idPB > 0)
                raw = raw.Where(x => x.IDPhongBan == idPB).ToList();

            var ranked = raw
                .GroupBy(x => new { x.IDNV, x.MaNv, x.HoTen, x.TenPB })
                .Select(g => new
                {
                    g.Key.MaNv,
                    g.Key.HoTen,
                    PhongBan      = g.Key.TenPB,
                    DiemCaoNhat   = Math.Round(g.Max(x => x.DiemSo ?? 0), 1),
                    DiemTrungBinh = Math.Round(g.Average(x => x.DiemSo ?? 0), 1),
                    SoLanThi      = g.Count(),
                    SoLanDat      = g.Count(x => x.TinhTrang == true),
                    TiLeDat       = g.Count() > 0
                                    ? Math.Round((double)g.Count(x => x.TinhTrang == true) * 100 / g.Count(), 1)
                                    : 0.0
                })
                .OrderByDescending(x => x.DiemCaoNhat)
                .ThenByDescending(x => x.TiLeDat)
                .ToList();

            return Json(ranked.Select((x, i) => new
            {
                Rank = i + 1,
                x.MaNv, x.HoTen, x.PhongBan,
                x.DiemCaoNhat, x.DiemTrungBinh,
                x.SoLanThi, x.SoLanDat, x.TiLeDat
            }));
        }

        // ── API: dữ liệu tập thể ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetTapThe(int? idLH)
        {
            if (!await CheckView()) return Json("Unauthorized");

            var query = _db.BaiThis.Where(x => x.DiemSo != null);
            if (idLH.HasValue && idLH > 0) query = query.Where(x => x.Idlh == idLH);

            var raw = await (from bt in query
                             join nv in _db.NhanViens on bt.Idnv equals nv.Id
                             join pb in _db.PhongBans on nv.IdphongBan equals pb.IdphongBan into pbj
                             from pb in pbj.DefaultIfEmpty()
                             select new
                             {
                                 IDPhongBan = nv.IdphongBan,
                                 TenPB      = pb != null ? pb.TenPhongBan : "Chưa phân công",
                                 IDNV       = nv.Id,
                                 bt.DiemSo,
                                 bt.TinhTrang
                             }).ToListAsync();

            var bestPerPerson = raw
                .GroupBy(x => new { x.IDPhongBan, x.TenPB, x.IDNV })
                .Select(g => new
                {
                    g.Key.IDPhongBan,
                    g.Key.TenPB,
                    DiemTot = g.Max(x => x.DiemSo ?? 0),
                    Dat     = g.Any(x => x.TinhTrang == true)
                });

            var teamRanked = bestPerPerson
                .GroupBy(x => new { x.IDPhongBan, x.TenPB })
                .Select(g => new
                {
                    PhongBan      = g.Key.TenPB,
                    SoNhanVien    = g.Count(),
                    SoNguoiDat    = g.Count(x => x.Dat),
                    DiemTrungBinh = Math.Round(g.Average(x => x.DiemTot), 1),
                    DiemCaoNhat   = Math.Round(g.Max(x => x.DiemTot), 1),
                    TiLeDat       = g.Count() > 0
                                    ? Math.Round((double)g.Count(x => x.Dat) * 100 / g.Count(), 1)
                                    : 0.0
                })
                .OrderByDescending(x => x.DiemTrungBinh)
                .ThenByDescending(x => x.TiLeDat)
                .ToList();

            return Json(teamRanked.Select((x, i) => new
            {
                Rank = i + 1,
                x.PhongBan, x.SoNhanVien, x.SoNguoiDat,
                x.DiemTrungBinh, x.DiemCaoNhat, x.TiLeDat
            }));
        }
    }
}
