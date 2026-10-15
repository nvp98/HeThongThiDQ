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

            // Tham gia = đã có ít nhất 1 BaiThi trong bất kỳ lớp ôn luyện nào
            var doneLhPairs = await _db.BaiThis
                .Where(b => b.Idnv != null && onLuyenLhIds.Contains(b.Idlh ?? 0))
                .Select(b => new { IDNV = b.Idnv!.Value, IDLH = b.Idlh ?? 0 })
                .Distinct()
                .ToListAsync();

            var TongDangKy = doneLhPairs.Select(x => x.IDNV).Distinct().Count();

            // Hoàn thành = đã có BaiThi cho TẤT CẢ lớp ôn luyện
            var TongHoanThanh = doneLhPairs
                .GroupBy(x => x.IDNV)
                .Count(g => g.Select(x => x.IDLH).Distinct().Count() == onLuyenLhIds.Count);

            var TongDonVi = await (from x in _db.XnhocTaps
                                   join n in _db.NhanViens on x.Nvid equals n.Id
                                   where onLuyenLhIds.Contains(x.Lhid ?? 0) && n.IdphongBan != null
                                   select n.IdphongBan).Distinct().CountAsync();

            return Json(new { TongDonVi, TongDangKy, TongHoanThanh });
        }

        [HttpGet]
        public async Task<IActionResult> GetStatsThiChinhThuc()
        {
            var chiThucLhIds = await _db.LopHocs
                .Where(l => l.IsCoCtdt == 0)
                .Select(l => l.Idlh)
                .ToListAsync();

            var TongNguoiThi = await _db.BaiThis
                .Where(b => b.Idnv != null && chiThucLhIds.Contains(b.Idlh ?? 0) && b.DiemSo != null)
                .Select(b => b.Idnv)
                .Distinct()
                .CountAsync();

            var TongNguoiDat = await _db.BaiThis
                .Where(b => b.Idnv != null && chiThucLhIds.Contains(b.Idlh ?? 0) && b.TinhTrang == true && b.DiemSo >= 21)
                .Select(b => b.Idnv)
                .Distinct()
                .CountAsync();

            var TongDonVi = await (from bt in _db.BaiThis
                                   join nv in _db.NhanViens on bt.Idnv equals nv.Id
                                   where chiThucLhIds.Contains(bt.Idlh ?? 0) && bt.DiemSo != null && nv.IdphongBan != null
                                   select nv.IdphongBan).Distinct().CountAsync();

            return Json(new { TongDonVi, TongNguoiThi, TongNguoiDat });
        }

        // Khoảng thời gian lọc: "day" = hôm nay, "week" = từ thứ Hai tuần này, còn lại = toàn bộ
        private static (DateOnly? tu, DateOnly? den) GetKy(string? ky)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return ky switch
            {
                "day"  => (today, today),
                "week" => (today.AddDays(-(((int)today.DayOfWeek + 6) % 7)), today),
                _      => (null, null)
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetData(string? search, string? ky)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.V)) return Json("Unauthorized");

            var onLuyenLhIds = await _db.LopHocs
                .Where(l => l.IsCoCtdt == 1)
                .Select(l => l.Idlh)
                .ToListAsync();

            if (onLuyenLhIds.Count == 0) return Json(Array.Empty<object>());

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

            var (tu, den) = GetKy(ky);
            var coKy = tu != null;

            // Lượt ôn luyện + điểm trong kỳ (toàn bộ nếu không chọn kỳ)
            var baiThiKy = await _db.BaiThis
                .Where(b => b.Idnv != null && onLuyenLhIds.Contains(b.Idlh ?? 0)
                         && (!coKy || (b.NgayThi >= tu && b.NgayThi <= den)))
                .Select(b => new { IDNV = b.Idnv!.Value, b.DiemSo })
                .ToListAsync();

            var luotTheoNv = baiThiKy
                .GroupBy(b => b.IDNV)
                .ToDictionary(g => g.Key, g => g.Count());

            var diemTheoNv = baiThiKy
                .Where(b => b.DiemSo != null)
                .GroupBy(b => b.IDNV)
                .ToDictionary(g => g.Key, g => (Tong: g.Sum(x => x.DiemSo!.Value), SoBai: g.Count()));

            // Hoàn thành = có BaiThi cho TẤT CẢ lớp ôn luyện (luỹ kế, không phụ thuộc kỳ)
            var doneLhPerNv = await _db.BaiThis
                .Where(b => b.Idnv != null && onLuyenLhIds.Contains(b.Idlh ?? 0))
                .Select(b => new { IDNV = b.Idnv!.Value, IDLH = b.Idlh ?? 0 })
                .Distinct()
                .ToListAsync();

            var totalLhCount = onLuyenLhIds.Count;
            var completedSet = doneLhPerNv
                .GroupBy(x => x.IDNV)
                .Where(g => g.Select(x => x.IDLH).Distinct().Count() == totalLhCount)
                .Select(g => g.Key)
                .ToHashSet();

            var ranked = enrolled
                .GroupBy(x => new { x.IDPhongBan, x.TenPB })
                .Select(g =>
                {
                    var nvIds   = g.Select(x => x.IDNV).Distinct().ToHashSet();
                    var soCbnv  = nvIds.Count;
                    var thamGia = nvIds.Count(id => luotTheoNv.ContainsKey(id));
                    var done    = nvIds.Count(id => completedSet.Contains(id));
                    var luot    = nvIds.Sum(id => luotTheoNv.TryGetValue(id, out var c) ? c : 0);
                    var diem    = nvIds.Where(diemTheoNv.ContainsKey).Select(id => diemTheoNv[id]).ToList();
                    var soBai   = diem.Sum(d => d.SoBai);
                    return new
                    {
                        PhongBan      = g.Key.TenPB,
                        SoCBNV        = soCbnv,
                        TongDangKy    = thamGia,
                        SoHoanThanh   = done,
                        SoLuotOnLuyen = luot,
                        DiemTB        = soBai > 0 ? Math.Round(diem.Sum(d => d.Tong) / soBai, 1) : 0.0,
                        TiLeHoanThanh = soCbnv > 0 ? Math.Round((double)done * 100 / soCbnv, 1) : 0.0
                    };
                });

            // Toàn bộ: xếp theo tỷ lệ hoàn thành; theo ngày/tuần: xếp theo mức độ ôn luyện trong kỳ
            ranked = coKy
                ? ranked.OrderByDescending(x => x.SoLuotOnLuyen).ThenByDescending(x => x.DiemTB).ThenByDescending(x => x.TiLeHoanThanh)
                : ranked.OrderByDescending(x => x.TiLeHoanThanh).ThenByDescending(x => x.SoHoanThanh).ThenByDescending(x => x.SoLuotOnLuyen);

            var result = ranked
                .Select((x, i) => new
                {
                    Rank = i + 1,
                    SoBoDe = totalLhCount,
                    x.PhongBan,
                    x.SoCBNV,
                    x.TongDangKy,
                    x.SoHoanThanh,
                    x.SoLuotOnLuyen,
                    x.DiemTB,
                    x.TiLeHoanThanh
                })
                .ToList();

            if (!string.IsNullOrEmpty(search))
                result = result.Where(x => x.PhongBan?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDataThiChinhThuc(string? search, string? ky)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.V)) return Json("Unauthorized");

            var chiThucLhIds = await _db.LopHocs
                .Where(l => l.IsCoCtdt == 0)
                .Select(l => l.Idlh)
                .ToListAsync();

            if (chiThucLhIds.Count == 0) return Json(Array.Empty<object>());

            var (tu, den) = GetKy(ky);
            var coKy = tu != null;

            var baiThiRaw = await _db.BaiThis
                .Where(b => b.Idnv != null && chiThucLhIds.Contains(b.Idlh ?? 0) && b.DiemSo != null
                         && (!coKy || (b.NgayThi >= tu && b.NgayThi <= den)))
                .Select(b => new { Idnv = b.Idnv!.Value, DiemSo = b.DiemSo ?? 0, TinhTrang = b.TinhTrang ?? false })
                .ToListAsync();

            if (baiThiRaw.Count == 0) return Json(Array.Empty<object>());

            var nvIds = baiThiRaw.Select(b => b.Idnv).Distinct().ToList();
            var nvPbDict = await (from n in _db.NhanViens
                                  join pb in _db.PhongBans on n.IdphongBan equals pb.IdphongBan into pbj
                                  from pb in pbj.DefaultIfEmpty()
                                  where nvIds.Contains(n.Id)
                                  select new
                                  {
                                      IDNV       = n.Id,
                                      IDPhongBan = n.IdphongBan ?? 0,
                                      TenPB      = pb != null ? pb.TenPhongBan : "Chưa phân công"
                                  })
                                 .ToDictionaryAsync(x => x.IDNV);

            var enriched = baiThiRaw.Select(b =>
            {
                nvPbDict.TryGetValue(b.Idnv, out var pb);
                return new { b.Idnv, b.DiemSo, b.TinhTrang, IDPhongBan = pb?.IDPhongBan ?? 0, TenPB = pb?.TenPB ?? "Chưa phân công" };
            }).ToList();

            var ranked = enriched
                .GroupBy(x => new { x.IDPhongBan, x.TenPB })
                .Select(g =>
                {
                    var byPerson   = g.GroupBy(x => x.Idnv)
                                      .Select(pg => new { DiemCao = pg.Max(x => x.DiemSo), CoLanDat = pg.Any(x => x.TinhTrang && x.DiemSo >= 21) })
                                      .ToList();
                    var soNguoiThi = byPerson.Count;
                    var soNguoiDat = byPerson.Count(x => x.CoLanDat);
                    var diemTbCao  = soNguoiThi > 0 ? Math.Round(byPerson.Average(x => x.DiemCao), 1) : 0.0;
                    var tiLeDat    = soNguoiThi > 0 ? Math.Round((double)soNguoiDat * 100 / soNguoiThi, 1) : 0.0;
                    return new { PhongBan = g.Key.TenPB, SoNguoiThi = soNguoiThi, SoNguoiDat = soNguoiDat, DiemTbCao = diemTbCao, TiLeDat = tiLeDat };
                })
                .OrderByDescending(x => x.TiLeDat)
                .ThenByDescending(x => x.DiemTbCao)
                .ThenByDescending(x => x.SoNguoiDat)
                .ToList();

            var result = ranked.Select((x, i) => new
            {
                Rank = i + 1,
                x.PhongBan,
                x.SoNguoiThi,
                x.SoNguoiDat,
                x.DiemTbCao,
                x.TiLeDat
            }).ToList();

            if (!string.IsNullOrEmpty(search))
                result = result.Where(x => x.PhongBan?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            return Json(result);
        }
    }
}
