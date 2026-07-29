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
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class ConfirmEStudyController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IWebHostEnvironment _env;
        private const string ControllerName = "ConfirmEStudy";

        public ConfirmEStudyController(ELEARNINGEntities db, MyAuthentication auth, IWebHostEnvironment env)
        {
            _db = db;
            _auth = auth;
            _env = env;
        }

        public IActionResult Index(int id, int? page, int? IDPhongBan, string? search)
        {
            IDPhongBan ??= 0;
            search ??= "";
            ViewBag.search = search;
            ViewBag.IDLH = id;
            ViewBag.PBList = new SelectList(_db.PhongBans.ToList(), "IdphongBan", "TenPhongBan", IDPhongBan);

            var lh = _db.LopHocs.FirstOrDefault(x => x.Idlh == id);
            ViewBag.TenLH = lh?.TenLh;
            ViewBag.IsLopMo = lh != null && lh.Tgktlh.HasValue && lh.Tgktlh.Value > DateTime.Now;

            var raw = (from h in _db.XnhocTaps.Where(x => x.Lhid == id)
                       join n in _db.NhanViens on h.Nvid equals n.Id
                       join p in _db.PhongBans on h.Pbid equals p.IdphongBan into pj
                       from p in pj.DefaultIfEmpty()
                       join v in _db.Vitris on h.Vtid equals v.IdviTri into vj
                       from v in vj.DefaultIfEmpty()
                       where (search == "" || n.MaNv!.Contains(search) || n.HoTen!.Contains(search))
                          && (IDPhongBan == 0 || n.IdphongBan == IDPhongBan)
                       select new
                       {
                           h.Idht,
                           NVID = n.Id,
                           n.MaNv,
                           n.HoTen,
                           PBID = h.Pbid ?? 0,
                           TenPB = p != null ? p.TenPhongBan : null,
                           VTID = h.Vtid ?? 0,
                           TenVT = v != null ? v.TenViTri : null,
                           LHID = h.Lhid ?? 0,
                       }).ToList();

            var baiThiAll = _db.BaiThis.Where(b => b.Idlh == id).ToList();

            var res = raw.Select(x =>
            {
                var ketqua = baiThiAll.Where(b => b.Idnv == x.NVID).OrderByDescending(b => b.IdbaiThi).ToList();
                int soLan = ketqua.Count;
                string kqText = soLan == 0 ? "Chưa tham gia" : "Đã tham gia";
                string diemText = soLan > 0 ? ketqua[0].DiemSo?.ToString() ?? "" : "";
                int idbaithi = soLan > 0 ? ketqua[0].IdbaiThi : 0;

                return new ConfirmEStudyValidation
                {
                    IDHT = x.Idht,
                    NVID = x.NVID,
                    MaNV = x.MaNv,
                    HoTenHV = x.HoTen,
                    PBID = x.PBID,
                    TenPB = x.TenPB,
                    VTID = x.VTID,
                    TenVT = x.TenVT,
                    LHID = x.LHID,
                    TenLH = lh?.TenLh,
                    KetQuaThi = kqText,
                    SoLanThi = soLan,
                    DiemText = diemText,
                    IDBaiThi = idbaithi
                };
            }).ToList();

            int pageNumber = page ?? 1;
            return View(res.ToPagedList(pageNumber, 50));
        }

        public IActionResult Create(int id)
        {
            ViewBag.LHList = new SelectList(_db.LopHocs.Where(x => x.Idlh == id).ToList(), "Idlh", "TenLh");
            ViewBag.PBList = new SelectList(_db.PhongBans.ToList(), "IdphongBan", "TenPhongBan");
            return PartialView();
        }

        [HttpPost]
        public IActionResult Create(ConfirmEStudyValidation _DO)
        {
            try
            {
                var nv = _db.NhanViens.FirstOrDefault(x => x.Id == _DO.NVID);
                if (nv == null)
                {
                    TempData["msgError"] = "<script>alert('Nhân viên không tồn tại');</script>";
                    return RedirectToAction("Index", "ConfirmEStudy", new { id = _DO.LHID });
                }

                bool exists = _db.XnhocTaps.Any(h => h.Lhid == _DO.LHID && h.Nvid == _DO.NVID);
                if (exists)
                {
                    TempData["msgError"] = "<script>alert('Nhân viên đã tồn tại trong lớp');</script>";
                }
                else
                {
                    _db.XnhocTaps.Add(new XnhocTap
                    {
                        Nvid = _DO.NVID,
                        Lhid = _DO.LHID,
                        Pbid = nv.IdphongBan,
                        Vtid = nv.IdviTri,
                        NgayTg = DateOnly.FromDateTime(DateTime.Now),
                        NgayHt = DateOnly.FromDateTime(DateTime.Now),
                        Xntg = false,
                        Xnht = false
                    });
                    _db.SaveChanges();
                    TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
                }
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới học viên: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ConfirmEStudy", new { id = _DO.LHID });
        }

        public IActionResult Delete(int id)
        {
            var ht = _db.XnhocTaps.FirstOrDefault(x => x.Idht == id);
            int lhid = ht?.Lhid ?? 0;
            try
            {
                if (ht != null) { _db.XnhocTaps.Remove(ht); _db.SaveChanges(); }
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Xóa dữ liệu thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ConfirmEStudy", new { id = lhid });
        }

        public JsonResult GetNhanVien(int id)
        {
            var list = _db.NhanViens
                .Where(x => x.IdphongBan == id && x.IdtinhTrangLv == 1)
                .Select(x => new { x.Id, x.MaNv, x.HoTen })
                .ToList();
            return Json(list);
        }

        public IActionResult ImportExcel(int id)
        {
            ViewBag.IDLH = id;
            return PartialView();
        }

        [HttpPost]
        public IActionResult ImportExcel(int id, IFormFile? FileUpload)
        {
            if (FileUpload == null || FileUpload.Length == 0)
            {
                TempData["msgError"] = "<script>alert('Vui lòng nhập file Import');</script>";
                return RedirectToAction("Index", "ConfirmEStudy", new { id });
            }
            if (!FileUpload.FileName.EndsWith(".xls") && !FileUpload.FileName.EndsWith(".xlsx"))
            {
                TempData["msgError"] = "<script>alert('Vui lòng chọn đúng định dạng file Excel');</script>";
                return RedirectToAction("Index", "ConfirmEStudy", new { id });
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            int dtc = 0, dtrung = 0;
            try
            {
                using var stream = FileUpload.OpenReadStream();
                IExcelDataReader reader = FileUpload.FileName.EndsWith(".xls")
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);
                DataSet result = reader.AsDataSet();
                DataTable dt = result.Tables[0];
                reader.Close();

                for (int i = 5; i < dt.Rows.Count; i++)
                {
                    string maNV = dt.Rows[i][0].ToString()!.Trim();
                    if (string.IsNullOrEmpty(maNV)) continue;

                    var nv = _db.NhanViens.FirstOrDefault(x => x.MaNv!.ToLower() == maNV.ToLower());
                    if (nv == null) continue;

                    bool exists = _db.XnhocTaps.Any(h => h.Lhid == id && h.Nvid == nv.Id);
                    if (exists) { dtrung++; continue; }

                    _db.XnhocTaps.Add(new XnhocTap
                    {
                        Nvid = nv.Id,
                        Lhid = id,
                        Pbid = nv.IdphongBan,
                        Vtid = nv.IdviTri,
                        NgayTg = DateOnly.FromDateTime(DateTime.Now),
                        NgayHt = DateOnly.FromDateTime(DateTime.Now),
                        Xntg = false,
                        Xnht = false
                    });
                    dtc++;
                }
                _db.SaveChanges();

                string msg = dtc > 0 && dtrung > 0
                    ? $"Import được {dtc} dòng dữ liệu, Có {dtrung} dòng trùng Mã NV"
                    : dtc > 0 ? $"Import được {dtc} dòng dữ liệu"
                    : dtrung > 0 ? $"Có {dtrung} dòng trùng Mã NV"
                    : "File import không có dữ liệu";

                TempData["msgSuccess"] = $"<script>alert('{msg}');</script>";
            }
            catch (Exception ex)
            {
                TempData["msgError"] = $"<script>alert('Lỗi import: {ex.Message}');</script>";
            }
            return RedirectToAction("Index", "ConfirmEStudy", new { id });
        }

        public IActionResult ExportToExcel(int id)
        {
            try
            {
                string fileMau = Path.Combine(_env.ContentRootPath, "App_Data", "BM_XDSHT.xlsx");
                string fileTemp = Path.Combine(_env.ContentRootPath, "App_Data", "BM_XDSHT_temp.xlsx");

                using var workbook = new XLWorkbook(fileMau);
                var ws = workbook.Worksheet("DSHT");

                var lopHoc = _db.LopHocs.FirstOrDefault(x => x.Idlh == id);
                var noiDung = lopHoc?.Ndid != null ? _db.NoiDungDts.FirstOrDefault(x => x.Idnd == lopHoc.Ndid) : null;
                var linhVucTen = noiDung?.Lvdtid != null
                    ? _db.LinhVucDts.FirstOrDefault(x => x.Idlvdt == noiDung.Lvdtid)?.TenLvdt : null;

                var raw = (from h in _db.XnhocTaps.Where(x => x.Lhid == id)
                           join n in _db.NhanViens on h.Nvid equals n.Id
                           join v in _db.Vitris on n.IdviTri equals v.IdviTri into vj
                           from v in vj.DefaultIfEmpty()
                           select new ConfirmEStudyValidation
                           {
                               IDHT = h.Idht,
                               NVID = n.Id,
                               MaNV = n.MaNv,
                               HoTenHV = n.HoTen,
                               TenVT = v != null ? v.TenViTri : null,
                               LinhVuc = linhVucTen,
                               TongCongTy = n.TongCongTy,
                               CongTyCapC2 = n.CongTyCapC2,
                               DonViToChucC4 = n.DonViToChucC4,
                               Email = n.Email,
                               DienThoai = n.DienThoai,
                               NgaySinhNV = n.NgaySinh.HasValue
                                   ? n.NgaySinh.Value.ToDateTime(TimeOnly.MinValue)
                                   : (DateTime?)null,
                               LHID = h.Lhid ?? 0
                           }).ToList();

                if (raw.Count == 0)
                {
                    TempData["msg"] = "<script>alert('Không có dữ liệu');</script>";
                    return RedirectToAction("Index", "ConfirmEStudy", new { id });
                }

                var baiThiAll = _db.BaiThis.Where(b => b.Idlh == id).ToList();

                int row = 1, stt = 0;
                foreach (var item in raw)
                {
                    row++; stt++;
                    var ketqua = baiThiAll.Where(b => b.Idnv == item.NVID).OrderByDescending(b => b.IdbaiThi).ToList();
                    int soLan = ketqua.Count;
                    string kqText = soLan == 0 ? "Chưa tham gia" : "Đã tham gia";
                    string diem = soLan > 0 ? ketqua[0].DiemSo?.ToString() ?? "" : "";
                    string tgianthi = soLan > 0 ? (ketqua[0].ThoiGianThi?.ToString() ?? "") + " Giây" : "";
                    string tgbdt = soLan > 0 && ketqua[0].GioBatDau != null
                        ? ketqua[0].GioBatDau?.ToString("dd/MM/yyyy HH:mm:ss") : "";
                    string tgkt = soLan > 0 && ketqua[0].GioKetThuc != null
                        ? ketqua[0].GioKetThuc?.ToString("dd/MM/yyyy HH:mm:ss") : "";

                    void SetCell(string col, object val) {
                        ws.Cell(col + row).Value = XLCellValue.FromObject(val);
                        ws.Cell(col + row).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(col + row).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        ws.Cell(col + row).Style.Alignment.WrapText = true;
                    }

                    SetCell("A", stt);
                    SetCell("B", item.LinhVuc ?? "");
                    SetCell("C", item.TongCongTy ?? "");
                    SetCell("D", item.CongTyCapC2 ?? "");
                    SetCell("E", item.DonViToChucC4 ?? "");
                    SetCell("F", item.MaNV ?? "");
                    SetCell("G", item.HoTenHV ?? "");
                    SetCell("H", item.TenVT ?? "");
                    SetCell("I", item.NgaySinhNV.HasValue ? item.NgaySinhNV.Value.ToString("dd-MM-yyyy") : "");
                    SetCell("J", item.Email ?? "");
                    SetCell("K", "'" + (item.DienThoai ?? ""));
                    SetCell("L", kqText);
                    SetCell("M", tgianthi);
                    SetCell("N", "'" + tgbdt);
                    SetCell("O", "'" + tgkt);
                    ws.Cell("P" + row).Value = diem;
                    ws.Cell("P" + row).Style.NumberFormat.Format = "#,##0.0";
                    SetCell("Q", soLan + " lần");
                }

                workbook.SaveAs(fileTemp);
                byte[] bytes = System.IO.File.ReadAllBytes(fileTemp);
                return File(bytes, "application/vnd.ms-excel",
                    "DSHT_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xlsx");
            }
            catch (Exception ex)
            {
                TempData["msg"] = $"<script>alert('{ex.Message}');</script>";
                return RedirectToAction("Index", "ConfirmEStudy", new { id });
            }
        }

        public IActionResult TestView(int? LHID, int? IDNV, int? IDBaiThi)
        {
            LHID ??= 0; IDNV ??= 0;

            if (IDBaiThi == null || IDBaiThi == 0)
            {
                var bt = _db.BaiThis
                    .Where(x => x.Idlh == LHID && x.Idnv == IDNV)
                    .OrderByDescending(x => x.IdbaiThi)
                    .FirstOrDefault();
                if (bt == null) return RedirectToAction("Index", "ConfirmEStudy", new { id = LHID });
                IDBaiThi = bt.IdbaiThi;
            }

            var baiThi = (from a in _db.BaiThis.Where(x => x.Idnv == IDNV && x.Idlh == LHID && x.IdbaiThi == IDBaiThi)
                          join n in _db.NhanViens on a.Idnv equals n.Id
                          join p in _db.PhongBans on a.IdphongBan equals p.IdphongBan into pj
                          from p in pj.DefaultIfEmpty()
                          join v in _db.Vitris on a.IdviTri equals v.IdviTri into vj
                          from v in vj.DefaultIfEmpty()
                          join nd in _db.NoiDungDts on a.Idnd equals nd.Idnd into ndj
                          from nd in ndj.DefaultIfEmpty()
                          join dt in _db.DeThis on a.IddeThi equals dt.IddeThi into dtj
                          from dt in dtj.DefaultIfEmpty()
                          select new
                          {
                              a.IdbaiThi, a.Idlh, a.IddeThi, a.Idnd, a.Idnv,
                              a.IdphongBan, a.IdviTri,
                              DiemSo = a.DiemSo ?? 0,
                              NgayThi = a.NgayThi.HasValue ? a.NgayThi.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue,
                              TinhTrang = a.TinhTrang ?? false,
                              LanThi = a.LanThi ?? 0,
                              MaNV = n.MaNv,
                              HoTen = n.HoTen,
                              TenPB = p != null ? p.TenPhongBan : null,
                              TenVT = v != null ? v.TenViTri : null,
                              NoiDungDT = nd != null ? nd.NoiDung : null,
                              ThoiGianLamBai = dt != null ? dt.ThoiGianLamBai ?? 0 : 0,
                              TenDeThi = dt != null ? dt.TenDe : null
                          }).FirstOrDefault();

            if (baiThi == null) return RedirectToAction("Index", "ConfirmEStudy", new { id = LHID });

            var cauHois = (from b in _db.CtbaiThis.Where(x => x.IdbaiThi == IDBaiThi)
                           join c in _db.CauHois on b.IdcauHoi equals c.Idch
                           join d in _db.DanhSachDa on c.Iddađung equals d.Iddsđa
                           join e in _db.DanhSachDa on b.IddapAnNv equals e.Iddsđa into gj
                           from sub in gj.DefaultIfEmpty()
                           select new ManageQuestionValidation
                           {
                               IDCH = c.Idch,
                               NoiDungCH = c.NoiDungCh,
                               DapAnA = c.DapAnA,
                               DapAnB = c.DapAnB,
                               DapAnC = c.DapAnC,
                               DapAnD = c.DapAnD,
                               DapAnDung = d.TenĐa,
                               IDDADung = c.Iddađung ?? 0,
                               Diem = b.Diem ?? 0,
                               DapAnHV = sub != null ? sub.TenĐa : null,
                               IDDAHocVien = b.IddapAnNv
                           }).ToList();

            var vm = new TestViewModel
            {
                IDBaiThi = baiThi.IdbaiThi,
                IDLH = baiThi.Idlh ?? 0,
                IDDeThi = baiThi.IddeThi ?? 0,
                IDND = baiThi.Idnd ?? 0,
                IDNV = baiThi.Idnv ?? 0,
                DiemSo = baiThi.DiemSo,
                NgayThi = baiThi.NgayThi,
                TinhTrang = baiThi.TinhTrang,
                LanThi = baiThi.LanThi,
                MaNV = baiThi.MaNV,
                HoTen = baiThi.HoTen,
                TenPhongBan = baiThi.TenPB,
                TenViTri = baiThi.TenVT,
                NoiDungDT = baiThi.NoiDungDT,
                ThoiGianLamBai = baiThi.ThoiGianLamBai,
                TenDeThi = baiThi.TenDeThi,
                CauHois = cauHois
            };

            return View(vm);
        }
    }
}
