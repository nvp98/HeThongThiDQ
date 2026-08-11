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

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var onLuyenLhIds = await _db.LopHocs
                .Where(l => l.IsCoCtdt == 1)
                .Select(l => l.Idlh)
                .ToListAsync();

            var TongDangKy = await _db.XnhocTaps
                .Where(x => onLuyenLhIds.Contains(x.Lhid ?? 0))
                .Select(x => x.Nvid)
                .Distinct()
                .CountAsync();

            var TongHoanThanh = await _db.BaiThis
                .Where(b => b.Idnv != null && onLuyenLhIds.Contains(b.Idlh ?? 0))
                .Select(b => b.Idnv)
                .Distinct()
                .CountAsync();

            var TongDonVi = await (from x in _db.XnhocTaps
                                   join n in _db.NhanViens on x.Nvid equals n.Id
                                   where onLuyenLhIds.Contains(x.Lhid ?? 0) && n.IdphongBan != null
                                   select n.IdphongBan).Distinct().CountAsync();

            return Json(new { TongDonVi, TongDangKy, TongHoanThanh });
        }

        [HttpGet]
        public async Task<IActionResult> GetData(string? search)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.V)) return Json("Unauthorized");

            var onLuyenLhIds = await _db.LopHocs
                .Where(l => l.IsCoCtdt == 1)
                .Select(l => l.Idlh)
                .ToListAsync();

            if (onLuyenLhIds.Count == 0) return Json(Array.Empty<object>());

            // Mẫu số: CBCNV đăng ký ít nhất 1 lớp ôn luyện, nhóm theo phòng ban
            var enrolled = await (from x in _db.XnhocTaps
                                  join n in _db.NhanViens on x.Nvid equals n.Id
                                  join pb in _db.PhongBans on n.IdphongBan equals pb.IdphongBan into pbj
                                  from pb in pbj.DefaultIfEmpty()
                                  where onLuyenLhIds.Contains(x.Lhid ?? 0)
                                  select new
                                  {
                                      IDPhongBan = n.IdphongBan ?? 0,
                                      TenPB      = pb != null ? pb.TenPhongBan : "Chưa phân công",
                                      IDNV       = n.Id
                                  }).ToListAsync();

            // Tử số: CBCNV đã nộp ít nhất 1 bài ôn luyện
            var completedSet = (await _db.BaiThis
                .Where(b => b.Idnv != null && onLuyenLhIds.Contains(b.Idlh ?? 0))
                .Select(b => b.Idnv!.Value)
                .Distinct()
                .ToListAsync()).ToHashSet();

            var ranked = enrolled
                .GroupBy(x => new { x.IDPhongBan, x.TenPB })
                .Select(g =>
                {
                    var nvIds = g.Select(x => x.IDNV).Distinct().ToList();
                    var total = nvIds.Count;
                    var done  = nvIds.Count(id => completedSet.Contains(id));
                    return new
                    {
                        PhongBan      = g.Key.TenPB,
                        TongDangKy    = total,
                        SoHoanThanh   = done,
                        TiLeHoanThanh = total > 0 ? Math.Round((double)done * 100 / total, 1) : 0.0
                    };
                })
                .OrderByDescending(x => x.TiLeHoanThanh)
                .ThenByDescending(x => x.SoHoanThanh)
                .ToList();

            var result = ranked
                .Select((x, i) => new
                {
                    Rank = i + 1,
                    x.PhongBan,
                    x.TongDangKy,
                    x.SoHoanThanh,
                    x.TiLeHoanThanh
                })
                .ToList();

            if (!string.IsNullOrEmpty(search))
                result = result.Where(x => x.PhongBan?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            return Json(result);
        }
    }
}
