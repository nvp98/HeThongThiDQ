using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExcelDataReader;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using X.PagedList.Extensions;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private readonly IConfiguration _config;
        private const string ControllerName = "Account";

        public AccountController(ELEARNINGEntities db, MyAuthentication auth, HomeController home, IConfiguration config)
        {
            _db = db;
            _auth = auth;
            _home = home;
            _config = config;
        }

        private List<EmployeeValidation> GetEmployeeList(string search)
        {
            search = search ?? "";
            return (from n in _db.NhanViens
                    join pb in _db.PhongBans on n.IdphongBan equals pb.IdphongBan into pbj
                    from pb in pbj.DefaultIfEmpty()
                    join q in _db.Quyens on n.Idquyen equals q.Idquyen into qj
                    from q in qj.DefaultIfEmpty()
                    join v in _db.Vitris on n.IdviTri equals v.IdviTri into vj
                    from v in vj.DefaultIfEmpty()
                    where search == "" || n.MaNv!.Contains(search) || n.HoTen!.Contains(search)
                    select new EmployeeValidation
                    {
                        ID = n.Id,
                        HoTen = n.HoTen,
                        MaNV = n.MaNv,
                        PhongBan = pb != null ? pb.TenPhongBan : null,
                        IDPhongBan = n.IdphongBan ?? 0,
                        TenQuyen = q != null ? q.TenQuyen : null,
                        ChucVu = v != null ? v.TenViTri : null,
                        Email = n.Email,
                        IsGV = n.IsGv ?? false,
                        ChuDeThi = n.ChuDeThi,
                        TongCongTy = n.TongCongTy,
                        DonViC2 = n.CongTyCapC2,
                        DoViC4 = n.DonViToChucC4,
                        SoDienThoai = n.DienThoai,
                        NgaySinh = n.NgaySinh.HasValue
                            ? n.NgaySinh.Value.ToDateTime(TimeOnly.MinValue)
                            : (DateTime?)null,
                        NgaySinhStr = n.NgaySinh.HasValue
                            ? n.NgaySinh.Value.ToString("dd/MM/yyyy")
                            : null
                    }).ToList();
        }

        public IActionResult Index(int? page, string? search)
        {
            var listQuyen = _home.GetPermisionCN(_auth.IDQuyen, ControllerName).Result;
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            search ??= "";
            ViewBag.search = search;

            var res = GetEmployeeList(search).OrderBy(x => x.ID);

            int pageNumber = page ?? 1;
            return View(res.ToPagedList(pageNumber, 50));
        }

        public async Task<IActionResult> GetData([FromQuery] JqueryDatatableParam param, int? IDPB)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.V))
                return Json("Unauthorized");

            var employees = GetEmployeeList("");

            if (!string.IsNullOrEmpty(param.sSearch))
            {
                employees = employees.Where(x =>
                    (x.HoTen?.ToLower().Contains(param.sSearch.ToLower()) ?? false) ||
                    (x.MaNV?.ToLower().Contains(param.sSearch.ToLower()) ?? false)).ToList();
            }

            var sortColumnIndex = Convert.ToInt32(Request.Query["iSortCol_0"]);
            var sortDirection = Request.Query["sSortDir_0"].ToString();

            employees = sortColumnIndex switch
            {
                2 => sortDirection == "asc" ? employees.OrderBy(c => c.MaNV).ToList() : employees.OrderByDescending(c => c.MaNV).ToList(),
                3 => sortDirection == "asc" ? employees.OrderBy(c => c.HoTen).ToList() : employees.OrderByDescending(c => c.HoTen).ToList(),
                4 => sortDirection == "asc" ? employees.OrderBy(c => c.IDPhongBan).ToList() : employees.OrderByDescending(c => c.IDPhongBan).ToList(),
                5 => sortDirection == "asc" ? employees.OrderBy(c => c.TenQuyen).ToList() : employees.OrderByDescending(c => c.TenQuyen).ToList(),
                _ => sortDirection == "asc" ? employees.OrderBy(c => c.MaNV).ToList() : employees.OrderByDescending(c => c.MaNV).ToList()
            };

            var displayResult = employees.Skip(param.iDisplayStart).Take(param.iDisplayLength).ToList();
            return Json(new
            {
                param.sEcho,
                iTotalRecords = employees.Count,
                iTotalDisplayRecords = employees.Count,
                aaData = displayResult
            });
        }

        public IActionResult Create()
        {
            return PartialView();
        }

        [HttpPost]
        public IActionResult Create(ManageClassValidation _DO, string? SelectedMonth)
        {
            var file = Request.Form.Files["FileUpload"];
            if (file == null || file.Length == 0)
            {
                TempData["msgSuccess"] = "<script>alert('Vui lòng nhập file Import');</script>";
                return RedirectToAction("Index", "Account");
            }

            if (!file.FileName.EndsWith(".xls") && !file.FileName.EndsWith(".xlsx"))
            {
                TempData["msg"] = "<script>alert('Vui lòng chọn đúng định dạng file Excel');</script>";
                return RedirectToAction("Index", "Account");
            }

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            try
            {
                using var stream = file.OpenReadStream();
                IExcelDataReader reader = file.FileName.EndsWith(".xls")
                    ? ExcelReaderFactory.CreateBinaryReader(stream)
                    : ExcelReaderFactory.CreateOpenXmlReader(stream);

                DataSet result = reader.AsDataSet();
                DataTable dt = result.Tables[0];
                reader.Close();

                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    string chude = dt.Rows[i][1].ToString()!.Trim();
                    string tongcongty = dt.Rows[i][2].ToString()!.Trim();
                    string congtyC2 = dt.Rows[i][3].ToString()!.Trim();
                    string congtyC4 = dt.Rows[i][4].ToString()!.Trim();
                    string maNV = dt.Rows[i][5].ToString()!.Trim();
                    string hoTen = dt.Rows[i][6].ToString()!.Trim();
                    string viTri = dt.Rows[i][7].ToString()!.Trim();
                    string ngaySinhRaw = dt.Rows[i][8].ToString()!.Trim();
                    string email = dt.Rows[i][9].ToString()!.Trim();
                    string soDienThoai = dt.Rows[i][10].ToString()!.Trim();
                    string matKhau = dt.Rows[i][11].ToString()!.Trim();

                    string mkDecode = Encryptor.MD5Hash(matKhau);
                    DateOnly? ngaySinh = ParseNgaySinh(ngaySinhRaw);

                    var idViTri = GetOrCreateViTri(viTri);
                    var idPhongBan = GetOrCreatePhongBan(congtyC4);

                    var checkNV = _db.NhanViens.FirstOrDefault(x => x.MaNv == maNV);
                    if (checkNV == null)
                    {
                        var nv = new NhanVien
                        {
                            MaNv = maNV,
                            HoTen = hoTen,
                            MatKhau = mkDecode,
                            Email = email,
                            NgaySinh = ngaySinh,
                            DienThoai = soDienThoai,
                            ChuDeThi = chude,
                            TongCongTy = tongcongty,
                            CongTyCapC2 = congtyC2,
                            DonViToChucC4 = congtyC4,
                            IdphongBan = idPhongBan,
                            IdviTri = idViTri,
                            Idquyen = 4,
                            IdtinhTrangLv = 1,
                            IsGv = false
                        };
                        _db.NhanViens.Add(nv);
                    }
                    else
                    {
                        checkNV.HoTen = hoTen;
                        checkNV.MatKhau = mkDecode;
                        checkNV.Email = email;
                        checkNV.NgaySinh = ngaySinh;
                        checkNV.DienThoai = soDienThoai;
                        checkNV.ChuDeThi = chude;
                        checkNV.TongCongTy = tongcongty;
                        checkNV.CongTyCapC2 = congtyC2;
                        checkNV.DonViToChucC4 = congtyC4;
                        checkNV.IdphongBan = idPhongBan;
                        checkNV.IdviTri = idViTri;
                    }
                    _db.SaveChanges();
                }

                TempData["msgSuccess"] = "<script>alert('Import dữ liệu thành công');</script>";
            }
            catch (Exception ex)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi Upload: {ex.Message}');</script>";
            }

            return RedirectToAction("Index", "Account");
        }

        public IActionResult Resetpass(int id)
        {
            var nv = _db.NhanViens.FirstOrDefault(x => x.Id == id);
            if (nv == null) return NotFound();

            var DO = new LoginValidation { ID = nv.Id, HoTen = nv.HoTen };
            return PartialView(DO);
        }

        [HttpPost]
        public IActionResult Resetpass(LoginValidation _DO)
        {
            try
            {
                var nv = _db.NhanViens.FirstOrDefault(x => x.Id == _DO.ID);
                if (nv != null)
                {
                    nv.MatKhau = Encryptor.MD5Hash(_DO.MatKhau!);
                    _db.SaveChanges();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "Account");
        }

        public async Task<IActionResult> ResetListPass()
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.R_PASS))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("", "Home");
            }
            return PartialView();
        }

        [HttpPost]
        public IActionResult ResetListPass(LoginValidation _DO)
        {
            try
            {
                if (!string.IsNullOrEmpty(_DO.MaNV))
                {
                    string tx = Regex.Replace(_DO.MaNV, @"[^0-9a-zA-Z]+", " ");
                    string[] nvs = tx.Split(' ');
                    foreach (var item in nvs)
                    {
                        var nv = _db.NhanViens.FirstOrDefault(x => x.MaNv == item);
                        if (nv != null)
                        {
                            nv.MatKhau = Encryptor.MD5Hash(_DO.MatKhau!);
                        }
                    }
                    _db.SaveChanges();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "Account");
        }

        public IActionResult Edit(int id)
        {
            var nv = _db.NhanViens.FirstOrDefault(x => x.Id == id);
            if (nv == null) return NotFound();

            var pq = _db.Quyens.ToList();
            ViewBag.PList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(pq, "Idquyen", "TenQuyen");

            var DO = new LoginValidation
            {
                ID = nv.Id,
                MaNV = nv.MaNv,
                HoTen = nv.HoTen,
                IDQuyen = nv.Idquyen ?? 0,
                IDQuyenKNL = nv.IdquyenKnl ?? 0,
                Email = nv.Email,
                SoDienThoai = nv.DienThoai
            };
            return PartialView(DO);
        }

        [HttpPost]
        public IActionResult Edit(LoginValidation _DO)
        {
            try
            {
                var nv = _db.NhanViens.FirstOrDefault(x => x.Id == _DO.ID);
                if (nv != null)
                {
                    nv.Idquyen = _DO.IDQuyen;
                    nv.Email = _DO.Email;
                    nv.DienThoai = _DO.SoDienThoai;
                    _db.SaveChanges();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "Account");
        }

        // ── Đồng bộ tài khoản từ HR API ────────────────────────────────────────

        public async Task<IActionResult> SyncHR()
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.SYC))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("Index");
            }

            try
            {
                var listNV = await GetHRData();
                if (listNV == null || listNV.Count == 0)
                {
                    TempData["msgError"] = "<script>alert('Không lấy được dữ liệu từ HR API');</script>";
                    return RedirectToAction("Index");
                }

                var prefixes = new[] { "HPDQ", "HMN", "RAY" };

                var hpdqList = listNV
                    .Where(x => !string.IsNullOrEmpty(x.manv)
                             //&& x.manv.Length >= 4
                             && x.tinhtranglamviec == 1
                             && prefixes.Any(p =>
                                    x.manv.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // Lọc chỉ lấy nhân viên HPDQ
                //var hpdqList = listNV
                //    .Where(x => !string.IsNullOrEmpty(x.manv)
                //             //&& x.manv.Length >= 4
                //             //&& x.manv.Length <= 10
                //             && x.tinhtranglamviec == 1
                //             //&& x.manv.StartsWith("HPDQ", StringComparison.OrdinalIgnoreCase)
                //             )
                //    .ToList();

                // ── Bước 1: Pre-build PhongBan & ViTri maps ─────────────────
                // Tránh SaveChanges bên trong loop NhanVien gây conflict tracking
                // Where(!=null) tránh ArgumentNullException khi có record tên null trong DB
                var pbMap = await _db.PhongBans
                    .Where(x => x.TenPhongBan != null)
                    .ToDictionaryAsync(x => x.TenPhongBan!, x => x.IdphongBan);
                var vtMap = await _db.Vitris
                    .Where(x => x.TenViTri != null)
                    .ToDictionaryAsync(x => x.TenViTri!, x => x.IdviTri);

                var newPBs = hpdqList
                    .Where(x => !string.IsNullOrEmpty(x.phongban) && !pbMap.ContainsKey(x.phongban!))
                    .Select(x => x.phongban!).Distinct().ToList();
                var newVTs = hpdqList
                    .Where(x => !string.IsNullOrEmpty(x.vitri) && !vtMap.ContainsKey(x.vitri!))
                    .Select(x => x.vitri!).Distinct().ToList();

                foreach (var name in newPBs)
                {
                    var pb = new Data.Models.PhongBan { TenPhongBan = name };
                    _db.PhongBans.Add(pb);
                    _db.SaveChanges();
                    pbMap[name] = pb.IdphongBan;
                }
                foreach (var name in newVTs)
                {
                    var vt = new Data.Models.Vitri { TenViTri = name };
                    _db.Vitris.Add(vt);
                    _db.SaveChanges();
                    vtMap[name] = vt.IdviTri;
                }

                // ── Bước 2: Sync NhanVien ────────────────────────────────────
                int inserted = 0, updated = 0;
                var allNV = await _db.NhanViens.ToListAsync();
                var defaultMk = Encryptor.MD5Hash("hpdq@2026");

                foreach (var item in hpdqList)
                {
                    pbMap.TryGetValue(item.phongban ?? "", out int idPB);
                    vtMap.TryGetValue(item.vitri ?? "", out int idVT);
                    int? kip = int.TryParse(item.makip, out int k) ? k : null;

                    DateOnly? ngayVaoLam = null;
                    if (!string.IsNullOrEmpty(item.ngayvaolam) &&
                        DateTime.TryParseExact(item.ngayvaolam, "dd/MM/yyyy",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dParsed))
                        ngayVaoLam = DateOnly.FromDateTime(dParsed);

                    var existing = allNV.FirstOrDefault(x =>
                        string.Equals(x.MaNv, item.manv, StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        _db.NhanViens.Add(new NhanVien
                        {
                            MaNv = item.manv,
                            HoTen = item.hoten,
                            HoTenKhongDau = ConvertToUnSign(item.hoten ?? ""),
                            MatKhau = defaultMk,
                            DiaChi = item.diachi,
                            DienThoai = item.sodienthoai,
                            Email = item.email,
                            NgayVaoLam = ngayVaoLam,
                            IdtinhTrangLv = item.tinhtranglamviec,
                            IdphongBan = idPB > 0 ? idPB : null,
                            IdviTri = idVT > 0 ? idVT : null,
                            MaViTri = item.mavitri,
                            Idkip = kip,
                            Idquyen = 4,
                            IsGv = false,
                            CCCD = item.cmnd
                        });
                        inserted++;
                    }
                    else
                    {
                        bool changed = existing.IdphongBan != (idPB > 0 ? idPB : (int?)null)
                                    || existing.IdviTri != (idVT > 0 ? idVT : (int?)null)
                                    || existing.MaViTri != item.mavitri
                                    || existing.Idkip != kip
                                    || existing.CCCD != item.cmnd;
                        if (changed)
                        {
                            if (item.tinhtranglamviec == 0)
                                existing.IdquyenKnl = null; // nghỉ việc → xóa KNL

                            existing.IdphongBan = idPB > 0 ? idPB : null;
                            existing.IdviTri = idVT > 0 ? idVT : null;
                            existing.MaViTri = item.mavitri;
                            existing.Idkip = kip;
                            existing.DiaChi = item.diachi;
                            existing.DienThoai = item.sodienthoai;
                            existing.IdtinhTrangLv = item.tinhtranglamviec;
                            if (!string.IsNullOrEmpty(item.cmnd))
                                existing.CCCD = item.cmnd;
                            updated++;
                        }
                    }
                }

                await _db.SaveChangesAsync();
                var msg = $"Đồng bộ HR thành công: thêm mới {inserted}, cập nhật {updated} nhân viên";
                TempData["msgSuccess"] = $"<script>alert('{msg}');</script>";
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message
                         ?? ex.InnerException?.Message
                         ?? ex.Message;
                TempData["msgError"] = $"<script>alert('Lỗi đồng bộ HR:\\n{inner}');</script>";
            }

            return RedirectToAction("Index");
        }

        private async Task<string> GetHRToken()
        {
            var url = _config["AppSettings:LinkToken"] ?? "";
            var username = _config["AppSettings:Username"] ?? "";
            var password = _config["AppSettings:Password"] ?? "";

            using var client = new HttpClient();
            var body = JsonSerializer.Serialize(new { username, password });
            var resp = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
            if (!resp.IsSuccessStatusCode) return "";

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                      .GetProperty("data")
                      .GetProperty("tokenLogin")
                      .GetString() ?? "";
        }

        private async Task<List<EmployeeHRData>> GetHRData()
        {
            var token = await GetHRToken();
            if (string.IsNullOrEmpty(token)) return new();

            var url = _config["AppSettings:LinkAPI"] ?? "";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            var raw = await client.GetStringAsync(url);

            // API trả về JSON không chuẩn: bỏ 36 ký tự đầu, fix đuôi }]} → }]
            if (raw.Length > 36) raw = raw[36..];
            raw = raw.Replace("}]}", "}]");

            return JsonSerializer.Deserialize<List<EmployeeHRData>>(raw,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                }) ?? new();
        }

        private static string ConvertToUnSign(string s)
        {
            var regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            var temp = s.Normalize(NormalizationForm.FormD);
            return regex.Replace(temp, "").Replace('đ', 'd').Replace('Đ', 'D');
        }

        public class EmployeeHRData
        {
            public string? manv { get; set; }
            public string? hoten { get; set; }
            public string? diachi { get; set; }
            public string? sodienthoai { get; set; }
            public string? email { get; set; }
            public string? ngayvaolam { get; set; }
            public int tinhtranglamviec { get; set; }
            public string? phongban { get; set; }
            public string? mavitri { get; set; }
            public string? vitri { get; set; }
            public string? makip { get; set; }
            public string? cmnd { get; set; }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private int GetOrCreateViTri(string tenViTri)
        {
            var vt = _db.Vitris.FirstOrDefault(x => x.TenViTri == tenViTri);
            if (vt != null) return vt.IdviTri;
            var newVt = new Vitri { TenViTri = tenViTri };
            _db.Vitris.Add(newVt);
            _db.SaveChanges();
            return newVt.IdviTri;
        }

        private int GetOrCreatePhongBan(string tenPhongBan)
        {
            var pb = _db.PhongBans.FirstOrDefault(x => x.TenPhongBan == tenPhongBan);
            if (pb != null) return pb.IdphongBan;
            var newPb = new PhongBan { TenPhongBan = tenPhongBan };
            _db.PhongBans.Add(newPb);
            _db.SaveChanges();
            return newPb.IdphongBan;
        }

        private static DateOnly? ParseNgaySinh(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (double.TryParse(raw, out double oaDate))
            {
                try { return DateOnly.FromDateTime(DateTime.FromOADate(oaDate)); } catch { }
            }
            string[] formats = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return DateOnly.FromDateTime(parsed);
            if (DateTime.TryParse(raw, out parsed))
                return DateOnly.FromDateTime(parsed);
            return null;
        }
    }
}
