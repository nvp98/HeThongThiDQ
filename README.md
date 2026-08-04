# HeThongThiDQ — Hệ Thống Thi Đánh Giá Năng Lực Hòa Phát DQ

ASP.NET Core 8 MVC · EF Core · SQL Server · Redis · IIS + Nginx

---

## Stack

| Thành phần | Chi tiết |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| ORM | EF Core (SQL Server) |
| DB | `ELEARNING_DQ` tại `10.192.212.29,1433` |
| Cache | Redis `127.0.0.1:6379` |
| Hosting | IIS inprocess, phía trước là Nginx reverse proxy |
| Auth | Cookie Authentication (`.HPDQ.Auth`) |

---

## Redis Cache

### Cấu hình

- `appsettings.json` → connection string key `"Redis"`
- `Program.cs` → `AddStackExchangeRedisCache`, instance name prefix `"HPDQ:"`

### Đã cache

| Cache key | Dữ liệu | TTL | Xóa khi |
|---|---|---|---|
| `HPDQ:perm:ctrl:{idQuyen}` | Danh sách controller mà nhóm quyền được xem | 30 phút | Gọi `InvalidatePermissionCache(idQuyen)` |
| `HPDQ:perm:cn:{idQuyen}:{controllerName}` | Danh sách chức năng (mã quyền) trong một controller | 30 phút | *(chưa có invalidate riêng)* |

### Invalidate cache

Khi admin cập nhật `QuyenDetails`, gọi:

```csharp
await _homeController.InvalidatePermissionCache(idQuyen);
```

> **Lưu ý:** `InvalidatePermissionCache` hiện chỉ xóa key `perm:ctrl:*`.
> Key `perm:cn:*` sẽ tự hết hạn sau 30 phút hoặc cần xóa thủ công.

---

## Middleware Order (quan trọng)

```
UseForwardedHeaders   ← phải đầu tiên để nginx proxy hoạt động đúng
UseResponseCompression
UseStaticFiles
UseRouting
UseSession            ← phải trước Authentication
UseAuthentication
UseAuthorization
```

---

## Data Protection

Keys được persist vào DB (`ELEARNINGEntities`) — chia sẻ giữa tất cả IIS worker processes, tránh mất session khi app pool recycle.

---

## Deploy

1. Publish → IIS site
2. Nginx cấu hình `proxy_pass` tới IIS port, truyền `X-Forwarded-For` và `X-Forwarded-Proto`
3. Đảm bảo Redis service đang chạy trên `127.0.0.1:6379`
