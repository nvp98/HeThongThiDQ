using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Controllers
{
    public class HomeController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;

        public HomeController(ELEARNINGEntities db, MyAuthentication auth)
        {
            _db = db;
            _auth = auth;
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
            var lsQuyen = await (from a in _db.QuyenDetails.Where(x => x.Idquyen == idQuyen && x.IsActive == 1)
                                 join b in _db.QuyenCns on a.IdquyenCn equals b.Id
                                 join c in _db.ListControllers.Where(x => x.Controller == controllerName && x.IsActive == 1)
                                     on a.Idcontroller equals c.Id
                                 select b.MaQuyen).ToListAsync();

            if (!lsQuyen.Contains(CONSTKEY.V))
                lsQuyen = new List<string>();

            return lsQuyen;
        }

        public async Task<List<string>> GetPermisionControll(int? idQuyen)
        {
            return await (from a in _db.QuyenDetails.Where(x => x.Idquyen == idQuyen && x.IdquyenCn == 1 && x.IsActive == 1)
                          join b in _db.ListControllers.Where(x => x.IsActive == 1) on a.Idcontroller equals b.Id
                          select b.Controller).ToListAsync();
        }

        [HttpPost]
        public IActionResult KeepSessionAlive()
        {
            return Json(new { result = "Success" });
        }
    }
}
