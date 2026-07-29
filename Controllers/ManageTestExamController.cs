using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class ManageTestExamController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private const string ControllerName = "ManageTestExam";

        public ManageTestExamController(ELEARNINGEntities db, MyAuthentication auth, HomeController home)
        {
            _db = db;
            _auth = auth;
            _home = home;
        }

        public async Task<IActionResult> Index(int? page, string? search, int? IDND)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            int id = _auth.ID;
            IDND   ??= 0;
            search ??= "";
            ViewBag.search = search;
            bool isGv = listQuyen.Contains(CONSTKEY.V_GV);

            var res = await (from b in _db.NoiDungDts
                             where (search == "" || b.NoiDung!.Contains(search) || b.MaNd!.Contains(search))
                             join nd in _db.NoiDungDts on b.Idnd equals nd.Idnd
                             join dt in _db.DeThis on b.Idnd equals dt.Idnd
                             where (IDND == 0 || dt.Idnd == IDND)
                             join nv in _db.NhanViens on dt.Gvid equals nv.Id
                             where (!isGv || nv.Id == id)
                             select new ManageTestExamValidation
                             {
                                 IDDeThi       = dt.IddeThi,
                                 MaDe          = dt.MaDe,
                                 TenDe         = dt.TenDe,
                                 DiemChuan     = dt.DiemChuan ?? 0,
                                 TongSoCau     = dt.TongSoCau ?? 0,
                                 ThoiGianLamBai = dt.ThoiGianLamBai ?? 0,
                                 IDND          = b.Idnd,
                                 TenND         = nd.NoiDung,
                                 GVID          = nv.Id,
                                 HoTen         = nv.HoTen,
                                 IsGV          = nv.IsGv ?? false
                             }).OrderByDescending(x => x.IDDeThi).ToListAsync();

            ViewBag.IDND = new SelectList(await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung");

            int pageNumber = page ?? 1;
            return View(res.ToPagedList(pageNumber, 20));
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.BDList = new SelectList(await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung");
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ManageTestExamValidation _DO)
        {
            try
            {
                _db.DeThis.Add(new DeThi
                {
                    MaDe           = _DO.MaDe,
                    TenDe          = _DO.TenDe,
                    DiemChuan      = _DO.DiemChuan,
                    ThoiGianLamBai = _DO.ThoiGianLamBai,
                    Idnd           = _DO.IDND,
                    Gvid           = _auth.ID,
                    TongSoCau      = _DO.TongSoCau
                });
                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageTestExam");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dt = await _db.DeThis.FirstOrDefaultAsync(x => x.IddeThi == id);
            if (dt == null) return NotFound();

            var DO = new ManageTestExamValidation
            {
                IDDeThi        = dt.IddeThi,
                MaDe           = dt.MaDe,
                TenDe          = dt.TenDe,
                DiemChuan      = dt.DiemChuan ?? 0,
                TongSoCau      = dt.TongSoCau ?? 0,
                ThoiGianLamBai = dt.ThoiGianLamBai ?? 0,
                GVID           = dt.Gvid ?? 0,
                IDND           = dt.Idnd ?? 0
            };

            ViewBag.BDList = new SelectList(await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung", DO.IDND);
            return PartialView(DO);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ManageTestExamValidation _DO)
        {
            try
            {
                var dt = await _db.DeThis.FirstOrDefaultAsync(x => x.IddeThi == _DO.IDDeThi);
                if (dt != null)
                {
                    dt.MaDe           = _DO.MaDe;
                    dt.TenDe          = _DO.TenDe;
                    dt.DiemChuan      = _DO.DiemChuan;
                    dt.ThoiGianLamBai = _DO.ThoiGianLamBai;
                    dt.Idnd           = _DO.IDND;
                    dt.TongSoCau      = _DO.TongSoCau;
                    await _db.SaveChangesAsync();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageTestExam");
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var inClass = await _db.LopHocs.AnyAsync(x => x.IddeThi == id);
                if (inClass)
                {
                    TempData["msgSuccess"] = "<script>alert('Đề thi đã được gắn vào lớp học');</script>";
                }
                else
                {
                    var dt = await _db.DeThis.FirstOrDefaultAsync(x => x.IddeThi == id);
                    if (dt != null) { _db.DeThis.Remove(dt); await _db.SaveChangesAsync(); }
                }
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Xóa dữ liệu thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageTestExam");
        }

        public async Task<IActionResult> Question(int id)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            var model = await (from a in _db.CauHoiDeThis.Where(x => x.IddeThi == id)
                               join b in _db.CauHois on a.IdcauHoi equals b.Idch into ul
                               from b in ul.DefaultIfEmpty()
                               join c in _db.DanhSachDa on b.Iddađung equals c.Iddsđa into uls
                               from c in uls.DefaultIfEmpty()
                               select new ManageQuestionValidation
                               {
                                   IDCH           = a.IdcauHoi ?? 0,
                                   NoiDungCH      = b != null ? b.NoiDungCh : null,
                                   DapAnA         = b != null ? b.DapAnA : null,
                                   DapAnB         = b != null ? b.DapAnB : null,
                                   DapAnC         = b != null ? b.DapAnC : null,
                                   DapAnD         = b != null ? b.DapAnD : null,
                                   DapAnDung      = c != null ? c.TenĐa : null,
                                   Diem           = a.Diem ?? 0,
                                   IDCauHoiDeThi  = a.IdcauHoiDeThi
                               }).ToListAsync();

            var dethi = await _db.DeThis.AsNoTracking().FirstOrDefaultAsync(x => x.IddeThi == id);
            var nd    = dethi != null ? await _db.NoiDungDts.AsNoTracking().FirstOrDefaultAsync(x => x.Idnd == dethi.Idnd) : null;
            ViewBag.TenDe   = dethi?.TenDe;
            ViewBag.NoiDung = nd?.NoiDung;
            ViewBag.IDDeThi = id;

            return View(model);
        }

        public IActionResult XuLyCauHoi(int? IDDethi)
        {
            ViewBag.IDDeThi = IDDethi;
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> XuLyCauHoi(LoginValidation _DO, int IDDethi, float? DiemChuan = 0.0f)
        {
            try
            {
                if (!string.IsNullOrEmpty(_DO.MaNV))
                {
                    string diemChuanRaw = Request.Form["DiemChuan"].ToString() ?? "0";
                    diemChuanRaw = diemChuanRaw.Replace(",", ".");
                    float.TryParse(diemChuanRaw, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float diemChuanVal);

                    string tx   = Regex.Replace(_DO.MaNV, @"[^0-9a-zA-Z\.]+", " ");
                    string[] nvs = tx.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    // Load tất cả CauHoi cần thiết trong 1 query thay vì N queries
                    var cauHoiMap = await _db.CauHois
                        .Where(x => nvs.Contains(x.MaCh))
                        .ToDictionaryAsync(x => x.MaCh!);

                    foreach (var item in nvs)
                    {
                        if (cauHoiMap.TryGetValue(item, out var ch))
                        {
                            _db.CauHoiDeThis.Add(new CauHoiDeThi
                            {
                                IdcauHoi = ch.Idch,
                                IddeThi  = IDDethi,
                                Diem     = diemChuanVal
                            });
                        }
                    }
                    await _db.SaveChangesAsync();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Question", "ManageTestExam", new { id = IDDethi });
        }

        public async Task<IActionResult> ViewQuestion(int id)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            var model = await (from a in _db.CauHoiDeThis.Where(x => x.IddeThi == id)
                               join b in _db.CauHois on a.IdcauHoi equals b.Idch
                               join c in _db.DanhSachDa on b.Iddađung equals c.Iddsđa
                               select new ManageQuestionValidation
                               {
                                   IDCH          = a.IdcauHoi ?? 0,
                                   NoiDungCH     = b.NoiDungCh,
                                   DapAnA        = b.DapAnA,
                                   DapAnB        = b.DapAnB,
                                   DapAnC        = b.DapAnC,
                                   DapAnD        = b.DapAnD,
                                   DapAnDung     = c.TenĐa,
                                   Diem          = a.Diem ?? 0,
                                   IDCauHoiDeThi = a.IdcauHoiDeThi
                               }).ToListAsync();

            var dethi = await _db.DeThis.AsNoTracking().FirstOrDefaultAsync(x => x.IddeThi == id);
            var nd    = dethi != null ? await _db.NoiDungDts.AsNoTracking().FirstOrDefaultAsync(x => x.Idnd == dethi.Idnd) : null;
            ViewBag.TenDe   = dethi?.TenDe;
            ViewBag.NoiDung = nd?.NoiDung;

            return View(model);
        }

        public async Task<IActionResult> AddQuestion(int id)
        {
            var dethi = await _db.DeThis.AsNoTracking().FirstOrDefaultAsync(x => x.IddeThi == id);
            if (dethi == null) return NotFound();

            var existingIds = await _db.CauHoiDeThis.Where(x => x.IddeThi == id)
                .Select(x => x.IdcauHoi).ToListAsync();

            var lch = await _db.CauHois.AsNoTracking()
                .Where(x => x.Idnd == dethi.Idnd && !existingIds.Contains(x.Idch))
                .Select(x => new { x.Idch, NoiDungCH = StripHTML(x.NoiDungCh ?? "") })
                .ToListAsync();

            ViewBag.CHList = new SelectList(lch, "Idch", "NoiDungCH");
            ViewBag.IDND = new SelectList(
                await _db.NoiDungDts.AsNoTracking().Where(x => x.Idnd == dethi.Idnd).ToListAsync(),
                "Idnd", "NoiDung", dethi.Idnd);

            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> AddQuestion(CauHoiDeThiValidation _DO, int? id)
        {
            var slCau  = await _db.CauHoiDeThis.CountAsync(x => x.IddeThi == id);
            var iddt   = await _db.DeThis.SingleOrDefaultAsync(x => x.IddeThi == id);
            var tong   = await _db.CauHoiDeThis.Where(x => x.IddeThi == id).SumAsync(x => x.Diem) ?? 0;
            var tongDiem = _DO.Diem + tong;

            try
            {
                if (slCau < iddt!.TongSoCau && tongDiem <= 10)
                {
                    _db.CauHoiDeThis.Add(new CauHoiDeThi
                    {
                        IdcauHoi = _DO.IDCauHoi,
                        IddeThi  = id,
                        Diem     = _DO.Diem
                    });
                    await _db.SaveChangesAsync();
                    TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
                }
                else if (slCau >= iddt.TongSoCau)
                    TempData["msgSuccess"] = "<script>alert('Vui lòng xem lại Tổng số câu hỏi');</script>";
                else
                    TempData["msgSuccess"] = "<script>alert('Vui lòng xem lại Tổng số điểm');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới: {e.Message}');</script>";
            }
            return RedirectToAction("Question", "ManageTestExam", new { id });
        }

        public async Task<IActionResult> DeleteQuestion(int? id)
        {
            var idb = await _db.CauHoiDeThis.Where(x => x.IdcauHoiDeThi == id)
                .Select(g => g.IddeThi).FirstOrDefaultAsync();
            try
            {
                var item = await _db.CauHoiDeThis.FirstOrDefaultAsync(x => x.IdcauHoiDeThi == id);
                if (item != null) { _db.CauHoiDeThis.Remove(item); await _db.SaveChangesAsync(); }
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Xóa dữ liệu thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Question", "ManageTestExam", new { id = idb });
        }

        public async Task<JsonResult> CheckCauHoi(string lsnv)
        {
            var list = new List<EmployeeValidation>();
            if (!string.IsNullOrEmpty(lsnv))
            {
                string tx    = Regex.Replace(lsnv, @"[^0-9a-zA-Z\.]+", " ");
                string[] nvs = tx.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var cauHoiMap = await _db.CauHois.AsNoTracking()
                    .Where(x => nvs.Contains(x.MaCh))
                    .ToDictionaryAsync(x => x.MaCh!);

                foreach (var item in nvs)
                {
                    if (cauHoiMap.TryGetValue(item, out var ch))
                    {
                        list.Add(new EmployeeValidation
                        {
                            MaNV  = ch.MaCh,
                            HoTen = ch.MaCh + " - " + ch.NoiDungCh
                        });
                    }
                }
            }
            return Json(list);
        }

        private static string StripHTML(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            return System.Net.WebUtility.HtmlDecode(
                Regex.Replace(html, "<[^>]+>|\\s{2}", ""));
        }
    }
}
