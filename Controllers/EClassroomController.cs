using HeThongThiDQ.Common;
using HeThongThiDQ.Data;
using HeThongThiDQ.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace HeThongThiDQ.Controllers
{
    [Authorize]
    public class EClassroomController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;

        public EClassroomController(ELEARNINGEntities db, MyAuthentication auth)
        {
            _db = db;
            _auth = auth;
        }

        public async Task<IActionResult> Index(int? page)
        {
            int id = _auth.ID;

            var res = await (from h in _db.XnhocTaps
                             join l in _db.LopHocs on h.Lhid equals l.Idlh
                             join n in _db.NhanViens on h.Nvid equals n.Id
                             join p in _db.PhongBans on n.IdphongBan equals p.IdphongBan
                             join nd in _db.NoiDungDts on l.Ndid equals nd.Idnd into ndj
                             from nd in ndj.DefaultIfEmpty()
                             join lv in _db.LinhVucDts on nd.Lvdtid equals lv.Idlvdt into lvj
                             from lv in lvj.DefaultIfEmpty()
                             join v in _db.Vitris on n.IdviTri equals v.IdviTri into vj
                             from v in vj.DefaultIfEmpty()
                             where n.Id == id
                             select new
                             {
                                 h.Idht,
                                 p.IdphongBan,
                                 p.TenPhongBan,
                                 n.Id,
                                 MaNv          = n.MaNv,
                                 n.HoTen,
                                 TenViTri      = v != null ? v.TenViTri : null,
                                 l.Idlh,
                                 l.MaLh,
                                 l.TenLh,
                                 NoiDung       = nd != null ? nd.NoiDung : null,
                                 TenLvdt       = lv != null ? lv.TenLvdt : null,
                                 VideoNd       = nd != null ? nd.VideoNd : null,
                                 ImageNd       = nd != null ? nd.ImageNd : null,
                                 l.Tgbdlh,
                                 l.Tgktlh,
                                 h.NgayTg,
                                 h.NgayHt,
                                 h.Xntg,
                                 h.Xnht,
                                 l.ToChucThi,
                                 l.IddeThi,
                                 l.IsCoCtdt
                             }).ToListAsync();

            var model = res.Select(x => new EClassroomValidation
            {
                IDHT        = x.Idht,
                PBID        = x.IdphongBan,
                TenPB       = x.TenPhongBan,
                NVID        = x.Id,
                MaNV        = x.MaNv,
                HoTenHV    = x.HoTen,
                TenVT       = x.TenViTri,
                LHID        = x.Idlh,
                MaLH        = x.MaLh,
                TenLH       = x.TenLh,
                TenND       = x.NoiDung,
                LinhVuc     = x.TenLvdt,
                VideoLH     = x.VideoNd,
                ImageLH     = x.ImageNd,
                TGBDLH      = x.Tgbdlh ?? default,
                TGKTLH      = x.Tgktlh ?? default,
                NgayTG      = x.NgayTg.HasValue ? x.NgayTg.Value.ToDateTime(TimeOnly.MinValue) : default,
                NgayHT      = x.NgayHt.HasValue ? x.NgayHt.Value.ToDateTime(TimeOnly.MinValue) : default,
                XNTG        = x.Xntg ?? false,
                XNHT        = x.Xnht ?? false,
                ToChucThi   = x.ToChucThi ?? false,
                IDDeThi     = x.IddeThi,
                ThiNhieuLan = x.IsCoCtdt ?? 0
            }).OrderBy(x => x.LHID).ToList();

            var lhIds        = model.Select(x => x.LHID).ToList();
            var baiThis      = await _db.BaiThis.AsNoTracking()
                .Where(x => x.Idnv == id && lhIds.Contains(x.Idlh ?? 0))
                .OrderBy(x => x.IdbaiThi).ToListAsync();
            var btIds        = baiThis.Select(b => b.IdbaiThi).ToList();
            var ctBaiThis    = await _db.CtbaiThis.AsNoTracking().Where(x => btIds.Contains((int)x.IdbaiThi)).ToListAsync();
            var deThiIds     = model.Where(x => x.IDDeThi.HasValue).Select(x => x.IDDeThi!.Value).ToList();
            var cauHoiDeThis = await _db.CauHoiDeThis.AsNoTracking().Where(x => deThiIds.Contains(x.IddeThi ?? 0)).ToListAsync();

            foreach (var m in model)
            {
                var mBaiThis = baiThis.Where(x => x.Idlh == m.LHID).ToList();
                m.BaiThiCount = mBaiThis.Count;
                if (mBaiThis.Count > 0)
                {
                    var lastBT = mBaiThis.Last();
                    m.LastIDBaiThi = lastBT.IdbaiThi;
                    m.LastDiemSo   = lastBT.DiemSo;
                    var lastCT     = ctBaiThis.Where(x => x.IdbaiThi == lastBT.IdbaiThi).ToList();
                    m.SoCauDung    = lastCT.Count(x => x.Diem != 0);
                    m.TongSoCau    = lastCT.Count;
                }
                m.TongDiem = cauHoiDeThis.Where(x => x.IddeThi == m.IDDeThi).Sum(x => x.Diem);
            }

            return View(model);
        }

        public async Task<IActionResult> HistoryTest(int? page, int? IDLH, int? IDNV)
        {
            var res = await (from h in _db.BaiThis
                                 .Where(x => x.Idnv == IDNV && x.Idlh == IDLH)
                             join l in _db.LopHocs on h.Idlh equals l.Idlh
                             join n in _db.NhanViens on h.Idnv equals n.Id
                             select new HistoryTestView
                             {
                                 IDBaiThi    = h.IdbaiThi,
                                 IDNV        = n.Id,
                                 HoTen       = n.HoTen,
                                 IDLH        = l.Idlh,
                                 DiemThi     = h.DiemSo,
                                 NgayThi     = h.NgayThi.HasValue
                                     ? h.NgayThi.Value.ToDateTime(TimeOnly.MinValue)
                                     : (DateTime?)null,
                                 ThoiGianThi = h.ThoiGianThi ?? 0
                             }).ToListAsync();

            if (page == null) page = 1;
            return View(res.ToPagedList(page.Value, 1000));
        }

        public async Task<IActionResult> ViewResult(int? IDLH, int? IDBaiThi)
        {
            ViewBag.IDLH    = IDLH;
            ViewBag.IDBaiThi = IDBaiThi;
            ViewBag.IDNV    = _auth.ID;

            var dethi   = await _db.LopHocs.AsNoTracking().FirstOrDefaultAsync(x => x.Idlh == IDLH);
            var tongdiem = await _db.CauHoiDeThis.AsNoTracking()
                .Where(x => x.IddeThi == dethi!.IddeThi)
                .SumAsync(x => x.Diem);

            var baithi = await _db.BaiThis.AsNoTracking().FirstOrDefaultAsync(x => x.IdbaiThi == IDBaiThi);
            ViewBag.DiemThi = (baithi?.DiemSo ?? 0) + "/" + tongdiem;

            return View();
        }
    }
}
