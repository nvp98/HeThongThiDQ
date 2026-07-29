using ExcelDataReader;
using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class QuestionController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private readonly IWebHostEnvironment _env;
        private const string ControllerName = "Question";

        public QuestionController(ELEARNINGEntities db, MyAuthentication auth, HomeController home, IWebHostEnvironment env)
        {
            _db = db;
            _auth = auth;
            _home = home;
            _env  = env;
        }

        public async Task<IActionResult> Index(int? page, int? IDND, int? IDDaoCH)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            int id   = _auth.ID;
            IDND   ??= 0;
            bool isGv = listQuyen.Contains(CONSTKEY.V_GV);

            // Đẩy các filter xuống SQL thay vì load hết rồi lọc trong C#
            var res = await (from c in _db.CauHois
                             where (!isGv || c.Gvid == id)
                                && (IDND == 0 || c.Idnd == IDND)
                                && (IDDaoCH == null || (IDDaoCH == 1 ? c.IsDao == true : c.IsDao != true))
                             join d in _db.DanhSachDa on c.Iddađung equals d.Iddsđa into ul
                             from d in ul.DefaultIfEmpty()
                             select new ManageQuestionValidation
                             {
                                 IDCH      = c.Idch,
                                 NoiDungCH = c.NoiDungCh,
                                 DapAnA    = c.DapAnA,
                                 DapAnB    = c.DapAnB,
                                 DapAnC    = c.DapAnC,
                                 DapAnD    = c.DapAnD,
                                 IDDADung  = c.Iddađung ?? 0,
                                 DapAnDung = d != null ? d.TenĐa : null,
                                 IDND      = c.Idnd ?? 0,
                                 GVID      = c.Gvid ?? 0,
                                 MaCH      = c.MaCh,
                                 IsDao     = c.IsDao ?? false
                             }).ToListAsync();

            ViewBag.IDND = new SelectList(await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung", IDND);
            ViewBag.IDDaoCH = new List<SelectListItem>
            {
                new() { Value = "0", Text = "Không đảo" },
                new() { Value = "1", Text = "Đảo câu hỏi" }
            };

            int pageNumber = page ?? 1;
            return View(res.ToPagedList(pageNumber, 20));
        }

        public JsonResult GetCTLVDT(int IDLVDT)
        {
            var list = _db.Ctlvdts.AsNoTracking().Where(x => x.Lvdtid == IDLVDT)
                .OrderBy(x => x.Idctlvdt)
                .Select(x => new { x.Idctlvdt, x.TenCtlvdt, x.Lvdtid })
                .ToList();
            return Json(list);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.IDND  = new SelectList(await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung");
            ViewBag.DSList = new SelectList(await _db.DanhSachDa.AsNoTracking().ToListAsync(), "Iddsđa", "TenĐa");
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ManageQuestionValidation _DO)
        {
            try
            {
                var ch = new CauHoi
                {
                    NoiDungCh = _DO.NoiDungCH,
                    DapAnA    = _DO.DapAnA,
                    DapAnB    = _DO.DapAnB,
                    DapAnC    = _DO.DapAnC,
                    DapAnD    = _DO.DapAnD,
                    Iddađung  = _DO.IDDADung,
                    Idnd      = _DO.IDND,
                    Gvid      = _auth.ID,
                    MaCh      = _DO.MaCH,
                    IsDao     = _DO.IsDao
                };
                _db.CauHois.Add(ch);
                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "Question");
        }

        public IActionResult Upload()
        {
            var file = Request.Form.Files["file"];
            if (file == null || file.Length == 0)
                return Json(new { location = "" });

            string fileName = Path.GetFileName(file.FileName);
            string folder   = Path.Combine(_env.WebRootPath, "UploadedFiles", "ImagesUpload");
            Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, fileName);
            using var fs    = new FileStream(filePath, FileMode.Create);
            file.CopyTo(fs);
            return Json(new { location = "/UploadedFiles/ImagesUpload/" + fileName });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.CauHois.FirstOrDefaultAsync(x => x.Idch == id);
            if (c == null) return NotFound();

            var da = await _db.DanhSachDa.AsNoTracking().FirstOrDefaultAsync(x => x.Iddsđa == c.Iddađung);

            var DO = new ManageQuestionValidation
            {
                IDCH      = c.Idch,
                NoiDungCH = c.NoiDungCh,
                DapAnA    = c.DapAnA,
                DapAnB    = c.DapAnB,
                DapAnC    = c.DapAnC,
                DapAnD    = c.DapAnD,
                IDDADung  = c.Iddađung ?? 0,
                DapAnDung = da?.TenĐa,
                IDND      = c.Idnd ?? 0,
                MaCH      = c.MaCh,
                IsDao     = c.IsDao ?? false
            };

            ViewBag.IDND    = new SelectList(await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung", DO.IDND);
            ViewBag.IDDADung = new SelectList(await _db.DanhSachDa.AsNoTracking().ToListAsync(), "Iddsđa", "TenĐa", DO.IDDADung);

            return PartialView(DO);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ManageQuestionValidation _DO)
        {
            try
            {
                var c = await _db.CauHois.FirstOrDefaultAsync(x => x.Idch == _DO.IDCH);
                if (c != null)
                {
                    c.NoiDungCh = _DO.NoiDungCH;
                    c.DapAnA    = _DO.DapAnA;
                    c.DapAnB    = _DO.DapAnB;
                    c.DapAnC    = _DO.DapAnC;
                    c.DapAnD    = _DO.DapAnD;
                    c.Iddađung  = _DO.IDDADung;
                    c.Idnd      = _DO.IDND;
                    c.MaCh      = _DO.MaCH;
                    c.IsDao     = _DO.IsDao;
                    await _db.SaveChangesAsync();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại {e.Message}');</script>";
            }
            return RedirectToAction("Index", "Question");
        }

        public FileResult DownloadExcel()
        {
            string path = Path.Combine(_env.ContentRootPath, "App_Data", "Template_Question.xlsx");
            return File(path, "application/vnd.ms-excel", "Template_Question.xlsx");
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var c = await _db.CauHois.FirstOrDefaultAsync(x => x.Idch == id);
                if (c != null) { _db.CauHois.Remove(c); await _db.SaveChangesAsync(); }
            }
            catch
            {
                TempData["msgError"] = "<script>alert('Xóa dữ liệu thất bại');</script>";
            }
            return RedirectToAction("Index", "Question");
        }

        public async Task<IActionResult> ImportExcel()
        {
            ViewBag.IDND = new SelectList(await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung");
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcel(ManageQuestionValidation _DO)
        {
            var file = Request.Form.Files["FileUpload"];
            if (file == null || file.Length == 0)
            {
                TempData["msgSuccess"] = "<script>alert('Vui lòng nhập file Import');</script>";
                return RedirectToAction("Index", "Question");
            }

            if (!file.FileName.EndsWith(".xls") && !file.FileName.EndsWith(".xlsx"))
            {
                TempData["msg"] = "<script>alert('Vui lòng chọn đúng định dạng file Excel');</script>";
                return RedirectToAction("Index", "Question");
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            try
            {
                using var stream = file.OpenReadStream();
                IExcelDataReader reader = file.FileName.EndsWith(".xls")
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);
                DataSet result = reader.AsDataSet();
                DataTable dt   = result.Tables[0];
                reader.Close();

                for (int i = 5; i < dt.Rows.Count; i++)
                {
                    string maCH      = dt.Rows[i][1].ToString()!.Trim();
                    string noiDungCH = dt.Rows[i][2].ToString()!.Trim();
                    string dapAnA    = dt.Rows[i][3].ToString()!.Trim();
                    string dapAnB    = dt.Rows[i][4].ToString()!.Trim();
                    string dapAnC    = dt.Rows[i][5].ToString()!.Trim();
                    string dapAnD    = dt.Rows[i][6].ToString()!.Trim();
                    string dapAnDungRaw = dt.Rows[i][7].ToString()!.Trim().ToUpper();

                    int dapAnDung = dapAnDungRaw switch { "A" => 1, "B" => 2, "C" => 3, "D" => 4, _ => 1 };

                    _db.CauHois.Add(new CauHoi
                    {
                        NoiDungCh = noiDungCH,
                        DapAnA    = dapAnA,
                        DapAnB    = dapAnB,
                        DapAnC    = dapAnC,
                        DapAnD    = dapAnD,
                        Iddađung  = dapAnDung,
                        Idnd      = _DO.IDND,
                        Gvid      = _auth.ID,
                        MaCh      = maCH,
                        IsDao     = true
                    });
                }
                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = "<script>alert('Import dữ liệu thành công');</script>";
            }
            catch (Exception ex)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi Upload: {ex.Message}');</script>";
            }
            return RedirectToAction("Index", "Question");
        }
    }
}
