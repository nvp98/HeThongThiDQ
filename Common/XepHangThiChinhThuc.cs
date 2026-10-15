using HeThongThiDQ.Data;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiDQ.Common
{
    public class BxhRow
    {
        public int Rank { get; set; }
        public int NVID { get; set; }
        public string HoTen { get; set; } = "";
        public string MaNV { get; set; } = "";
        public string TenPB { get; set; } = "";
        public int SoCauDung { get; set; }
        public int TongSoCau { get; set; }
        public int ThoiGianSec { get; set; }
        public bool DaThi { get; set; }
        public bool DongHang { get; set; }
        public DateTime? HoanThanhOnLuyen { get; set; }
        public double DiemOnLuyen { get; set; }
        public int SoDeOnLuyenHoanThanh { get; set; }
    }

    /// <summary>
    /// Xếp hạng thi chính thức theo Điều 4 thể lệ — dùng chung cho bảng xếp hạng và file Excel xuất kết quả.
    /// 1. Điểm bài thi (mỗi câu đúng 1 điểm, tối đa 30) cao hơn xếp trên
    /// 2. Cùng điểm → thời gian làm bài ngắn hơn xếp trên (so đến giây)
    /// 3. Cùng điểm và thời gian → hoàn thành ôn luyện sớm hơn xếp trên (so đến giây)
    /// 4. Trùng cả 3 tiêu chí → đồng hạng (BTC xử lý ngoài hệ thống)
    /// </summary>
    public static class XepHangThiChinhThuc
    {
        // Lớp thi chính thức mới nhất (khi có lớp thi lại thì lấy lớp mở sau cùng)
        public static async Task<int> GetLopThiMoiNhatAsync(ELEARNINGEntities db)
            => await db.LopHocs
                .Where(l => l.IsCoCtdt != 1)
                .OrderByDescending(l => l.Tgbdlh).ThenByDescending(l => l.Idlh)
                .Select(l => l.Idlh)
                .FirstOrDefaultAsync();

        // Thời gian làm bài (giây): ưu tiên ThoiGianThi do server tính, nếu thiếu thì lấy giờ nộp - giờ bắt đầu
        public static int TinhThoiGianSec(int? thoiGianThi, DateTime? gioBatDau, DateTime? gioKetThuc)
        {
            if (thoiGianThi > 0) return thoiGianThi.Value;
            if (gioBatDau != null && gioKetThuc != null)
                return Math.Max(0, (int)(gioKetThuc.Value - gioBatDau.Value).TotalSeconds);
            return 0;
        }

        private static DateTime TruncateToSecond(DateTime d) => d.AddTicks(-(d.Ticks % TimeSpan.TicksPerSecond));

        public static async Task<List<BxhRow>> TinhAsync(ELEARNINGEntities db, int lhId)
        {
            if (lhId == 0) return new();

            // Mỗi CBNV chỉ có 1 bài thi chính thức trong lớp → lấy bài nộp đầu tiên
            var baiThi = (await db.BaiThis.AsNoTracking()
                    .Where(b => b.Idnv != null && b.DiemSo != null && b.Idlh == lhId)
                    .Select(b => new { b.IdbaiThi, Idnv = b.Idnv!.Value, b.DiemSo, b.ThoiGianThi, b.GioBatDau, b.GioKetThuc })
                    .ToListAsync())
                .GroupBy(b => b.Idnv)
                .ToDictionary(g => g.Key, g => g.OrderBy(b => b.IdbaiThi).First());

            var enrolledNvIds = await db.XnhocTaps
                .Where(x => x.Nvid != null && x.Lhid == lhId)
                .Select(x => x.Nvid!.Value)
                .Distinct()
                .ToListAsync();
            var nvIds = enrolledNvIds.Union(baiThi.Keys).ToList();
            if (nvIds.Count == 0) return new();

            var thiIds = baiThi.Values.Select(b => b.IdbaiThi).ToList();
            var tongCauMap = await db.CtbaiThis
                .Where(ct => ct.IdbaiThi != null && thiIds.Contains(ct.IdbaiThi.Value))
                .GroupBy(ct => ct.IdbaiThi!.Value)
                .Select(g => new { g.Key, Tong = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Tong);

            // Thời điểm hoàn thành ôn luyện = lần nộp đầu tiên của đề ôn luyện cuối cùng (đủ tất cả đề)
            var olLhIds = await db.LopHocs.Where(l => l.IsCoCtdt == 1).Select(l => l.Idlh).ToListAsync();
            var olPerNv = (await db.BaiThis.AsNoTracking()
                    .Where(b => b.Idnv != null && nvIds.Contains(b.Idnv.Value) && olLhIds.Contains(b.Idlh ?? 0))
                    .Select(b => new { Idnv = b.Idnv!.Value, Idlh = b.Idlh ?? 0, b.GioKetThuc, b.NgayThi, b.DiemSo })
                    .ToListAsync())
                .GroupBy(b => b.Idnv)
                .ToDictionary(g => g.Key, g =>
                {
                    var nopDauTheoDe = g.GroupBy(b => b.Idlh)
                        .Select(d => d.Min(b => b.GioKetThuc ?? b.NgayThi?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.MaxValue))
                        .ToList();
                    DateTime? hoanThanh = olLhIds.Count > 0 && nopDauTheoDe.Count == olLhIds.Count
                        ? TruncateToSecond(nopDauTheoDe.Max())
                        : null;
                    return (HoanThanh: hoanThanh, Diem: g.Sum(b => b.DiemSo ?? 0), SoDe: nopDauTheoDe.Count);
                });

            var nvMap = await (from n in db.NhanViens
                               join pb in db.PhongBans on n.IdphongBan equals pb.IdphongBan into pbj
                               from pb in pbj.DefaultIfEmpty()
                               where nvIds.Contains(n.Id)
                               select new { n.Id, n.HoTen, n.MaNv, TenPB = pb != null ? pb.TenPhongBan : null })
                              .ToDictionaryAsync(x => x.Id);

            var sorted = nvIds
                .Select(id =>
                {
                    baiThi.TryGetValue(id, out var bt);
                    olPerNv.TryGetValue(id, out var ol);
                    nvMap.TryGetValue(id, out var nv);
                    return new BxhRow
                    {
                        NVID = id,
                        HoTen = nv?.HoTen ?? "",
                        MaNV = nv?.MaNv ?? "",
                        TenPB = nv?.TenPB ?? "",
                        DaThi = bt != null,
                        SoCauDung = (int)Math.Round(bt?.DiemSo ?? 0),
                        TongSoCau = bt != null && tongCauMap.TryGetValue(bt.IdbaiThi, out var t) ? t : 0,
                        ThoiGianSec = bt != null ? TinhThoiGianSec(bt.ThoiGianThi, bt.GioBatDau, bt.GioKetThuc) : 0,
                        HoanThanhOnLuyen = ol.HoanThanh,
                        DiemOnLuyen = ol.Diem,
                        SoDeOnLuyenHoanThanh = ol.SoDe
                    };
                })
                .OrderByDescending(x => x.DaThi)
                .ThenByDescending(x => x.SoCauDung)
                .ThenBy(x => x.ThoiGianSec)
                .ThenBy(x => x.HoanThanhOnLuyen ?? DateTime.MaxValue)
                .ThenBy(x => x.HoTen)
                .ToList();

            // Xếp hạng kiểu thi đấu: trùng cả 3 tiêu chí thì đồng hạng (1, 1, 3...); chưa thi thì không xếp hạng
            BxhRow? prev = null;
            for (int i = 0; i < sorted.Count; i++)
            {
                var x = sorted[i];
                if (!x.DaThi) continue;
                bool trung = prev != null
                             && prev.SoCauDung == x.SoCauDung
                             && prev.ThoiGianSec == x.ThoiGianSec
                             && prev.HoanThanhOnLuyen == x.HoanThanhOnLuyen;
                x.Rank = trung ? prev!.Rank : i + 1;
                if (trung) prev!.DongHang = x.DongHang = true;
                prev = x;
            }
            return sorted;
        }
    }
}
