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
    public class SHistoryController : Controller
    {
        private readonly ELEARNINGEntities _db;
        private readonly MyAuthentication _auth;

        public SHistoryController(ELEARNINGEntities db, MyAuthentication auth)
        {
            _db = db;
            _auth = auth;
        }

        public async Task<IActionResult> Index(int? page)
        {
            int id = _auth.ID;

            var raw = await (from h in _db.XnhocTaps
                             join l in _db.LopHocs on h.Lhid equals l.Idlh
                             join n in _db.NhanViens on h.Nvid equals n.Id
                             join p in _db.PhongBans on h.Pbid equals p.IdphongBan into pj
                             from p in pj.DefaultIfEmpty()
                             join v in _db.Vitris on h.Vtid equals v.IdviTri into vj
                             from v in vj.DefaultIfEmpty()
                             join nd in _db.NoiDungDts on l.Ndid equals nd.Idnd into ndj
                             from nd in ndj.DefaultIfEmpty()
                             join lv in _db.LinhVucDts on nd.Lvdtid equals lv.Idlvdt into lvj
                             from lv in lvj.DefaultIfEmpty()
                             where n.Id == id
                             select new
                             {
                                 h.Idht,
                                 PBID     = h.Pbid ?? 0,
                                 TenPB    = p != null ? p.TenPhongBan : null,
                                 NVID     = n.Id,
                                 n.MaNv,
                                 n.HoTen,
                                 VTID     = h.Vtid ?? 0,
                                 TenViTri = v != null ? v.TenViTri : null,
                                 l.Idlh,
                                 l.TenLh,
                                 TenND    = nd != null ? nd.NoiDung : null,
                                 TenLVDT  = lv != null ? lv.TenLvdt : null,
                                 l.Tgbdlh,
                                 l.Tgktlh,
                                 h.NgayTg,
                                 h.NgayHt,
                                 h.Xntg,
                                 h.Xnht
                             }).OrderByDescending(x => x.Tgbdlh).ToListAsync();

            var baiThiAll = await _db.BaiThis.AsNoTracking().Where(b => b.Idnv == id).ToListAsync();

            var res = raw.Select(x =>
            {
                var ketqua = baiThiAll
                    .Where(b => b.Idlh == x.Idlh && b.Idnv == x.NVID)
                    .OrderBy(b => b.NgayThi)
                    .ToList();

                string kq; string diem;
                int c = ketqua.Count;
                if (c == 0) { kq = "Chưa hoàn thành"; diem = ""; }
                else
                {
                    var passed = ketqua.Where(b => b.TinhTrang == true).ToList();
                    if (passed.Count > 0) { kq = "Đạt"; diem = passed[0].DiemSo?.ToString() ?? ""; }
                    else if (c >= 3)      { kq = "Không đạt"; diem = ketqua[c - 1].DiemSo?.ToString() ?? ""; }
                    else                  { kq = "Chưa hoàn thành"; diem = ketqua[c - 1].DiemSo?.ToString() ?? ""; }
                }

                return new ConfirmEStudyValidation
                {
                    IDHT    = x.Idht,
                    PBID    = x.PBID,
                    TenPB   = x.TenPB,
                    NVID    = x.NVID,
                    MaNV    = x.MaNv,
                    HoTenHV = x.HoTen,
                    VTID    = x.VTID,
                    TenVT   = x.TenViTri,
                    LHID    = x.Idlh,
                    TenLH   = x.TenLh,
                    TenND   = x.TenND,
                    LinhVuc = x.TenLVDT,
                    TGBDLH  = x.Tgbdlh ?? default,
                    TGKTLH  = x.Tgktlh ?? default,
                    NgayTG  = x.NgayTg.HasValue ? x.NgayTg.Value.ToDateTime(TimeOnly.MinValue) : default,
                    NgayHT  = x.NgayHt.HasValue ? x.NgayHt.Value.ToDateTime(TimeOnly.MinValue) : default,
                    XNTG    = x.Xntg ?? false,
                    XNHT    = x.Xnht ?? false,
                    PPDaoTao = "Mở lớp tập trung (E-learning)",
                    KetQuaThi = kq,
                    DiemText  = diem
                };
            }).ToList();

            int pageNumber = page ?? 1;
            return PartialView(res.ToPagedList(pageNumber, 20));
        }
    }
}
