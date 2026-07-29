using System.Security.Claims;

namespace HeThongThiDQ.Common
{
    /// <summary>
    /// Thay thế FormsAuthentication cũ. Đọc thông tin từ ClaimsPrincipal thay vì HttpContext.Current.
    /// Cookie format cũ: "ID;MaNV;HoTen;IDPhongBan;IDQuyen;IDViTri;IDQuyenKNL;IDVTKNL;MaViTri"
    /// </summary>
    public class MyAuthentication
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MyAuthentication(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private string? GetClaim(string claimType)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
        }

        public int ID => int.TryParse(GetClaim("NV_ID"), out var v) ? v : 0;
        public string? Username => GetClaim("NV_MaNV");
        public string? TenNV => GetClaim("NV_HoTen");
        public int IDPhongban => int.TryParse(GetClaim("NV_IDPhongBan"), out var v) ? v : 0;
        public int IDQuyen => int.TryParse(GetClaim("NV_IDQuyen"), out var v) ? v : 0;
        public int IDViTri => int.TryParse(GetClaim("NV_IDViTri"), out var v) ? v : 0;
        public int IDQuyenKNL => int.TryParse(GetClaim("NV_IDQuyenKNL"), out var v) ? v : 0;
        public int? IDVTKNL => int.TryParse(GetClaim("NV_IDVTKNL"), out var v) ? v : (int?)null;
        public string? MaViTri => GetClaim("NV_MaViTri");
    }
}
