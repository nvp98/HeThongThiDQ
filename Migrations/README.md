# Migration Notes

## DataProtectionKeys — Web Farm Fix

**File SQL**: `../migration_dataprotection.sql`

### Vấn đề

Mô hình triển khai: `User → nginx (load balancer) → IIS1 / IIS2 / IIS3`

Người dùng đăng nhập thành công nhưng bị redirect về trang login ngay lập tức khi truy cập qua nginx.

**Nguyên nhân**: Lỗi thuộc nhóm **Web Farm / Load Balancing — Data Protection key mismatch**.

- Mỗi IIS instance tự sinh ra một Data Protection key ring riêng trong memory khi khởi động
- Cookie auth được mã hóa bằng key của IIS1, nhưng request tiếp theo nginx route sang IIS2
- IIS2 dùng key khác → không decrypt được cookie → coi như chưa đăng nhập → redirect về login

**Bằng chứng**: Trình duyệt tích lũy nhiều antiforgery cookie với suffix khác nhau (`.AspNetCore.Antiforgery.xxxxx`) — mỗi suffix là 1 key ring khác nhau từ các IIS instance.

### Giải pháp

Đưa Data Protection key ra ngoài (externalize key store) vào SQL Server — tất cả IIS instance đọc/ghi key từ cùng 1 bảng `DataProtectionKeys`.

**Code** (`Program.cs`):
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ELEARNINGEntities>()
    .SetApplicationName("HeThongThiDQ");
```

**Package cần thêm**:
```
Microsoft.AspNetCore.DataProtection.EntityFrameworkCore 8.0.0
```

**Tạo bảng**: Chạy `migration_dataprotection.sql` trên DB `ELEARNING_DQ` trước khi deploy.

### Ghi chú

- Key rotation mặc định mỗi 90 ngày — không ảnh hưởng đến concurrent login
- Mỗi IIS instance cache key trong memory sau lần đọc đầu, không query DB mỗi request
- Áp dụng tương tự cho bất kỳ hệ thống nào dùng ASP.NET Core Cookie Auth trên môi trường load-balanced
