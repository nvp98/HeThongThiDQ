# Migration Notes

## Redis Exam Session — Lưu bài thi vào Redis

### Mục tiêu

- Thí sinh có thể **đổi thiết bị giữa chừng** và tiếp tục thi từ đúng trạng thái đã lưu
- **Lưu thời điểm chọn từng đáp án** (`ThoiGianChon`) để phân tích sau
- Redis chỉ lưu tạm (TTL sliding 30 phút), **không ghi DB** cho đến khi nộp bài

---

### Cấu trúc Redis session

**Key:** `exam:session:{IDNV}:{IDLH}`  
**TTL:** Sliding 30 phút (reset mỗi lần thí sinh chọn đáp án)

```json
{
  "IDDeThi": 5,
  "IDND": 2,
  "IDLH": 10,
  "IDNV": 123,
  "TotalTimeSec": 1800,
  "StartTimestamp": 1754321000000,
  "EndTimestamp":   1754322800000,
  "SavedAt":        1754321500000,
  "QuestionOrder": [101, 87, 45],
  "Answers": {
    "101": { "AnswerId": 3,    "ChosenAt": 1754321060000 },
    "87":  { "AnswerId": null, "ChosenAt": null }
  }
}
```

- `StartTimestamp` / `EndTimestamp`: server set khi tạo session → timer trên mọi thiết bị đều chính xác
- `SavedAt`: thời điểm lưu Redis lần cuối
- `QuestionOrder`: thứ tự câu hỏi sau khi shuffle → đổi thiết bị vẫn giữ nguyên thứ tự
- `Answers[IDCH].ChosenAt`: Unix ms khi thí sinh chọn đáp án đó

---

### Luồng hoạt động

```
1. Thí sinh vào trang thi (STestController.Index)
   ├─ Redis có session hợp lệ?
   │     Có  → Restore QuestionOrder + Answers + timer từ EndTimestamp
   │     Không → Shuffle câu hỏi, tạo session mới, lưu Redis
   └─ Truyền ExamSession xuống view qua ViewBag.ExamSession

2. Thí sinh chọn đáp án (frontend onchange → saveAnswer)
   ├─ Lưu vào localStorage (fallback)
   └─ Gọi POST /STest/AutoSave { IDLH, IDCH, AnswerId, ChosenAt: Date.now() }
         → Cập nhật Answers[IDCH] trong Redis
         → Reset TTL 30 phút
         → KHÔNG ghi DB

3. Thí sinh đổi thiết bị
   → Vào lại trang thi → Index đọc Redis → restore đúng thứ tự câu hỏi + đáp án + timer

4. Thí sinh nộp bài (SubmitExamAjax)
   ├─ Đọc ChosenAt từ Redis cho từng câu
   ├─ Lưu BaiThi vào DB
   ├─ Lưu CtbaiThi (Answer + Diem + ThoiGianChon) vào DB
   └─ Xóa Redis key
```

---

### Fallback khi Redis down

- `AutoSave` trả về `{ success: true }` (không fail request)
- Timer dùng localStorage như cũ
- Đáp án restore từ localStorage
- `ThoiGianChon` = null khi lưu DB (chấp nhận được)
- Bài thi vẫn nộp được bình thường

---

### Các file đã thay đổi

| File | Thay đổi |
|------|----------|
| `Models/ExamSession.cs` | Mới — class ExamSession + SessionAnswer |
| `Data/Models/CtbaiThi.cs` | Thêm property `DateTime? ThoiGianChon` |
| `Controllers/STestController.cs` | Inject IDistributedCache, cập nhật Index / AutoSave / SubmitExamAjax |
| `Views/STest/Index.cshtml` | Timer dùng EndTimestamp, saveAnswer gọi AutoSave, restoreAnswers từ Redis session |
| `Migrations/add_ctbaithi_thoigianchon.sql` | Mới — ALTER TABLE thêm cột ThoiGianChon |

---

### Migration DB cần chạy

**File SQL**: `add_ctbaithi_thoigianchon.sql`

```sql
-- Chạy trên DB ELEARNING_DQ (1 lần, có thể chạy lại an toàn)
ALTER TABLE [dbo].[CTBaiThi] ADD [ThoiGianChon] DATETIME NULL
```

Script tự kiểm tra `IF NOT EXISTS` — chạy nhiều lần không lỗi.

---


## Single Device Login — 1 tài khoản chỉ 1 thiết bị đăng nhập

### Mục tiêu

- Tại một thời điểm chỉ cho phép **1 thiết bị** đăng nhập với cùng 1 tài khoản
- Thiết bị 2 đăng nhập → thiết bị 1 bị kick tự động ở request tiếp theo
- Trên màn hình thi (`STest`): phát hiện kick sau tối đa **15 giây** dù không có thao tác

---

### Cấu trúc Redis

```
Key:   user:session:{IDNV}
Value: sessionToken (GUID 32 ký tự)
TTL:   Sliding 360 phút (bằng thời gian sống cookie auth)
```

---

### Luồng hoạt động

```
Thiết bị 1 đăng nhập
  → sinh token "AAA"
  → lưu Redis["user:session:123"] = "AAA"
  → ghi claim NV_ST = "AAA" vào cookie

Thiết bị 2 đăng nhập cùng tài khoản
  → sinh token "BBB"
  → ghi đè Redis["user:session:123"] = "BBB"
  → cookie thiết bị 2 có NV_ST = "BBB"

Thiết bị 1 request tiếp theo (bất kỳ trang nào)
  → SessionGuardMiddleware đọc claim "AAA"
  → Redis trả về "BBB"
  → "AAA" ≠ "BBB" → SignOut → redirect /Login/Index?kicked=1
  → Hiện alert: "Tài khoản đã đăng nhập ở thiết bị khác"
```

---

### Phát hiện kick khi đang treo màn hình STest

Middleware chỉ check khi có HTTP request. Nếu thí sinh ngồi đọc đề mà không thao tác → không có request → không bị kick ngay.

**Giải pháp:** JS trên trang STest gọi `GET /STest/PingSession` mỗi **15 giây**.  
Middleware xử lý request này → nếu token mismatch → redirect login → JS phát hiện redirect → hiện overlay kick.

```
Thiết bị 2 đăng nhập → Redis token đổi

Thiết bị 1 (đang treo màn hình thi, không thao tác):
  → 15s ping → middleware detect mismatch → redirect /Login
  → fetch thấy response.redirected = true
  → Overlay "Phiên thi bị gián đoạn" xuất hiện
  → Đếm ngược 3 giây → redirect login
```

**Ngoài ping 15s**, response của `AutoSave` cũng được kiểm tra:  
Nếu thí sinh chọn đáp án mà lúc đó đã bị kick → AutoSave response là redirect → cũng trigger overlay ngay lập tức.

---

### Fallback khi Redis down

- Middleware bắt exception → cho request qua, không block
- Người dùng vẫn dùng được bình thường
- Tính năng single device tạm thời vô hiệu cho đến khi Redis phục hồi

---

### Các file đã thay đổi

| File | Thay đổi |
|------|----------|
| `Middleware/SessionGuardMiddleware.cs` | Mới — check token mỗi request |
| `Controllers/LoginController.cs` | Sinh token khi login, xóa token khi logout, inject IDistributedCache |
| `Controllers/STestController.cs` | Thêm `GET /STest/PingSession` |
| `Program.cs` | Đăng ký `UseMiddleware<SessionGuardMiddleware>()` |
| `Views/Login/Index.cshtml` | Hiện alert khi URL có `?kicked=1` |
| `Views/STest/Index.cshtml` | Overlay kicked, ping 15s, check AutoSave response |

---

### Lưu ý

- Cookie cũ (trước khi tính năng được bật) không có claim `NV_ST` → middleware **bỏ qua**, không kick — tránh gián đoạn người dùng hiện tại
- Refresh trang không bị kick vì JS cũ đã dừng trước khi trang mới load
- Logout xóa Redis key → tài khoản hoàn toàn sạch, không thiết bị nào dùng lại token cũ được

---

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
