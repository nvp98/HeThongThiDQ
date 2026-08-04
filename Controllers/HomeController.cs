using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace HeThongThiDQ.Controllers
{
    public class HomeController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;
        private readonly IDistributedCache _cache;

        private static readonly DistributedCacheEntryOptions _permCacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        public HomeController(ELEARNINGEntities db, MyAuthentication auth, IDistributedCache cache)
        {
            _db = db;
            _auth = auth;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }

        public async Task<List<string>> GetPermisionCN(int? idQuyen, string controllerName)
        {
            var cacheKey = $"perm:cn:{idQuyen}:{controllerName}";

            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (cached != null)
                    return JsonSerializer.Deserialize<List<string>>(cached)!;
            }
            catch { }

            var lsQuyen = await (from a in _db.QuyenDetails.Where(x => x.Idquyen == idQuyen && x.IsActive == 1)
                                 join b in _db.QuyenCns on a.IdquyenCn equals b.Id
                                 join c in _db.ListControllers.Where(x => x.Controller == controllerName && x.IsActive == 1)
                                     on a.Idcontroller equals c.Id
                                 select b.MaQuyen).ToListAsync();

            if (!lsQuyen.Contains(CONSTKEY.V))
                lsQuyen = new List<string>();

            try { await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(lsQuyen), _permCacheOptions); }
            catch { }

            return lsQuyen;
        }

        public async Task<List<string>> GetPermisionControll(int? idQuyen)
        {
            var cacheKey = $"perm:ctrl:{idQuyen}";

            try
            {
                var cached = await _cache.GetStringAsync(cacheKey);
                if (cached != null)
                    return JsonSerializer.Deserialize<List<string>>(cached)!;
            }
            catch { }

            var result = await (from a in _db.QuyenDetails.Where(x => x.Idquyen == idQuyen && x.IdquyenCn == 1 && x.IsActive == 1)
                                join b in _db.ListControllers.Where(x => x.IsActive == 1) on a.Idcontroller equals b.Id
                                select b.Controller).ToListAsync();

            try { await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), _permCacheOptions); }
            catch { }

            return result;
        }

        // Gọi khi admin cập nhật QuyenDetails — xóa cache của nhóm quyền đó
        public async Task InvalidatePermissionCache(int idQuyen)
        {
            try { await _cache.RemoveAsync($"perm:ctrl:{idQuyen}"); }
            catch { }
            try { await _cache.RemoveAsync($"perm:cn:{idQuyen}:*"); }
            catch { }
        }

        [HttpGet]
        public IActionResult TestSpeed()
        {
            return Content("OK");
        }

        [HttpPost]
        public IActionResult KeepSessionAlive()
        {
            return Json(new { result = "Success" });
        }
    }
}
