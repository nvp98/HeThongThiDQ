using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class XepHangCaNhanController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private const string ControllerName = "XepHangCaNhan";

        public XepHangCaNhanController(ELEARNINGEntities db, MyAuthentication auth, HomeController home)
        {
            _db   = db;
            _auth = auth;
            _home = home;
        }

        public async Task<IActionResult> Index()
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

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

            return View();
        }

        // ── Thống kê tổng hợp ─────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetStats(int? idLH)
        {
            var allQ  = _db.BaiThis.AsQueryable();
            if (idLH.HasValue && idLH > 0) allQ = allQ.Where(x => x.Idlh == idLH);
            var doneQ = allQ.Where(x => x.DiemSo != null);

            var tongLuotThi   = await allQ.CountAsync();
            var tongHoanThanh = await doneQ.CountAsync();
            var tongNguoi     = await doneQ.Select(x => x.Idnv).Distinct().CountAsync();
            var tongDonVi     = await (from bt in doneQ
                                       join nv in _db.NhanViens on bt.Idnv equals nv.Id
                                       where nv.IdphongBan != null
                                       select nv.IdphongBan).Distinct().CountAsync();

            return Json(new { tongNguoi, tongDonVi, tongLuotThi, tongHoanThanh });
        }

        // ── Xếp hạng cá nhân ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetData(int? idLH, int? idPB, string? search)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.V)) return Json("Unauthorized");

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
                                 bt.TinhTrang,
                                 bt.GioBatDau,
                                 bt.GioKetThuc,
                                 bt.ThoiGianThi
                             }).ToListAsync();

            if (idPB.HasValue && idPB > 0)
                raw = raw.Where(x => x.IDPhongBan == idPB).ToList();

            if (!string.IsNullOrEmpty(search))
                raw = raw.Where(x => (x.HoTen?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                                  || (x.MaNv?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            var ranked = raw
                .GroupBy(x => new { x.IDNV, x.MaNv, x.HoTen, x.TenPB })
                .Select(g =>
                {
                    // Lấy lần thi đạt điểm cao nhất
                    var best = g.OrderByDescending(x => x.DiemSo).First();
                    int? giay = null;
                    if (best.GioBatDau.HasValue && best.GioKetThuc.HasValue)
                        giay = (int)(best.GioKetThuc.Value - best.GioBatDau.Value).TotalSeconds;
                    else if (best.ThoiGianThi.HasValue)
                        giay = best.ThoiGianThi.Value * 60;

                    var soLanThi = g.Count();
                    var soLanDat = g.Count(x => x.TinhTrang == true);
                    return new
                    {
                        g.Key.MaNv,
                        g.Key.HoTen,
                        PhongBan      = g.Key.TenPB,
                        DiemCaoNhat   = Math.Round(g.Max(x => x.DiemSo ?? 0), 1),
                        DiemTrungBinh = Math.Round(g.Average(x => x.DiemSo ?? 0), 1),
                        SoLanThi      = soLanThi,
                        SoLanDat      = soLanDat,
                        TiLeDat       = soLanThi > 0
                                        ? Math.Round((double)soLanDat * 100 / soLanThi, 1)
                                        : 0.0,
                        ThoiGian      = giay.HasValue
                                        ? $"{giay.Value / 60:D2}:{giay.Value % 60:D2}"
                                        : "--"
                    };
                })
                .OrderByDescending(x => x.DiemCaoNhat)
                .ThenBy(x => x.ThoiGian)
                .ToList();

            return Json(ranked.Select((x, i) => new
            {
                Rank = i + 1,
                x.MaNv, x.HoTen, x.PhongBan,
                x.DiemCaoNhat, x.DiemTrungBinh, x.TiLeDat,
                x.SoLanThi, x.SoLanDat, x.ThoiGian
            }));
        }
    }
}
