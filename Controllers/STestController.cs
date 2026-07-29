using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class STestController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;

        public STestController(ELEARNINGEntities db, MyAuthentication auth)
        {
            _db = db;
            _auth = auth;
        }

        public async Task<IActionResult> Index(int LHID)
        {
            var random = new Random();

            // Load LopHoc + DeThi trước để tránh join thừa trong query chính
            var lh = await _db.LopHocs.AsNoTracking().FirstOrDefaultAsync(x => x.Idlh == LHID);
            if (lh == null || lh.IddeThi == null) return RedirectToAction("Index", "EClassroom");

            var dethi = await _db.DeThis.AsNoTracking().FirstOrDefaultAsync(x => x.IddeThi == lh.IddeThi);

            var res = await (from cd in _db.CauHoiDeThis.Where(x => x.IddeThi == lh.IddeThi)
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

            res = res.OrderBy(_ => random.Next()).ToList();

            ViewBag.ThoiGianLamBai = DateTime.Now.AddMinutes(dethi?.ThoiGianLamBai ?? 0);
            ViewBag.ThoiGianThi    = dethi?.ThoiGianLamBai ?? 0;
            ViewBag.IDNV           = _auth.ID;
            ViewBag.IDLH           = LHID;

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
                        IdbaiThi     = IDBaiThi,
                        IdcauHoi     = Q.IDCH,
                        IddapAnDung  = Q.IDDADung,
                        IddapAnNv    = Q.Answer,
                        Diem         = Q.IDDADung == Q.Answer ? Q.Diem : 0
                    });
                    i++;
                }
                else
                {
                    _db.CtbaiThis.Add(new CtbaiThi
                    {
                        IdbaiThi     = IDBaiThi,
                        IdcauHoi     = Q.IDCH,
                        IddapAnDung  = Q.IDDADung,
                        IddapAnNv    = Q.Answer,
                        Diem         = Q.IDDADung == Q.Answer ? Q.Diem : 0
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
        public IActionResult AutoSave()
        {
            return Json(new { success = true });
        }

        public JsonResult AddEvent()
        {
            return Json(true);
        }
    }
}
