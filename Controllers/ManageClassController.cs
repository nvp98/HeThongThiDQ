using ClosedXML.Excel;
using ExcelDataReader;
using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class ManageClassController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private readonly IWebHostEnvironment _env;
        private const string ControllerName = "ManageClass";

        public ManageClassController(ELEARNINGEntities db, MyAuthentication auth, HomeController home, IWebHostEnvironment env)
        {
            _db = db;
            _auth = auth;
            _home = home;
            _env = env;
        }

        public async Task<IActionResult> Index(int? page, int? IDLH, int? IDPB, int? IDND)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            int id = _auth.ID;
            IDLH ??= 0; IDPB ??= 0; IDND ??= 0;

            var query = from l in _db.LopHocs
                        join n in _db.NoiDungDts on l.Ndid equals n.Idnd
                        join g in _db.NhanViens on l.Gvid equals g.Id
                        join pb in _db.PhongBans on g.IdphongBan equals pb.IdphongBan into pbj
                        from pb in pbj.DefaultIfEmpty()
                        join lv in _db.LinhVucDts on n.Lvdtid equals lv.Idlvdt into lvj
                        from lv in lvj.DefaultIfEmpty()
                        join d in _db.DeThis on l.IddeThi equals d.IddeThi into dj
                        from d in dj.DefaultIfEmpty()
                        select new
                        {
                            l.Idlh,
                            l.MaLh,
                            l.TenLh,
                            NDID = n.Idnd,
                            n.MaNd,
                            n.NoiDung,
                            TenLVDT = lv != null ? lv.TenLvdt : null,
                            n.VideoNd,
                            n.ImageNd,
                            l.QuyDt,
                            l.NamDt,
                            l.Tgbdlh,
                            l.Tgktlh,
                            IddeThi = l.IddeThi ?? 0,
                            TenDe = d != null ? d.TenDe : null,
                            GVID = g.Id,
                            g.HoTen,
                            MaNv = g.MaNv,
                            TenPhongBan = pb != null ? pb.TenPhongBan : null,
                            g.IsGv,
                            IDPB = g.IdphongBan
                        };

            if (IDPB != 0) query = query.Where(x => x.IDPB == IDPB);

            var raw = query.ToList();

            var hocTapCounts = _db.XnhocTaps.GroupBy(x => x.Lhid)
                .Select(g => new { Lhid = g.Key, Count = g.Count() }).ToList();
            var hoanThanhCounts = _db.BaiThis.Where(x => x.TinhTrang == true)
                .GroupBy(x => x.Idlh)
                .Select(g => new { Idlh = g.Key, Count = g.Count() }).ToList();

            var res = raw.Select(x => new ManageClassValidation
            {
                IDLH = x.Idlh,
                MaLH = x.MaLh,
                TenLH = x.TenLh,
                NDID = x.NDID,
                MaND = x.MaNd,
                TenND = x.NoiDung,
                LinhVuc = x.TenLVDT,
                VideoLH = x.VideoNd,
                ImageLH = x.ImageNd,
                QuyDT = x.QuyDt ?? 0,
                NamDT = x.NamDt ?? 0,
                TGBDLH = x.Tgbdlh ?? default,
                TGKTLH = x.Tgktlh ?? default,
                IDDeThi = x.IddeThi,
                TenDeThi = x.TenDe,
                GVID = x.GVID,
                TenGV = x.HoTen,
                MaGV = x.MaNv,
                TenPhongBan = x.TenPhongBan,
                IsGV = x.IsGv,
                TSLHV = hocTapCounts.FirstOrDefault(h => h.Lhid == x.Idlh)?.Count ?? 0,
                SLHT = hoanThanhCounts.FirstOrDefault(h => h.Idlh == x.Idlh)?.Count ?? 0,
                IDPB = x.IDPB
            }).ToList();

            if (listQuyen.Contains(CONSTKEY.V_GV))
                res = res.Where(x => x.GVID == id).ToList();

            ViewBag.IDPB = new SelectList(_db.PhongBans.ToList(), "IdphongBan", "TenPhongBan");
            ViewBag.IDLH = new SelectList(_db.LopHocs.ToList(), "Idlh", "TenLh");
            ViewBag.IDND = new SelectList(_db.NoiDungDts.ToList(), "Idnd", "NoiDung");

            if (IDLH != 0) res = res.Where(x => x.IDLH == IDLH).ToList();
            if (IDND != 0) res = res.Where(x => x.NDID == IDND).ToList();

            int pageNumber = page ?? 1;
            return View(res.OrderByDescending(x => x.TGBDLH).ToPagedList(pageNumber, 20));
        }

        public async Task<IActionResult> Create()
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.ADD))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            ViewBag.NDList = new SelectList(_db.NoiDungDts.ToList(), "Idnd", "NoiDung");

            var lastRecord = _db.LopHocs.OrderByDescending(c => c.Idlh).FirstOrDefault();
            int nextId = (lastRecord?.Idlh ?? 0) + 1;
            ViewBag.MaLH = nextId < 10 ? $"LH0000{nextId}" :
                           nextId < 100 ? $"LH000{nextId}" :
                           nextId < 1000 ? $"LH00{nextId}" :
                           nextId < 10000 ? $"LH0{nextId}" : $"LH{nextId}";

            return PartialView();
        }

        [HttpPost]
        public IActionResult Create(ManageClassValidation _DO)
        {
            int gvId = _auth.ID;
            try
            {
                if (IsLHAvailable(_DO.MaLH!))
                {
                    TempData["msgSuccess"] = "<script>alert('Lớp học đã tồn tại');</script>";
                    return RedirectToAction("Index", "ManageClass");
                }

                var lopHoc = new LopHoc
                {
                    MaLh = _DO.MaLH,
                    TenLh = _DO.TenLH,
                    Ndid = _DO.NDID,
                    QuyDt = _DO.QuyDT,
                    NamDt = _DO.NamDT,
                    Tgbdlh = _DO.TGBDLH,
                    Tgktlh = _DO.TGKTLH,
                    Gvid = gvId,
                    IddeThi = _DO.IDDeThi,
                    IsCoCtdt = _DO.IsThiNhieuLan ? 1 : 0
                };
                _db.LopHocs.Add(lopHoc);
                _db.SaveChanges();

                var file = Request.Form.Files["FileUpload"];
                if (file != null && file.Length > 0)
                {
                    ImportHocVien(file, _DO.MaLH!, lopHoc.Idlh);
                }

                TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới lớp học: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageClass");
        }

        private void ImportHocVien(IFormFile file, string maLH, int lhid)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            if (!file.FileName.EndsWith(".xls") && !file.FileName.EndsWith(".xlsx")) return;

            using var stream = file.OpenReadStream();
            IExcelDataReader reader = file.FileName.EndsWith(".xls")
                ? ExcelReaderFactory.CreateBinaryReader(stream)
                : ExcelReaderFactory.CreateOpenXmlReader(stream);
            DataSet result = reader.AsDataSet();
            DataTable dt = result.Tables[0];
            reader.Close();

            int dtc = 0, dtrung = 0;
            for (int i = 5; i < dt.Rows.Count; i++)
            {
                string maNV = dt.Rows[i][0].ToString()!;
                if (!IsHVAvailable(maLH, maNV) && CheckMaNV(maNV))
                {
                    var nv = _db.NhanViens.FirstOrDefault(x => x.MaNv == maNV);
                    if (nv != null)
                    {
                        _db.XnhocTaps.Add(new XnhocTap
                        {
                            Nvid = nv.Id,
                            Lhid = lhid,
                            NgayTg = DateOnly.FromDateTime(new DateTime(1, 1, 1)),
                            NgayHt = DateOnly.FromDateTime(new DateTime(1, 1, 1)),
                            Xntg = false,
                            Xnht = false,
                            Pbid = nv.IdphongBan,
                            Vtid = nv.IdviTri
                        });
                        dtc++;
                    }
                }
                else dtrung++;
            }
            _db.SaveChanges();

            string msg = dtc > 0 && dtrung > 0
                ? $"Import được {dtc} dòng dữ liệu, Có {dtrung} dòng trùng Mã NV"
                : dtc > 0 ? $"Import được {dtc} dòng dữ liệu"
                : dtrung > 0 ? $"Có {dtrung} dòng trùng Mã NV"
                : "File import không có dữ liệu";
            TempData["msgSuccess"] = $"<script>alert('{msg}');</script>";
        }

        public async Task<IActionResult> Edit(int id)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.EDIT))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            var l = _db.LopHocs.FirstOrDefault(x => x.Idlh == id);
            if (l == null) return NotFound();

            var DO = new ManageClassValidation
            {
                IDLH = l.Idlh,
                MaLH = l.MaLh,
                TenLH = l.TenLh,
                NDID = l.Ndid ?? 0,
                QuyDT = l.QuyDt ?? 0,
                NamDT = l.NamDt ?? 0,
                TGBDLH = l.Tgbdlh ?? default,
                TGKTLH = l.Tgktlh ?? default,
                IDDeThi = l.IddeThi ?? 0,
                IsThiNhieuLan = l.IsCoCtdt == 1
            };

            ViewBag.NDList = new SelectList(_db.NoiDungDts.ToList(), "Idnd", "NoiDung");
            ViewBag.DeThiList = new SelectList(
                _db.DeThis.Where(x => x.Idnd == DO.NDID).ToList(), "IddeThi", "TenDe", DO.IDDeThi);
            ViewBag.TGBDLH = DO.TGBDLH.ToString("yyyy-MM-dd HH:mm:ss");
            ViewBag.TGKTLH = DO.TGKTLH.ToString("yyyy-MM-dd HH:mm:ss");

            return PartialView(DO);
        }

        [HttpPost]
        public IActionResult Edit(ManageClassValidation _DO)
        {
            try
            {
                var l = _db.LopHocs.FirstOrDefault(x => x.Idlh == _DO.IDLH);
                if (l != null)
                {
                    l.MaLh = _DO.MaLH;
                    l.TenLh = _DO.TenLH;
                    l.Ndid = _DO.NDID;
                    l.QuyDt = _DO.QuyDT;
                    l.NamDt = _DO.NamDT;
                    l.Tgbdlh = _DO.TGBDLH;
                    l.Tgktlh = _DO.TGKTLH;
                    l.IddeThi = _DO.IDDeThi;
                    l.IsCoCtdt = _DO.IsThiNhieuLan ? 1 : 0;
                    _db.SaveChanges();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageClass");
        }

        public IActionResult Delete(int id)
        {
            try
            {
                var l = _db.LopHocs.FirstOrDefault(x => x.Idlh == id);
                if (l != null)
                {
                    _db.LopHocs.Remove(l);
                    _db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Xóa dữ liệu thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageClass");
        }

        public IActionResult ExportToExcelLH()
        {
            try
            {
                string fileNameMau = Path.Combine(_env.ContentRootPath, "App_Data", "BM_LH.xlsx");
                string fileNameTemp = Path.Combine(_env.ContentRootPath, "App_Data", "BM_LH_Temp.xlsx");
                using var workbook = new XLWorkbook(fileNameMau);
                var ws = workbook.Worksheet("LH");
                var data = GetDSLopHoc();
                int row = 4;
                foreach (var d in data)
                {
                    ws.Cell(row, "A").Value = row - 3;
                    ws.Cell(row, "B").Value = d.MaLH;
                    ws.Cell(row, "C").Value = d.TenLH;
                    ws.Cell(row, "D").Value = d.LinhVuc;
                    ws.Cell(row, "E").Value = d.TenND;
                    ws.Cell(row, "F").Value = d.QuyDT;
                    ws.Cell(row, "G").Value = d.NamDT;
                    ws.Cell(row, "H").Value = d.TGBDLH;
                    ws.Cell(row, "I").Value = d.TGKTLH;
                    ws.Cell(row, "J").Value = d.TenDeThi;
                    ws.Cell(row, "K").Value = d.SLHT;
                    ws.Cell(row, "L").Value = d.TenGV;
                    ws.Cell(row, "M").Value = d.TenPhongBan;
                    row++;
                }
                workbook.SaveAs(fileNameTemp);
                byte[] fileBytes = System.IO.File.ReadAllBytes(fileNameTemp);
                string fileName = $"DSLopHoc_{DateTime.Now:dd-MM-yyyy}.xlsx";
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["msg"] = $"<script>alert('{ex.Message}');window.location.href='/ManageClass'</script>";
                return RedirectToAction("Index", "ManageClass");
            }
        }

        public IActionResult ExportToExcel()
        {
            try
            {
                string fileNameMau = Path.Combine(_env.ContentRootPath, "App_Data", "BM_DSHT.xlsx");
                byte[] fileBytes = System.IO.File.ReadAllBytes(fileNameMau);
                return File(fileBytes, "application/vnd.ms-excel", "BM_DSHT.xlsx");
            }
            catch (Exception ex)
            {
                TempData["msg"] = $"<script>alert('{ex.Message}');window.location.href='/ManageClass'</script>";
                return RedirectToAction("Index", "ManageClass");
            }
        }

        public JsonResult GetDeThi(int id)
        {
            var list = (from d in _db.DeThis.Where(x => x.Idnd == id)
                        select new ManageTestExamValidation
                        {
                            IDDeThi = d.IddeThi,
                            TenDe = d.TenDe
                        }).ToList();
            return Json(list);
        }

        private List<ManageClassValidation> GetDSLopHoc()
        {
            var hocTapCounts = _db.XnhocTaps.GroupBy(x => x.Lhid)
                .Select(g => new { Lhid = g.Key, Count = g.Count() }).ToList();
            var hoanThanhCounts = _db.BaiThis.Where(x => x.TinhTrang == true)
                .GroupBy(x => x.Idlh)
                .Select(g => new { Idlh = g.Key, Count = g.Count() }).ToList();

            var raw = (from l in _db.LopHocs
                       join n in _db.NoiDungDts on l.Ndid equals n.Idnd
                       join g in _db.NhanViens on l.Gvid equals g.Id
                       join pb in _db.PhongBans on g.IdphongBan equals pb.IdphongBan into pbj
                       from pb in pbj.DefaultIfEmpty()
                       join lv in _db.LinhVucDts on n.Lvdtid equals lv.Idlvdt into lvj
                       from lv in lvj.DefaultIfEmpty()
                       join d in _db.DeThis on l.IddeThi equals d.IddeThi
                       select new
                       {
                           l.Idlh, l.MaLh, l.TenLh, NDID = n.Idnd, n.MaNd, n.NoiDung,
                           TenLVDT = lv != null ? lv.TenLvdt : null,
                           n.VideoNd, n.ImageNd, l.QuyDt, l.NamDt, l.Tgbdlh, l.Tgktlh,
                           TenDe = d.TenDe, GVID = g.Id, g.HoTen, g.MaNv,
                           TenPhongBan = pb != null ? pb.TenPhongBan : null,
                           g.IsGv, IDPB = g.IdphongBan
                       }).ToList();

            return raw.Select(x => new ManageClassValidation
            {
                IDLH = x.Idlh, MaLH = x.MaLh, TenLH = x.TenLh,
                NDID = x.NDID, MaND = x.MaNd, TenND = x.NoiDung,
                LinhVuc = x.TenLVDT, VideoLH = x.VideoNd, ImageLH = x.ImageNd,
                QuyDT = x.QuyDt ?? 0, NamDT = x.NamDt ?? 0,
                TGBDLH = x.Tgbdlh ?? default, TGKTLH = x.Tgktlh ?? default,
                TenDeThi = x.TenDe, GVID = x.GVID, TenGV = x.HoTen, MaGV = x.MaNv,
                TenPhongBan = x.TenPhongBan, IsGV = x.IsGv,
                TSLHV = hocTapCounts.FirstOrDefault(h => h.Lhid == x.Idlh)?.Count ?? 0,
                SLHT = hoanThanhCounts.FirstOrDefault(h => h.Idlh == x.Idlh)?.Count ?? 0,
                IDPB = x.IDPB
            }).OrderBy(x => x.IDPB).ToList();
        }

        private bool IsLHAvailable(string maLH) =>
            _db.LopHocs.Any(l => l.MaLh!.ToLower() == maLH.ToLower());

        private bool IsHVAvailable(string maLH, string maNV) =>
            (from h in _db.XnhocTaps
             join l in _db.LopHocs on h.Lhid equals l.Idlh
             join n in _db.NhanViens on h.Nvid equals n.Id
             where l.MaLh!.ToLower() == maLH.ToLower() && n.MaNv!.ToLower() == maNV.ToLower()
             select h).Any();

        private bool CheckMaNV(string maNV) =>
            _db.NhanViens.Any(x => x.MaNv!.ToLower() == maNV.ToLower());
    }
}
