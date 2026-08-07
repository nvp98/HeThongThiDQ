using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Security.Claims;

namespace HeThongThiDQ.Controllers
{
    public class LoginController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _mux;

        private static readonly DistributedCacheEntryOptions _sessionOpts =
            new() { SlidingExpiration = TimeSpan.FromMinutes(360) };

        public LoginController(ELEARNINGEntities db, MyAuthentication auth,
                               IDistributedCache cache, IConnectionMultiplexer mux)
        {
            _db    = db;
            _auth  = auth;
            _cache = cache;
            _mux   = mux;
        }

        public async Task<IActionResult> Index()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginUser(LoginValidation u)
        {
            if (!string.IsNullOrEmpty(u.SoDienThoai) && !string.IsNullOrEmpty(u.MatKhau))
            {
                string mk = Encryptor.MD5Hash(u.MatKhau);
                NhanVien? user = await _db.NhanViens
                    .Where(x => (x.MaNv == u.SoDienThoai || x.DienThoai == u.SoDienThoai)
                                && x.MatKhau == mk
                                && x.IdtinhTrangLv == 1)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    // Sinh token mới — ghi đè session cũ trên thiết bị khác
                    var sessionToken = Guid.NewGuid().ToString("N");

                    var claims = new List<Claim>
                    {
                        new Claim("NV_ID",         user.Id.ToString()),
                        new Claim("NV_MaNV",        user.MaNv ?? ""),
                        new Claim("NV_HoTen",       user.HoTen ?? ""),
                        new Claim("NV_IDPhongBan",  (user.IdphongBan ?? 0).ToString()),
                        new Claim("NV_IDQuyen",     (user.Idquyen ?? 0).ToString()),
                        new Claim("NV_IDViTri",     (user.IdviTri ?? 0).ToString()),
                        new Claim("NV_IDQuyenKNL",  (user.IdquyenKnl ?? 0).ToString()),
                        new Claim("NV_IDVTKNL",     (user.Idvtknl ?? 0).ToString()),
                        new Claim("NV_MaViTri",     user.MaViTri ?? ""),
                        new Claim("NV_ST",          sessionToken),
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties { IsPersistent = false });

                    // Lưu token vào Redis — thiết bị cũ sẽ bị kick ở request tiếp theo
                    try
                    {
                        await _cache.SetStringAsync(
                            $"user:session:{user.Id}", sessionToken, _sessionOpts);
                    }
                    catch { }

                    // Ghi thống kê lượt truy cập
                    try
                    {
                        var rdb  = _mux.GetDatabase();
                        var now  = DateTime.Now;
                        var date = now.ToString("yyyyMMdd");
                        var hour = now.ToString("HH");

                        await rdb.StringIncrementAsync("HPDQ:stats:logins:total");

                        var dailyKey = $"HPDQ:stats:logins:daily:{date}";
                        await rdb.StringIncrementAsync(dailyKey);
                        await rdb.KeyExpireAsync(dailyKey, TimeSpan.FromDays(90));

                        var hourlyKey = $"HPDQ:stats:logins:hourly:{date}:{hour}";
                        await rdb.StringIncrementAsync(hourlyKey);
                        await rdb.KeyExpireAsync(hourlyKey, TimeSpan.FromDays(7));
                    }
                    catch { }

                    TempData["ClearLocalStorage"] = true;
                    return RedirectToAction("Index", "EClassroom");
                }
                else
                {
                    TempData["msglg"] = "<script>alert('Tài khoản hoặc mật khẩu không đúng, liên hệ P.CNTT&CĐS nếu bạn quên mật khẩu')</script>";
                    return RedirectToAction("Index", "Login");
                }
            }
            else
            {
                TempData["msglg"] = "<script>alert('Vui lòng nhập tài khoản và mật khẩu')</script>";
                return RedirectToAction("Index", "Login");
            }
        }

        public async Task<IActionResult> Logout()
        {
            int idNV = _auth.ID;
            if (idNV > 0)
            {
                var logs = _db.HistoryLogs.Where(x => x.NhanVienId == idNV);
                _db.HistoryLogs.RemoveRange(logs);
                await _db.SaveChangesAsync();

                // Xóa session token + xóa khỏi danh sách online
                try { await _cache.RemoveAsync($"user:session:{idNV}"); } catch { }
                try { await _mux.GetDatabase().SortedSetRemoveAsync("HPDQ:online:users", idNV.ToString()); } catch { }
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(LoginValidation model)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                ViewBag.Message = "<script>alert('Lỗi thay đổi mật khẩu')</script>";
                return View();
            }

            string mk = Encryptor.MD5Hash(model.MatKhauCu ?? "");
            NhanVien? user = await _db.NhanViens
                .SingleOrDefaultAsync(x => x.MaNv == _auth.Username && x.MatKhau == mk);

            if (user != null)
            {
                user.MatKhau = Encryptor.MD5Hash(model.MatKhau ?? "");
                await _db.SaveChangesAsync();
                HttpContext.Session.Clear();
                ViewBag.Message = "<script>alert('Thay đổi mật khẩu thành công');window.location.href = '/Login'</script>";
            }
            else
            {
                ViewBag.Message = "<script>alert('Mật khẩu cũ không đúng, vui lòng nhập lại')</script>";
            }

            return View();
        }

        [HttpPost]
        public IActionResult VerifyToken([FromBody] TokenRequest model)
        {
            var nv = _db.NhanViens.FirstOrDefault(x => x.MaNv == model.TenDangNhap);
            if (nv == null) return Json(new { isValid = false });

            var token = _db.HistoryLogs.FirstOrDefault(t =>
                t.NhanVienId == nv.Id &&
                t.DeviceId == model.Token &&
                t.ExpireTime < DateTime.Now);

            return Json(new { isValid = token != null });
        }

        public class TokenRequest
        {
            public string? Token { get; set; }
            public string? TenDangNhap { get; set; }
        }
    }
}
