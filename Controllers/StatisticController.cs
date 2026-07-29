using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class StatisticController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private const string ControllerName = "Statistic";

        public StatisticController(ELEARNINGEntities db, MyAuthentication auth, HomeController home)
        {
            _db = db;
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

            // Dùng COUNT trực tiếp trên SQL thay vì load toàn bảng
            var xnhtTotal = await _db.XnhocTaps.CountAsync();
            var xnhtDone  = await _db.XnhocTaps.CountAsync(x => x.Xnht == true);
            float tlxnht  = xnhtTotal > 0 ? xnhtDone * 100f / xnhtTotal : 0;

            var slhv   = await _db.NhanViens.CountAsync();
            var sllh   = await _db.LopHocs.CountAsync();
            var slch   = await _db.CauHois.CountAsync();
            var sldt   = await _db.DeThis.CountAsync(x => x.DiemChuan != 0 && x.TongSoCau != 0 && x.ThoiGianLamBai != 0);
            var slnddt = await _db.NoiDungDts.CountAsync();
            var sltgdt = await _db.NoiDungDts.SumAsync(x => (long?)x.ThoiLuongDt) ?? 0;
            var slgv   = await _db.NhanViens.CountAsync(x => x.Idquyen == 2 || x.Idquyen == 3);

            var res = new StatisticValidation
            {
                SLHV    = slhv,
                SLuotDT = xnhtTotal,
                SLLH    = sllh,
                SLVTKNL = 0,
                SLDG    = 0,
                TLHT    = tlxnht,
                SLCH    = slch,
                SLDT    = sldt,
                SLNDDT  = slnddt,
                SLTGDT  = (int)sltgdt,
                SLGV    = slgv
            };

            return View(res);
        }

        public async Task<JsonResult> GetLVDT()
        {
            var lvdt = await _db.LinhVucDts.AsNoTracking().ToListAsync();

            // GroupBy trên SQL thay vì load toàn bảng NoiDungDts
            var ndCounts = await _db.NoiDungDts
                .GroupBy(x => x.Lvdtid)
                .Select(g => new { Lvdtid = g.Key, Count = g.Count() })
                .ToListAsync();

            int total = ndCounts.Sum(x => x.Count);
            if (total == 0) total = 1;

            var res = lvdt.Select(a =>
            {
                int count = ndCounts.FirstOrDefault(x => x.Lvdtid == a.Idlvdt)?.Count ?? 0;
                return new ChartLVDT
                {
                    TenLV    = a.TenLvdt,
                    GiaTri   = count,
                    TLGiaTri = count * 100f / total
                };
            }).ToList();

            return Json(res);
        }

        public async Task<JsonResult> GetNDDTBP()
        {
            var lvdt = await _db.LinhVucDts.AsNoTracking().ToListAsync();
            var phongBans = await _db.PhongBans.AsNoTracking()
                .Where(x => x.MaPb != "" && x.MaPb != null && x.MaPb != "Trùng lặp")
                .ToListAsync();

            // GroupBy trên SQL thay vì load toàn bảng NoiDungDts
            var ndGrouped = await _db.NoiDungDts
                .GroupBy(x => new { x.Bplid, x.Lvdtid })
                .Select(g => new { g.Key.Bplid, g.Key.Lvdtid, Count = g.Count() })
                .ToListAsync();

            var activePbIds = ndGrouped.Select(x => x.Bplid).Distinct().ToHashSet();
            var rePB = phongBans.Where(pb => activePbIds.Contains(pb.IdphongBan)).ToList();

            var listLVDT = lvdt.Select(a => new LVDT
            {
                name = a.TenLvdt,
                data = rePB.Select(pb =>
                    ndGrouped.FirstOrDefault(x => x.Bplid == pb.IdphongBan && x.Lvdtid == a.Idlvdt)?.Count ?? 0
                ).ToList()
            }).ToList();

            return Json(new ChartNDDT
            {
                ListTenBP = rePB.Select(x => x.TenPhongBan ?? "").ToList(),
                ListLVDT  = listLVDT
            });
        }

        public async Task<JsonResult> GetKQHT()
        {
            // COUNT trực tiếp thay vì ToList() rồi đếm trong C#
            var xntgTrue      = await _db.XnhocTaps.CountAsync(x => x.Xntg == true);
            var xntgXnhtTrue  = await _db.XnhocTaps.CountAsync(x => x.Xntg == true && x.Xnht == true);
            var xntgFalse     = await _db.XnhocTaps.CountAsync(x => x.Xntg == false);

            return Json(new List<int> { xntgTrue, xntgXnhtTrue, xntgFalse });
        }

        public async Task<JsonResult> GetKQDT()
        {
            var phongBans = await _db.PhongBans.AsNoTracking()
                .Where(x => x.MaPb != "" && x.MaPb != null && x.MaPb != "Trùng lặp")
                .ToListAsync();

            // GroupBy trên SQL thay vì load toàn bảng BaiThis
            var btGrouped = await _db.BaiThis
                .GroupBy(x => new { x.IdphongBan, x.TinhTrang })
                .Select(g => new { g.Key.IdphongBan, g.Key.TinhTrang, Count = g.Count() })
                .ToListAsync();

            int datTotal  = btGrouped.Where(x => x.TinhTrang == true).Sum(x => x.Count);
            int kdatTotal = btGrouped.Where(x => x.TinhTrang == false).Sum(x => x.Count);

            var kqdat = phongBans
                .Select(pb => new KQDT
                {
                    name = pb.TenPhongBan,
                    data = btGrouped.FirstOrDefault(x => x.IdphongBan == pb.IdphongBan && x.TinhTrang == true)?.Count ?? 0
                })
                .Where(x => x.data > 0).ToList();

            var kqkdat = phongBans
                .Select(pb => new KQDT
                {
                    name = pb.TenPhongBan,
                    data = btGrouped.FirstOrDefault(x => x.IdphongBan == pb.IdphongBan && x.TinhTrang == false)?.Count ?? 0
                })
                .Where(x => x.data > 0).ToList();

            return Json(new ChartKQHT
            {
                Dat      = datTotal,
                KhongDat = kdatTotal,
                ListKQD  = kqdat,
                ListKQKD = kqkdat
            });
        }
    }
}
