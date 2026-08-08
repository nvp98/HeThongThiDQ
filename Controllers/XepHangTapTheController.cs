using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class XepHangTapTheController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private const string ControllerName = "XepHangTapThe";

        public XepHangTapTheController(ELEARNINGEntities db, MyAuthentication auth, HomeController home)
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

            return View();
        }

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

            return Json(new { tongDonVi, tongNguoi, tongLuotThi, tongHoanThanh });
        }

        [HttpGet]
        public async Task<IActionResult> GetData(int? idLH, string? search)
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

            var result = teamRanked.Select((x, i) => new
            {
                Rank = i + 1,
                x.PhongBan, x.SoNhanVien, x.SoNguoiDat,
                x.DiemTrungBinh, x.DiemCaoNhat, x.TiLeDat
            }).ToList();

            if (!string.IsNullOrEmpty(search))
                result = result.Where(x => x.PhongBan?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            return Json(result);
        }
    }
}
