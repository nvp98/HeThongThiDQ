using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class FPermisionController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private const string ControllerName = "FPermision";

        public FPermisionController(ELEARNINGEntities db, MyAuthentication auth, HomeController home)
        {
            _db = db;
            _auth = auth;
            _home = home;
        }

        // Danh sách nhóm quyền
        public async Task<IActionResult> Index()
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;
            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("Index", "Home");
            }

            var res = await _db.Quyens.OrderBy(x => x.Idquyen).ToListAsync();
            return View(res);
        }

        // Thêm nhóm quyền mới
        [HttpPost]
        public async Task<IActionResult> Create(string TenQuyen)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.ADD))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("Index");
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(TenQuyen))
                {
                    var exists = await _db.Quyens.AnyAsync(x => x.TenQuyen == TenQuyen.Trim());
                    if (!exists)
                    {
                        _db.Quyens.Add(new Quyen { TenQuyen = TenQuyen.Trim() });
                        await _db.SaveChangesAsync();
                    }
                }
                TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới: {e.Message}');</script>";
            }
            return RedirectToAction("Index");
        }

        // Trang chỉnh sửa chi tiết quyền theo từng controller
        public async Task<IActionResult> Edit(int id)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.EDIT))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("Index");
            }

            var quyen = await _db.Quyens.FindAsync(id);
            if (quyen == null) return NotFound();

            var allQuyenCns = await _db.QuyenCns.ToListAsync();
            var resCon = await _db.ListControllers
                .Where(x => x.IsActive == 1)
                .OrderBy(x => x.Mota)
                .ToListAsync();

            var listController = new List<ListControllerViewModel>();
            foreach (var con in resCon)
            {
                var dsquyen = con.DsquyenCn?.Split(',') ?? Array.Empty<string>();
                var lsCheck = new List<ItemCheckViewModel>();

                foreach (var qcn in allQuyenCns)
                {
                    if (!dsquyen.Contains(qcn.Id.ToString())) continue;

                    var detail = await _db.QuyenDetails
                        .Where(x => x.Idquyen == id && x.IdquyenCn == qcn.Id && x.Idcontroller == con.Id)
                        .FirstOrDefaultAsync();

                    lsCheck.Add(new ItemCheckViewModel
                    {
                        Name = qcn.TenQuyenCn,
                        IdCN = qcn.Id,
                        IsChecked = detail?.IsActive == 1
                    });
                }

                listController.Add(new ListControllerViewModel
                {
                    ID = con.Id,
                    Controller = con.Controller,
                    Mota = con.Mota,
                    IsActive = con.IsActive,
                    DsquyenCn = con.DsquyenCn,
                    LSChecked = lsCheck,
                    IsCheck = lsCheck.Count > 0 && lsCheck.All(x => x.IsChecked)
                });
            }

            var allQuyens = await _db.Quyens.ToListAsync();
            ViewBag.SelectQuyen = new SelectList(allQuyens, "Idquyen", "TenQuyen", id);

            return View(new GroupQuyenViewModel
            {
                IDQuyen = quyen.Idquyen,
                TenQuyen = quyen.TenQuyen,
                ListController = listController
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(GroupQuyenViewModel vm)
        {
            try
            {
                foreach (var item in vm.ListController ?? new())
                {
                    foreach (var chec in item.LSChecked ?? new())
                    {
                        var detail = await _db.QuyenDetails
                            .Where(x => x.Idquyen == vm.IDQuyen && x.Idcontroller == item.ID && x.IdquyenCn == chec.IdCN)
                            .FirstOrDefaultAsync();

                        if (detail == null)
                        {
                            _db.QuyenDetails.Add(new QuyenDetail
                            {
                                Idquyen = vm.IDQuyen,
                                Idcontroller = item.ID,
                                IdquyenCn = chec.IdCN,
                                IsActive = chec.IsChecked ? 1 : 0
                            });
                        }
                        else
                        {
                            detail.IsActive = chec.IsChecked ? 1 : 0;
                        }
                    }
                }
                await _db.SaveChangesAsync();
                await _home.InvalidatePermissionCache(vm.IDQuyen);
                TempData["msgSuccess"] = "<script>alert('Cập nhật thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Cập nhật thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("Edit", new { id = vm.IDQuyen });
        }

        // Danh sách user theo nhóm quyền
        public async Task<IActionResult> ListUser(int id)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            if (!listQuyen.Contains(CONSTKEY.PER))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền thực hiện chức năng này');</script>";
                return RedirectToAction("Index");
            }

            var allQuyens = await _db.Quyens.ToListAsync();
            ViewBag.SelectQuyen = new SelectList(allQuyens, "Idquyen", "TenQuyen", id);
            return View();
        }

        // JSON: danh sách user theo nhóm quyền
        public async Task<JsonResult> GetListUser(int id)
        {
            if (!User.Identity!.IsAuthenticated) return Json(null);

            var res = await (from nv in _db.NhanViens.Where(x => x.Idquyen == id && x.IdtinhTrangLv == 1)
                             join pb in _db.PhongBans on nv.IdphongBan equals pb.IdphongBan into pbj
                             from pb in pbj.DefaultIfEmpty()
                             join v in _db.Vitris on nv.IdviTri equals v.IdviTri into vj
                             from v in vj.DefaultIfEmpty()
                             select new EmployeeValidation
                             {
                                 ID = nv.Id,
                                 HoTen = nv.MaNv + " - " + nv.HoTen,
                                 PhongBan = pb != null ? pb.TenPhongBan : "",
                                 TenQuyen = v != null ? v.TenViTri : "",
                                 IDPhongBan = id
                             }).ToListAsync();
            return Json(res);
        }

        // Xóa quyền của user (reset về quyền mặc định = 4)
        public async Task<IActionResult> DeleteQuyen(int id, int? IDQuyen)
        {
            try
            {
                var nv = await _db.NhanViens.FindAsync(id);
                if (nv != null)
                {
                    nv.Idquyen = 4;
                    await _db.SaveChangesAsync();
                }
            }
            catch
            {
                TempData["msgError"] = "<script>alert('Xóa dữ liệu thất bại');</script>";
            }
            return RedirectToAction("ListUser", new { id = IDQuyen });
        }

        // Partial: form thêm user vào nhóm quyền
        public async Task<IActionResult> AddUserQuyen(int? id)
        {
            var allQuyens = await _db.Quyens.Where(x => x.Idquyen == id).ToListAsync();
            ViewBag.IDKNL = new SelectList(allQuyens, "Idquyen", "TenQuyen", id);

            var allNV = await _db.NhanViens.Where(x => x.IdtinhTrangLv == 1).OrderBy(x => x.MaNv).ToListAsync();
            var nv3 = allNV.Select(x => new EmployeeValidation { MaNV = x.MaNv, HoTen = x.MaNv + " - " + x.HoTen }).ToList();
            ViewBag.Selected = new SelectList(nv3, "MaNV", "HoTen");

            var allPB = await _db.PhongBans.OrderBy(x => x.TenPhongBan).ToListAsync();
            ViewBag.IDPB = new SelectList(allPB, "IdphongBan", "TenPhongBan");

            return PartialView("_AddUserQuyen");
        }

        [HttpPost]
        public async Task<IActionResult> AddUserQuyen(AddUserQuyenViewModel vm)
        {
            try
            {
                // Nhập danh sách mã NV bulk
                if (!string.IsNullOrEmpty(vm.NVDG))
                {
                    var codes = Regex.Replace(vm.NVDG, @"[^0-9a-zA-Z]+", " ")
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var code in codes)
                    {
                        var nv = await _db.NhanViens.FirstOrDefaultAsync(x => x.MaNv == code);
                        if (nv != null) nv.Idquyen = vm.IDKNL;
                    }
                }
                // Chọn từ dropdown
                if (vm.Selected != null)
                {
                    foreach (var maNV in vm.Selected)
                    {
                        if (string.IsNullOrEmpty(maNV)) continue;
                        var nv = await _db.NhanViens.FirstOrDefaultAsync(x => x.MaNv == maNV);
                        if (nv != null) nv.Idquyen = vm.IDKNL;
                    }
                }
                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = "<script>alert('Cập nhật thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi cập nhật: {e.Message}');</script>";
            }
            return RedirectToAction("ListUser", new { id = vm.IDKNL });
        }

        // JSON: danh sách nhân viên theo phòng ban
        public async Task<JsonResult> CheckNV(int? IDPB)
        {
            var list = await _db.NhanViens
                .Where(x => x.IdphongBan == IDPB && x.IdtinhTrangLv == 1)
                .OrderBy(x => x.MaNv)
                .Select(x => new EmployeeValidation { MaNV = x.MaNv, HoTen = x.MaNv + " - " + x.HoTen })
                .ToListAsync();
            return Json(list);
        }

        // JSON: kiểm tra danh sách mã NV nhập tay
        public async Task<JsonResult> CheckLSNV(string lsnv)
        {
            var list = new List<EmployeeValidation>();
            if (!string.IsNullOrEmpty(lsnv))
            {
                var codes = Regex.Replace(lsnv, @"[^0-9a-zA-Z]+", " ")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var code in codes)
                {
                    var nv = await _db.NhanViens.FirstOrDefaultAsync(x => x.MaNv == code);
                    if (nv != null)
                        list.Add(new EmployeeValidation { MaNV = nv.MaNv, HoTen = nv.MaNv + " - " + nv.HoTen });
                }
            }
            return Json(list);
        }

        // Trang quản trị danh sách controller và quyền chi tiết
        public async Task<IActionResult> AdminPermision()
        {
            var allQuyenCns = await _db.QuyenCns.ToListAsync();
            var res = await _db.ListControllers.OrderBy(x => x.Mota).ToListAsync();

            var viewModels = res.Select(a =>
            {
                var words = new List<string>();
                if (!string.IsNullOrEmpty(a.DsquyenCn))
                {
                    var dsquyen = a.DsquyenCn.Split(',');
                    foreach (var qcn in allQuyenCns)
                    {
                        if (dsquyen.Contains(qcn.Id.ToString()))
                            words.Add(qcn.MaQuyen ?? "");
                    }
                }
                return new ListControllerViewModel
                {
                    ID = a.Id,
                    Controller = a.Controller,
                    Mota = a.Mota,
                    IsActive = a.IsActive,
                    DsquyenCn = a.DsquyenCn,
                    DsTenQuyen = string.Join(", ", words)
                };
            }).ToList();

            return View(viewModels);
        }

        // Partial: chỉnh sửa controller (tên, quyền chi tiết, trạng thái)
        public async Task<IActionResult> EditController(int id)
        {
            var con = await _db.ListControllers.FindAsync(id);
            if (con == null) return NotFound();

            var allQuyenCns = await _db.QuyenCns.ToListAsync();
            var dsquyen = con.DsquyenCn?.Split(',') ?? Array.Empty<string>();

            var lsCheck = allQuyenCns.Select(qcn => new ItemCheckViewModel
            {
                Name = qcn.TenQuyenCn,
                IdCN = qcn.Id,
                IsChecked = dsquyen.Contains(qcn.Id.ToString())
            }).ToList();

            return PartialView("_EditController", new ListControllerViewModel
            {
                ID = con.Id,
                Controller = con.Controller,
                Mota = con.Mota,
                IsActive = con.IsActive,
                DsquyenCn = con.DsquyenCn,
                LSChecked = lsCheck,
                IsCheck = con.IsActive == 1
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditController(ListControllerViewModel vm)
        {
            try
            {
                var checkedIds = vm.LSChecked?.Where(x => x.IsChecked).Select(x => x.IdCN.ToString()).ToList() ?? new();

                // Deactivate QuyenDetail entries for unchecked items
                if (vm.LSChecked != null)
                {
                    foreach (var item in vm.LSChecked.Where(x => !x.IsChecked))
                    {
                        var details = await _db.QuyenDetails
                            .Where(x => x.Idcontroller == vm.ID && x.IdquyenCn == item.IdCN)
                            .ToListAsync();
                        foreach (var d in details) d.IsActive = 0;
                    }
                }

                var con = await _db.ListControllers.FindAsync(vm.ID);
                if (con != null)
                {
                    con.Mota = vm.Mota;
                    con.Controller = vm.Controller;
                    con.IsActive = vm.IsCheck ? 1 : 0;
                    con.DsquyenCn = string.Join(",", checkedIds);
                }
                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = "<script>alert('Cập nhật thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Chỉnh sửa thất bại: {e.Message}');</script>";
            }
            return RedirectToAction("AdminPermision");
        }

        // Đồng bộ tất cả controller trong assembly vào DB
        public async Task<IActionResult> Sync()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var controllerTypes = assembly.GetExportedTypes()
                    .Where(t => typeof(ControllerBase).IsAssignableFrom(t));

                foreach (var type in controllerTypes)
                {
                    int idx = type.Name.IndexOf("Controller");
                    if (idx < 0) continue;
                    var name = type.Name[..idx];
                    var exists = await _db.ListControllers.AnyAsync(x => x.Controller == name);
                    if (!exists)
                    {
                        _db.ListControllers.Add(new ListController { Controller = name, IsActive = 1 });
                    }
                }
                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = "<script>alert('Đồng bộ dữ liệu thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi đồng bộ: {e.Message}');</script>";
            }
            return RedirectToAction("AdminPermision");
        }
    }
}
