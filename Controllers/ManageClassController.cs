using ClosedXML.Excel;
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

            var pbList = _db.PhongBans.Where(x => x.TenPhongBan != null).OrderBy(x => x.TenPhongBan).ToList();
            ViewBag.IDPB         = new SelectList(pbList, "IdphongBan", "TenPhongBan");
            ViewBag.PhongBanList = pbList.Select(x => new { x.IdphongBan, x.TenPhongBan }).ToList();
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

        [HttpGet]
        public async Task<IActionResult> ExportOnLuyen([FromQuery] int[]? idPBs)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.EX)) return Forbid();

            var onLuyenLhIds = await _db.LopHocs
                .Where(l => l.IsCoCtdt == 1)
                .Select(l => l.Idlh)
                .ToListAsync();

            var enrolledRaw = await (from x in _db.XnhocTaps
                                     join n in _db.NhanViens on x.Nvid equals n.Id
                                     join pb in _db.PhongBans on n.IdphongBan equals pb.IdphongBan into pbj
                                     from pb in pbj.DefaultIfEmpty()
                                     join v in _db.Vitris on n.IdviTri equals v.IdviTri into vj
                                     from v in vj.DefaultIfEmpty()
                                     where onLuyenLhIds.Contains(x.Lhid ?? 0)
                                        && (idPBs == null || idPBs.Length == 0 || idPBs.Contains(n.IdphongBan ?? 0))
                                     select new
                                     {
                                         IDNV  = n.Id,
                                         MaNv  = n.MaNv,
                                         HoTen = n.HoTen,
                                         TenPB = pb != null ? pb.TenPhongBan : "Chưa phân công",
                                         TenVT = v != null ? v.TenViTri : null
                                     }).ToListAsync();

            var persons = enrolledRaw
                .GroupBy(x => x.IDNV)
                .Select(g => g.First())
                .OrderBy(x => x.TenPB).ThenBy(x => x.HoTen)
                .ToList();

            var nvIds = persons.Select(p => p.IDNV).ToList();
            var baiThiRaw = await _db.BaiThis
                .Where(b => b.Idnv != null && onLuyenLhIds.Contains(b.Idlh ?? 0) && nvIds.Contains(b.Idnv!.Value))
                .Select(b => new
                {
                    Idnv        = b.Idnv!.Value,
                    Idlh        = b.Idlh ?? 0,
                    DiemSo      = b.DiemSo ?? 0,
                    ThoiGianThi = b.ThoiGianThi,
                    GioBatDau   = b.GioBatDau,
                    GioKetThuc  = b.GioKetThuc
                })
                .ToListAsync();

            var baiThiStats = baiThiRaw
                .GroupBy(b => b.Idnv)
                .ToDictionary(g => g.Key, g =>
                {
                    var items = g.ToList();
                    var times = items
                        .Select(x => x.GioBatDau.HasValue && x.GioKetThuc.HasValue
                            ? (int?)(int)(x.GioKetThuc.Value - x.GioBatDau.Value).TotalSeconds
                            : x.ThoiGianThi)
                        .Where(t => t.HasValue && t.Value > 0)
                        .Select(t => t!.Value)
                        .ToList();
                    return new
                    {
                        SoLan          = items.Count,
                        DiemCao        = items.Max(x => x.DiemSo),
                        TongDiem       = items.Sum(x => x.DiemSo),
                        SoDeHoanThanh  = items.Select(x => x.Idlh).Distinct().Count(),
                        TgNganNhat     = times.Count > 0 ? (int?)times.Min() : null
                    };
                });

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ôn Luyện");

            ws.Cell(1, 1).Value = $"DANH SÁCH ÔN LUYỆN - Xuất ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(1, 1, 1, 11).Merge();
            ws.Cell(1, 1).Style.Font.Bold     = true;
            ws.Cell(1, 1).Style.Font.FontSize  = 13;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string[] hdOL = {
                "STT", "Phòng ban / Bộ phận", "Mã NV", "Họ và tên", "Chức vụ",
                "Tổng số lần ôn luyện", "Số đề hoàn thành",
                "Tổng điểm ôn luyện", "Điểm cao nhất",
                "TG làm bài ngắn nhất", "Trạng thái"
            };
            for (int c = 0; c < hdOL.Length; c++)
            {
                var cell = ws.Cell(2, c + 1);
                cell.Value = hdOL[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3949AB");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.WrapText = true;
            }

            int row = 3, stt = 1;
            foreach (var p in persons)
            {
                baiThiStats.TryGetValue(p.IDNV, out var stat);
                int soLan = stat?.SoLan ?? 0;
                string tgText = stat?.TgNganNhat.HasValue == true
                    ? $"{stat.TgNganNhat.Value / 60:D2}:{stat.TgNganNhat.Value % 60:D2}"
                    : "--";
                ws.Cell(row, 1).Value  = stt++;
                ws.Cell(row, 2).Value  = p.TenPB ?? "";
                ws.Cell(row, 3).Value  = p.MaNv   ?? "";
                ws.Cell(row, 4).Value  = p.HoTen  ?? "";
                ws.Cell(row, 5).Value  = p.TenVT  ?? "";
                ws.Cell(row, 6).Value  = soLan;
                ws.Cell(row, 7).Value  = stat?.SoDeHoanThanh ?? 0;
                ws.Cell(row, 8).Value  = stat?.TongDiem ?? 0;
                ws.Cell(row, 9).Value  = stat?.DiemCao  ?? 0;
                ws.Cell(row, 10).Value = tgText;
                ws.Cell(row, 11).Value = soLan > 0 ? "Đã tham gia" : "Chưa tham gia";
                if (soLan == 0) ws.Cell(row, 11).Style.Font.FontColor = XLColor.Red;
                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 6;

            using var ms1 = new MemoryStream();
            wb.SaveAs(ms1);
            return File(ms1.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"OnLuyen_{DateTime.Now:ddMMyyyyHHmmss}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportThiChinhThuc([FromQuery] int[]? idPBs, int? idlh)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.EX)) return Forbid();

            // Xuất theo từng lớp thi chính thức (có thể có lớp thi lại); mặc định lớp mới nhất
            var lhId = idlh ?? await XepHangThiChinhThuc.GetLopThiMoiNhatAsync(_db);
            var tenLop = await _db.LopHocs.Where(l => l.Idlh == lhId).Select(l => l.TenLh).FirstOrDefaultAsync();

            // Base list: XnhocTap enrolled in official exam class (same as ConfirmEStudy pattern)
            var enrolledRaw = await (from x in _db.XnhocTaps
                                     join n in _db.NhanViens on x.Nvid equals n.Id
                                     join pb in _db.PhongBans on n.IdphongBan equals pb.IdphongBan into pbj
                                     from pb in pbj.DefaultIfEmpty()
                                     join v in _db.Vitris on n.IdviTri equals v.IdviTri into vj
                                     from v in vj.DefaultIfEmpty()
                                     where x.Lhid == lhId
                                        && (idPBs == null || idPBs.Length == 0 || idPBs.Contains(n.IdphongBan ?? 0))
                                     select new
                                     {
                                         IDNV          = n.Id,
                                         MaNv          = n.MaNv,
                                         HoTen         = n.HoTen,
                                         TenPB         = pb != null ? pb.TenPhongBan : "Chưa phân công",
                                         TenVT         = v != null ? v.TenViTri : null,
                                         TongCongTy    = n.TongCongTy,
                                         CongTyCapC2   = n.CongTyCapC2,
                                         DonViToChucC4 = n.DonViToChucC4,
                                         NgaySinh      = n.NgaySinh,
                                         Email         = n.Email,
                                         DienThoai     = n.DienThoai
                                     }).ToListAsync();

            var persons = enrolledRaw
                .GroupBy(x => x.IDNV)
                .Select(g => g.First())
                .OrderBy(x => x.TenPB).ThenBy(x => x.HoTen)
                .ToList();

            if (persons.Count == 0)
            {
                TempData["msg"] = "<script>alert('Không có dữ liệu');</script>";
                return RedirectToAction("Index", "ManageClass");
            }

            var nvIds = persons.Select(p => p.IDNV).ToList();

            var baiThiAll = await _db.BaiThis
                .Where(b => b.Idnv != null && b.Idlh == lhId && nvIds.Contains(b.Idnv!.Value))
                .Select(b => new
                {
                    IdbaiThi    = b.IdbaiThi,
                    Idnv        = b.Idnv!.Value,
                    DiemSo      = b.DiemSo,
                    TinhTrang   = b.TinhTrang ?? false,
                    ThoiGianThi = b.ThoiGianThi,
                    GioBatDau   = b.GioBatDau,
                    GioKetThuc  = b.GioKetThuc
                })
                .ToListAsync();

            // Mỗi người: bài tính xếp hạng = bài nộp đầu tiên trong lớp (giống bảng xếp hạng)
            var baiThiDict = baiThiAll
                .GroupBy(b => b.Idnv)
                .ToDictionary(g => g.Key, g =>
                {
                    var items = g.ToList();
                    var bai   = items.OrderBy(x => x.IdbaiThi).First();
                    return new
                    {
                        SoLan      = items.Count,
                        Diem       = bai.DiemSo,
                        CoLanDat   = items.Any(x => x.TinhTrang),
                        Giay       = XepHangThiChinhThuc.TinhThoiGianSec(bai.ThoiGianThi, bai.GioBatDau, bai.GioKetThuc),
                        GioBatDau  = bai.GioBatDau,
                        GioKetThuc = bai.GioKetThuc
                    };
                });

            // Xếp hạng theo Điều 4 thể lệ — tính trên toàn bộ lớp thi, không phụ thuộc bộ lọc đơn vị
            var xepHang = (await XepHangThiChinhThuc.TinhAsync(_db, lhId)).ToDictionary(x => x.NVID);

            // ── Ôn luyện: tổng điểm + tổng thời gian ────────────────────────────
            var onLuyenLhIds = await _db.LopHocs
                .Where(l => l.IsCoCtdt == 1)
                .Select(l => l.Idlh)
                .ToListAsync();

            var olPerNv = new Dictionary<int, (double DiemOL, int GiayOL)>();
            if (onLuyenLhIds.Count > 0)
            {
                var olRaw = await _db.BaiThis
                    .Where(b => b.Idnv != null && b.DiemSo != null
                             && onLuyenLhIds.Contains(b.Idlh ?? 0)
                             && nvIds.Contains(b.Idnv!.Value))
                    .Select(b => new
                    {
                        Idnv        = b.Idnv!.Value,
                        DiemSo      = b.DiemSo ?? 0,
                        ThoiGianThi = b.ThoiGianThi,
                        GioBatDau   = b.GioBatDau,
                        GioKetThuc  = b.GioKetThuc
                    })
                    .ToListAsync();

                olPerNv = olRaw
                    .GroupBy(b => b.Idnv)
                    .ToDictionary(g => g.Key, g =>
                    {
                        double tongDiem = g.Sum(x => x.DiemSo);
                        int tongGiay = g.Sum(x => XepHangThiChinhThuc.TinhThoiGianSec(x.ThoiGianThi, x.GioBatDau, x.GioKetThuc));
                        return (DiemOL: tongDiem, GiayOL: tongGiay);
                    });
            }

            string fileMau  = Path.Combine(_env.ContentRootPath, "App_Data", "BM_XDSHT.xlsx");
            string fileTemp = Path.Combine(_env.ContentRootPath, "App_Data", "BM_XDSHT_TCT_temp.xlsx");

            using var workbook = new XLWorkbook(fileMau);
            var ws = workbook.Worksheet("DSHT");

            void SetHeader(string col, string title)
            {
                ws.Cell(col + "1").Value = title;
                ws.Cell(col + "1").Style.Font.Bold = true;
                ws.Cell(col + "1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(col + "1").Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                ws.Cell(col + "1").Style.Alignment.WrapText   = true;
            }

            SetHeader("R", "Xếp hạng (Điều 4)");
            SetHeader("S", "Điểm ôn luyện");
            SetHeader("T", "Tổng thời gian ôn luyện");
            SetHeader("U", "Điểm bài thi (tiêu chí 1)");
            SetHeader("V", "Thời gian làm bài (tiêu chí 2)");
            SetHeader("W", "Thời điểm hoàn thành ôn luyện (tiêu chí 3)");
            SetHeader("X", "Ghi chú xếp hạng");
            SetHeader("Y", "Lớp thi");

            int row = 1, stt = 0;
            foreach (var p in persons)
            {
                row++; stt++;
                baiThiDict.TryGetValue(p.IDNV, out var bt);
                xepHang.TryGetValue(p.IDNV, out var xh);

                bool daThi  = bt != null;
                bool datDat = bt?.CoLanDat == true;
                string kqText   = !daThi ? "Chưa thi" : datDat ? "Đã hoàn thành" : "Chưa đạt";
                string tgianthi = daThi ? bt!.Giay + " Giây" : "";
                string tgbdt    = bt?.GioBatDau?.ToString("dd/MM/yyyy HH:mm:ss")  ?? "";
                string tgkt     = bt?.GioKetThuc?.ToString("dd/MM/yyyy HH:mm:ss") ?? "";

                void SetCell(string col, object val)
                {
                    ws.Cell(col + row).Value = XLCellValue.FromObject(val);
                    ws.Cell(col + row).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(col + row).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                    ws.Cell(col + row).Style.Alignment.WrapText   = true;
                }

                SetCell("A", stt);
                SetCell("B", p.TenPB         ?? "");
                SetCell("C", p.TongCongTy    ?? "");
                SetCell("D", p.CongTyCapC2   ?? "");
                SetCell("E", p.DonViToChucC4 ?? "");
                SetCell("F", p.MaNv          ?? "");
                SetCell("G", p.HoTen         ?? "");
                SetCell("H", p.TenVT         ?? "");
                SetCell("I", p.NgaySinh.HasValue ? p.NgaySinh.Value.ToDateTime(TimeOnly.MinValue).ToString("dd-MM-yyyy") : "");
                SetCell("J", p.Email         ?? "");
                SetCell("K", "'" + (p.DienThoai ?? ""));
                SetCell("L", kqText);
                SetCell("M", tgianthi);
                SetCell("N", "'" + tgbdt);
                SetCell("O", "'" + tgkt);
                if (daThi && bt!.Diem.HasValue)
                {
                    ws.Cell("P" + row).Value = bt.Diem.Value;
                    ws.Cell("P" + row).Style.NumberFormat.Format = "#,##0.0";
                }
                SetCell("Q", daThi ? bt!.SoLan + " lần" : "0 lần");

                // Các cột xếp hạng theo Điều 4
                if (xh != null && xh.DaThi)
                {
                    SetCell("R", xh.Rank);
                    SetCell("U", xh.SoCauDung + "/" + (xh.TongSoCau > 0 ? xh.TongSoCau : 30));
                    int h2 = xh.ThoiGianSec / 3600, m2 = (xh.ThoiGianSec % 3600) / 60, s2 = xh.ThoiGianSec % 60;
                    SetCell("V", h2 > 0 ? $"{h2}:{m2:D2}:{s2:D2}" : $"{m2:D2}:{s2:D2}");
                    if (xh.DongHang) SetCell("X", "Đồng hạng – BTC xét thi trực tiếp");
                }
                if (xh?.HoanThanhOnLuyen != null)
                    SetCell("W", "'" + xh.HoanThanhOnLuyen.Value.ToString("dd/MM/yyyy HH:mm:ss"));
                else
                    SetCell("W", "Chưa hoàn thành");
                SetCell("Y", tenLop ?? "");

                olPerNv.TryGetValue(p.IDNV, out var ol);
                if (ol.DiemOL > 0)
                {
                    ws.Cell("S" + row).Value = ol.DiemOL;
                    ws.Cell("S" + row).Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell("S" + row).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell("S" + row).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                }
                if (ol.GiayOL > 0)
                {
                    int h = ol.GiayOL / 3600, m = (ol.GiayOL % 3600) / 60, s = ol.GiayOL % 60;
                    string tgOL = h > 0 ? $"{h}h {m:D2}m {s:D2}s" : $"{m}m {s:D2}s";
                    ws.Cell("T" + row).Value = tgOL;
                    ws.Cell("T" + row).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell("T" + row).Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                }

                // Màu kết quả
                ws.Cell("L" + row).Style.Font.FontColor = !daThi
                    ? XLColor.Gray
                    : datDat ? XLColor.FromHtml("#0A8F3C") : XLColor.Red;
                if (xh?.DongHang == true)
                    ws.Cell("X" + row).Style.Font.FontColor = XLColor.Red;
            }

            ws.Columns("R", "Y").AdjustToContents(1, row, 12, 40);

            workbook.SaveAs(fileTemp);
            byte[] bytes = System.IO.File.ReadAllBytes(fileTemp);
            return File(bytes, "application/vnd.ms-excel",
                $"ThiChinhThuc_{DateTime.Now:ddMMyyyyHHmmss}.xlsx");
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

        // ── Pool đề thi (random đề chính thức) ──────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DeThiPool(int idlh)
        {
            var lh = await _db.LopHocs.AsNoTracking().FirstOrDefaultAsync(x => x.Idlh == idlh);
            if (lh == null) return NotFound();

            var pool = await (
                from p in _db.LopHocDeThiPools.Where(x => x.Idlh == idlh)
                join d in _db.DeThis on p.IddeThi equals d.IddeThi
                select new { p.Id, p.IddeThi, d.TenDe, d.MaDe }
            ).ToListAsync();

            // Danh sách đề theo cùng NDID để chọn thêm
            var available = await _db.DeThis.AsNoTracking()
                .Where(x => x.Idnd == lh.Ndid)
                .Select(x => new ManageTestExamValidation { IDDeThi = x.IddeThi, TenDe = x.TenDe, MaDe = x.MaDe })
                .ToListAsync();

            ViewBag.IDLH        = idlh;
            ViewBag.TenLH       = lh.TenLh;
            ViewBag.Pool        = pool;
            ViewBag.Available   = available;
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> ThemDeVaoPool(int idlh, int iddeThi)
        {
            try
            {
                var exists = await _db.LopHocDeThiPools
                    .AnyAsync(x => x.Idlh == idlh && x.IddeThi == iddeThi);
                if (!exists)
                {
                    _db.LopHocDeThiPools.Add(new HeThongThiDQ.Data.Models.LopHocDeThiPool
                    {
                        Idlh = idlh, IddeThi = iddeThi
                    });
                    await _db.SaveChangesAsync();

                    // Xóa cache lophoc để load lại pool mới
                    await InvalidateLopHocCache(idlh);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ThemNhieuDeVaoPool([FromBody] ThemNhieuDeRequest req)
        {
            if (req.IdlhList == null || req.IddeThi == null || req.IddeThi.Count == 0)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            try
            {
                var existIds = (await _db.LopHocDeThiPools
                    .Where(x => x.Idlh == req.Idlh)
                    .Select(x => x.IddeThi)
                    .ToListAsync()).ToHashSet();

                foreach (var idde in req.IddeThi)
                {
                    if (!existIds.Contains(idde))
                        _db.LopHocDeThiPools.Add(new HeThongThiDQ.Data.Models.LopHocDeThiPool
                            { Idlh = req.Idlh, IddeThi = idde });
                }
                await _db.SaveChangesAsync();
                await InvalidateLopHocCache(req.Idlh);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class ThemNhieuDeRequest
        {
            public int Idlh { get; set; }
            public List<int>? IdlhList { get; set; } // unused, kept for compat
            public List<int>? IddeThi  { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> XoaDeKhoiPool(int id, int idlh)
        {
            try
            {
                var item = await _db.LopHocDeThiPools.FindAsync(id);
                if (item != null)
                {
                    _db.LopHocDeThiPools.Remove(item);
                    await _db.SaveChangesAsync();
                    await InvalidateLopHocCache(idlh);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ── Sync thí sinh từ danh sách nhân sự đang làm việc ───────────────────────

        [HttpGet]
        public async Task<IActionResult> SyncThiSinh()
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.ADD))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("Index");
            }

            var lopHocList = await _db.LopHocs
                .OrderByDescending(x => x.Tgbdlh)
                .Select(x => new { x.Idlh, x.MaLh, x.TenLh })
                .ToListAsync();

            ViewBag.LopHocList = lopHocList;
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> SyncThiSinh(int[] lhIds)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.ADD))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("Index");
            }

            if (lhIds == null || lhIds.Length == 0)
            {
                TempData["msgError"] = "<script>alert('Vui lòng chọn ít nhất một lớp học');</script>";
                return RedirectToAction("Index");
            }

            try
            {
                var activeNVs = await _db.NhanViens
                    .Where(x => x.IdtinhTrangLv == 1 && x.IsGv != true)
                    .Select(x => new { x.Id, x.IdphongBan, x.IdviTri })
                    .ToListAsync();

                var activeNvIdSet = activeNVs.Select(x => x.Id).ToHashSet();

                int totalAdded = 0, totalRemoved = 0;

                foreach (var lhId in lhIds)
                {
                    var existing = await _db.XnhocTaps
                        .Where(x => x.Lhid == lhId)
                        .ToListAsync();

                    var existingNvIds = existing.Select(x => x.Nvid).ToHashSet();

                    // Thêm nhân viên active chưa có trong lớp
                    foreach (var nv in activeNVs)
                    {
                        if (existingNvIds.Contains(nv.Id)) continue;
                        _db.XnhocTaps.Add(new XnhocTap
                        {
                            Nvid   = nv.Id,
                            Lhid   = lhId,
                            NgayTg = DateOnly.FromDateTime(new DateTime(1, 1, 1)),
                            NgayHt = DateOnly.FromDateTime(new DateTime(1, 1, 1)),
                            Xntg   = false,
                            Xnht   = false,
                            Pbid   = nv.IdphongBan,
                            Vtid   = nv.IdviTri
                        });
                        totalAdded++;
                    }

                    // Xóa thí sinh đã nghỉ việc khỏi lớp
                    var toRemove = existing.Where(x => !activeNvIdSet.Contains(x.Nvid ?? 0)).ToList();
                    if (toRemove.Count > 0)
                    {
                        _db.XnhocTaps.RemoveRange(toRemove);
                        totalRemoved += toRemove.Count;
                    }
                }

                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = $"<script>alert('Sync thí sinh thành công: thêm {totalAdded}, loại bỏ nghỉ việc {totalRemoved} học viên ({lhIds.Length} lớp)');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi sync thí sinh: {e.Message}');</script>";
            }

            return RedirectToAction("Index");
        }

        // ── Sinh lớp học hàng loạt theo danh sách đề thi ôn luyện ──────────────────

        [HttpGet]
        public async Task<IActionResult> SinhLopHangLoat()
        {
            ViewBag.NDList = new SelectList(
                await _db.NoiDungDts.AsNoTracking().ToListAsync(), "Idnd", "NoiDung");
            return PartialView();
        }

        [HttpGet]
        public async Task<IActionResult> GetDeThiOnLuyen(int idnd)
        {
            var list = await _db.DeThis.AsNoTracking()
                .Where(x => x.Idnd == idnd)
                .OrderBy(x => x.MaDe)
                .Select(x => new { x.IddeThi, x.MaDe, x.TenDe, x.TongSoCau })
                .ToListAsync();
            return Json(list);
        }

        [HttpPost]
        public async Task<IActionResult> SinhLopHangLoat(
            List<int>? IdDeThiList, int QuyDT, int NamDT,
            DateTime TGBDLH, DateTime TGKTLH, string? MaLHPrefix,
            IFormFile? FileUpload)
        {
            if (IdDeThiList == null || IdDeThiList.Count == 0)
            {
                TempData["msgError"] = "<script>alert('Chưa chọn đề thi nào');</script>";
                return RedirectToAction("Index", "ManageClass");
            }

            try
            {
                var deThiList = await _db.DeThis.AsNoTracking()
                    .Where(x => IdDeThiList.Contains(x.IddeThi))
                    .OrderBy(x => x.MaDe)
                    .ToListAsync();

                var prefix = string.IsNullOrWhiteSpace(MaLHPrefix) ? "OL" : MaLHPrefix.Trim().ToUpper();
                var existingMaLH = (await _db.LopHocs.Select(x => x.MaLh).ToListAsync()).ToHashSet();
                int counter = 1;
                int created = 0;

                foreach (var dt in deThiList)
                {
                    string maLH;
                    do { maLH = $"{prefix}{counter++:D4}"; } while (existingMaLH.Contains(maLH));
                    existingMaLH.Add(maLH);

                    var lopHoc = new LopHoc
                    {
                        MaLh     = maLH,
                        TenLh    = dt.TenDe,
                        Ndid     = dt.Idnd,
                        QuyDt    = QuyDT,
                        NamDt    = NamDT,
                        Tgbdlh   = TGBDLH,
                        Tgktlh   = TGKTLH,
                        Gvid     = _auth.ID,
                        IddeThi  = dt.IddeThi,
                        IsCoCtdt = 1
                    };
                    _db.LopHocs.Add(lopHoc);
                    await _db.SaveChangesAsync();

                    if (FileUpload != null && FileUpload.Length > 0)
                        ImportHocVien(FileUpload, maLH, lopHoc.Idlh);

                    created++;
                }

                TempData["msgSuccess"] = $"<script>alert('Đã tạo {created} lớp học ôn luyện thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "ManageClass");
        }

        private async Task InvalidateLopHocCache(int idlh)
        {
            try
            {
                var cache = HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                await cache.RemoveAsync($"lophoc:{idlh}");
            }
            catch { }
        }
    }
}
