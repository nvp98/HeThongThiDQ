using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly HomeController _home;
        private const string ControllerName = "Notification";

        public NotificationController(ELEARNINGEntities db, MyAuthentication auth, HomeController home)
        {
            _db = db;
            _auth = auth;
            _home = home;
        }

        public async Task<IActionResult> Index(int? page)
        {
            var listQuyen = await _home.GetPermisionCN(_auth.IDQuyen, ControllerName);
            ViewBag.QUYENCN = listQuyen;

            if (!listQuyen.Contains(CONSTKEY.V))
            {
                TempData["msgError"] = "<script>alert('Bạn không có quyền truy cập chức năng này');</script>";
                return RedirectToAction("", "Home");
            }

            var res = await _db.ThongBaos.AsNoTracking()
                .Select(a => new NotificationView
                {
                    ID         = a.Id,
                    NgayTB     = a.NgayTb.HasValue ? a.NgayTb.Value.ToDateTime(TimeOnly.MinValue) : default,
                    NoiDungTB  = a.NoiDungTb,
                    TinhTrang  = a.TinhTrang
                })
                .OrderBy(x => x.NgayTB)
                .ToListAsync();

            int pageNumber = page ?? 1;
            return View(res.ToPagedList(pageNumber, 50));
        }

        public IActionResult Create()
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> Create(NotificationView _DO)
        {
            try
            {
                _db.ThongBaos.Add(new ThongBao
                {
                    NoiDungTb = _DO.NoiDungTB,
                    NgayTb    = DateOnly.FromDateTime(DateTime.Now),
                    TinhTrang = 1
                });
                await _db.SaveChangesAsync();
                TempData["msgSuccess"] = "<script>alert('Thêm mới thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgError"] = $"<script>alert('Có lỗi khi thêm mới: {e.Message}');</script>";
            }
            return RedirectToAction("Index", "Notification");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.ThongBaos.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound();

            var DO = new NotificationView
            {
                ID         = item.Id,
                NoiDungTB  = item.NoiDungTb,
                NgayTB     = item.NgayTb.HasValue ? item.NgayTb.Value.ToDateTime(TimeOnly.MinValue) : default,
                TinhTrang  = item.TinhTrang
            };

            ViewBag.SelectList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new() { Text = "Không Thông báo", Value = "0" },
                    new() { Text = "Hoạt động",       Value = "1" }
                },
                "Value", "Text", DO.TinhTrang);

            return PartialView(DO);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(NotificationView _DO)
        {
            try
            {
                var item = await _db.ThongBaos.FirstOrDefaultAsync(x => x.Id == _DO.ID);
                if (item != null)
                {
                    item.NoiDungTb = _DO.NoiDungTB;
                    item.NgayTb    = DateOnly.FromDateTime(DateTime.Now);
                    item.TinhTrang = _DO.TinhTrang;
                    await _db.SaveChangesAsync();
                }
                TempData["msgSuccess"] = "<script>alert('Cập nhập thành công');</script>";
            }
            catch (Exception e)
            {
                TempData["msgSuccess"] = $"<script>alert('Cập nhập thất bại {e.Message}');</script>";
            }
            return RedirectToAction("Index", "Notification");
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var item = await _db.ThongBaos.FirstOrDefaultAsync(x => x.Id == id);
                if (item != null)
                {
                    _db.ThongBaos.Remove(item);
                    await _db.SaveChangesAsync();
                }
            }
            catch
            {
                TempData["msgError"] = "<script>alert('Xóa dữ liệu thất bại');</script>";
            }
            return RedirectToAction("Index", "Notification");
        }

        public async Task<IActionResult> Details()
        {
            var item = await _db.ThongBaos.AsNoTracking()
                .Where(x => x.TinhTrang == 1)
                .OrderBy(x => x.NgayTb)
                .FirstOrDefaultAsync();

            if (item == null) return PartialView((NotificationView?)null);

            var model = new NotificationView
            {
                ID        = item.Id,
                NoiDungTB = item.NoiDungTb,
                NgayTB    = item.NgayTb.HasValue ? item.NgayTb.Value.ToDateTime(TimeOnly.MinValue) : default,
                TinhTrang = item.TinhTrang
            };
            return PartialView(model);
        }
    }
}
