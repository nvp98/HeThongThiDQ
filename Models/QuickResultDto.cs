namespace HeThongThiDQ.Models;

public record QuickResultDto(
    double DiemSo,
    int    TongSoCau,
    int    SoCauDung,
    int    SoCauSai,
    int    SoCauChuaTraLoi,
    int    ThoiGianSec,
    long   TGBDLamBaiThi,
    long   GioKetThucMs,
    string TaiKhoan,
    string HoTen
);
