using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class ManageETContentController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private readonly IWebHostEnvironment _env;
        private const string ControllerName = "ManageETContent";

        public ManageETContentController(ELEARNINGEntities db, MyAuthentication auth, HomeController home, IWebHostEnvironment env)
        {
            _db = db;
            _auth = auth;
            _home = home;
            _env  = env;
        }

        public async Task<IActionResult> Index(int? page, string? search, int? IDLVDT, int? IDCTLVDT)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            search    ??= "";
            ViewBag.search = search;
            IDCTLVDT ??= 0;
            IDLVDT   ??= 0;

            // Tính số lớp học cho mỗi nội dung trực tiếp trên SQL
            var sllhMap = await _db.LopHocs.AsNoTracking()
                .GroupBy(l => l.Ndid)
                .Select(g => new { Ndid = g.Key, Count = g.Count() })
                .ToListAsync();

            var raw = await (from n in _db.NoiDungDts
                             where (search == "" || n.NoiDung!.Contains(search) || n.MaNd!.Contains(search))
                                && (IDCTLVDT == 0 || n.Idctlvdt == IDCTLVDT)
                                && (IDLVDT == 0 || n.Lvdtid == IDLVDT)
                             join lv in _db.LinhVucDts on n.Lvdtid equals lv.Idlvdt into lvj
                             from lv in lvj.DefaultIfEmpty()
                             join ct in _db.Ctlvdts on n.Idctlvdt equals ct.Idctlvdt into ctj
                             from ct in ctj.DefaultIfEmpty()
                             join pb in _db.PhongBans on n.Bplid equals pb.IdphongBan into pbj
                             from pb in pbj.DefaultIfEmpty()
                             select new
                             {
                                 n.Idnd, n.MaNd, n.NoiDung, n.VideoNd, n.ImageNd,
                                 LVDTID      = n.Lvdtid ?? 0,
                                 TenLVDT     = lv != null ? lv.TenLvdt : null,
                                 IDCTLVDT2   = n.Idctlvdt ?? 0,
                                 TenCTLVDT   = ct != null ? ct.TenCtlvdt : null,
                                 TenPhongBan = pb != null ? pb.TenPhongBan : null,
                                 n.ThoiLuongDt, n.FileDinhKem, n.NgayTao
                             }).ToListAsync();

            var res = raw.Select(x => new ManageETContentValidation
            {
                IDND       = x.Idnd,
                MaND       = x.MaNd,
                NoiDung    = x.NoiDung,
                VideoND    = x.VideoNd,
                ImageND    = x.ImageNd,
                LVDTID     = x.LVDTID,
                LinhVuc    = x.TenLVDT,
                IDCTLVDT   = x.IDCTLVDT2,
                LVChiTiet  = x.TenCTLVDT,
                BPLNC      = x.TenPhongBan,
                ThoiLuongDT = x.ThoiLuongDt ?? 0,
                FileDinhKem = x.FileDinhKem,
                SLLH       = sllhMap.FirstOrDefault(s => s.Ndid == x.Idnd)?.Count ?? 0,
                NgayTao    = x.NgayTao.HasValue ? x.NgayTao.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null
            }).ToList();

            ViewBag.CTLVDTID = new SelectList(await _db.Ctlvdts.AsNoTracking().ToListAsync(), "Idctlvdt", "TenCtlvdt");
            ViewBag.IDLVDT   = new SelectList(await _db.LinhVucDts.AsNoTracking().ToListAsync(), "Idlvdt", "TenLvdt");

            int pageNumber = page ?? 1;
            return View(res.ToPagedList(pageNumber, 50));
        }

        public JsonResult GetLVChiTiet(int id)
        {
            var list = _db.Ctlvdts.AsNoTracking().Where(x => x.Lvdtid == id)
                .Select(x => new { IDCTLVDT = x.Idctlvdt, TenCTLVDT = x.TenCtlvdt }).ToList();
            return Json(list);
        }

        public async Task<IActionResult> Create()
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.ADD))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            var lastRecord = await _db.NoiDungDts.OrderByDescending(c => c.Idnd).FirstOrDefaultAsync();
            int nextId     = (lastRecord?.Idnd ?? 0) + 1;
            ViewBag.MaND   = nextId < 10 ? $"NDĐT0000{nextId}" :
                             nextId < 100 ? $"NDĐT000{nextId}" :
                             nextId < 1000 ? $"NDĐT00{nextId}" :
                             nextId < 10000 ? $"NDĐT0{nextId}" : $"NDĐT{nextId}";

            ViewBag.LVList  = new SelectList(await _db.LinhVucDts.AsNoTracking().ToListAsync(), "Idlvdt", "TenLvdt");
            ViewBag.BPLList = new SelectList(await _db.PhongBans.AsNoTracking().ToListAsync(), "IdphongBan", "TenPhongBan");

            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ManageETContentValidation _DO)
        {
            try
            {
                string path = Path.Combine(_env.WebRootPath, "UploadedFiles", "EduPro");
                Directory.CreateDirectory(path);

                if (_DO.PDFEduFile != null)
                {
                    string ext      = Path.GetExtension(_DO.PDFEduFile.FileName);
                    string fileName = _DO.MaND!.Trim() + ext;
                    using var fs    = new FileStream(Path.Combine(path, fileName), FileMode.Create);
                    _DO.PDFEduFile.CopyTo(fs);
                    _DO.FileDinhKem = "/UploadedFiles/EduPro/" + fileName;
                }

                if (await _db.NoiDungDts.AnyAsync(k => k.MaNd!.ToLower() == _DO.MaND!.ToLower()))
                {
                    TempData["msgSuccess"] = "<script>alert('Chương trình đã tồn tại');</script>";
                }
                else
                {
                    _db.NoiDungDts.Add(new NoiDungDt
                    {
                        MaNd        = _DO.MaND,
                        NoiDung     = _DO.NoiDung,
                        VideoNd     = _DO.VideoND,
                        ImageNd     = _DO.ImageND,
                        Bplid       = _DO.BPLID,
                        Lvdtid      = _DO.LVDTID,
                        Idctlvdt    = _DO.IDCTLVDT,
                        ThoiLuongDt = _DO.ThoiLuongDT,
                        FileDinhKem = _DO.FileDinhKem,
                        NgayTao     = _DO.NgayTao.HasValue
                            ? DateOnly.FromDateTime(_DO.NgayTao.Value) : (DateOnly?)null
                    });
                    await _db.SaveChangesAsync();
                    TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
                }
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageETContent");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.EDIT))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            var n = await _db.NoiDungDts.FirstOrDefaultAsync(x => x.Idnd == id);
            if (n == null) return NotFound();

            var DO = new ManageETContentValidation
            {
                IDND       = n.Idnd,
                MaND       = n.MaNd,
                NoiDung    = n.NoiDung,
                VideoND    = n.VideoNd,
                ImageND    = n.ImageNd,
                BPLID      = n.Bplid ?? 0,
                LVDTID     = n.Lvdtid ?? 0,
                IDCTLVDT   = n.Idctlvdt ?? 0,
                ThoiLuongDT = n.ThoiLuongDt ?? 0,
                FileDinhKem = n.FileDinhKem,
                NgayTao    = n.NgayTao.HasValue ? n.NgayTao.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null
            };

            ViewBag.LVList  = new SelectList(await _db.LinhVucDts.AsNoTracking().ToListAsync(), "Idlvdt", "TenLvdt", DO.LVDTID);
            ViewBag.CTLVList = new SelectList(
                await _db.Ctlvdts.AsNoTracking().Where(x => x.Lvdtid == DO.LVDTID).ToListAsync(),
                "Idctlvdt", "TenCtlvdt", DO.IDCTLVDT);
            ViewBag.BPLList = new SelectList(await _db.PhongBans.AsNoTracking().ToListAsync(), "IdphongBan", "TenPhongBan");
            ViewBag.NgayTao = DO.NgayTao?.ToString("yyyy-MM-dd");

            return PartialView(DO);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ManageETContentValidation _DO)
        {
            try
            {
                string path = Path.Combine(_env.WebRootPath, "UploadedFiles", "EduPro");
                Directory.CreateDirectory(path);

                if (_DO.PDFEduFile != null)
                {
                    string ext      = Path.GetExtension(_DO.PDFEduFile.FileName);
                    string fileName = _DO.MaND!.Trim() + ext;
                    using var fs    = new FileStream(Path.Combine(path, fileName), FileMode.Create);
                    _DO.PDFEduFile.CopyTo(fs);
                    _DO.FileDinhKem = "/UploadedFiles/EduPro/" + fileName;
                }

                var n = await _db.NoiDungDts.FirstOrDefaultAsync(x => x.Idnd == _DO.IDND);
                if (n != null)
                {
                    n.MaNd        = _DO.MaND;
                    n.NoiDung     = _DO.NoiDung;
                    n.VideoNd     = _DO.VideoND;
                    n.ImageNd     = _DO.ImageND;
                    n.Bplid       = _DO.BPLID;
                    n.Lvdtid      = _DO.LVDTID;
                    n.Idctlvdt    = _DO.IDCTLVDT;
                    n.ThoiLuongDt = _DO.ThoiLuongDT;
                    n.FileDinhKem = _DO.FileDinhKem;
                    n.NgayTao     = _DO.NgayTao.HasValue
                        ? DateOnly.FromDateTime(_DO.NgayTao.Value) : (DateOnly?)null;
                    await _db.SaveChangesAsync();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageETContent");
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var n = await _db.NoiDungDts.FirstOrDefaultAsync(x => x.Idnd == id);
                if (n != null) { _db.NoiDungDts.Remove(n); await _db.SaveChangesAsync(); }
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Xóa dữ liệu thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageETContent");
        }
    }
}
