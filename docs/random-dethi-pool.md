# Tính năng: Random đề thi chính thức (Pool đề)

## Mục tiêu
Đề thi chính thức (`IsCoCtdt = 0` → thi 1 lần) có thể cấu hình nhiều đề thi.
Mỗi thí sinh được **random 1 đề cố định** — cùng người luôn cùng đề dù reload nhiều lần.

---

## Các bước triển khai

### Bước 1 — SQL: Tạo bảng `LopHocDeThiPool`
**File:** `Migrations/add_lophoc_dethi_pool.sql`

```sql
CREATE TABLE LopHocDeThiPool (
    ID      INT IDENTITY(1,1) PRIMARY KEY,
    IDLH    INT NOT NULL,   -- FK → LopHoc
    IDDeThi INT NOT NULL,   -- FK → DeThi
    CONSTRAINT UQ_LopHocDeThiPool UNIQUE (IDLH, IDDeThi)
);
CREATE INDEX IX_LopHocDeThiPool_IDLH ON LopHocDeThiPool(IDLH);
```

**Cần chạy trực tiếp trên SQL Server trước khi deploy.**

---

### Bước 2 — EF Entity
**File mới:** `Data/Models/LopHocDeThiPool.cs`

```csharp
public class LopHocDeThiPool {
    public int Id { get; set; }
    public int Idlh { get; set; }
    public int IddeThi { get; set; }
}
```

---

### Bước 3 — EF Context
**File:** `Data/ELEARNINGEntities.cs`

- Thêm `DbSet<LopHocDeThiPool> LopHocDeThiPools`
- Thêm `modelBuilder.Entity<LopHocDeThiPool>` config sau block LopHoc

---

### Bước 4 — STestController (logic chính)
**File:** `Controllers/STestController.cs`

**4a. `LopHocDto` record** — thêm `List<int> DeThiPool`

**4b. `GetLopHocCachedAsync`** — load pool từ `LopHocDeThiPools`, dùng pool[0] cho `ThoiGianLamBai`

**4c. 2 helper mới:**
```csharp
// Danh sách IDDeThi hợp lệ cho invalidate check
GetValidDeThiIds(lh) → pool IDs hoặc {lh.IddeThi}

// Chọn đề deterministic: cùng IDNV → luôn cùng đề
SelectDeThiForUser(lh, idnv) → pool[IDNV * 2654435761 % pool.Count]
```

**4d. `Index`:**
- Check `lh.IddeThi == null && pool.Count == 0` mới redirect
- Invalidate dùng `GetValidDeThiIds` thay vì so sánh cứng
- `selectedDeThiId` = session.IDDeThi (nếu HIT) hoặc `SelectDeThiForUser` (MISS)
- `qKey = "exam:q:{LHID}:{selectedDeThiId}"` (tách cache theo đề)
- Load câu hỏi theo `selectedDeThiId` không phải `lh.IddeThi`
- Tạo session mới với `IDDeThi = selectedDeThiId`

**4e. `SubmitExamAjax`:**
- `qKey = "exam:q:{IDLH}:{IDDeThi}"` — khớp key đúng đề

---

### Bước 5 — ManageClassController
**File:** `Controllers/ManageClassController.cs`

Thêm 3 endpoints:
| Endpoint | Method | Chức năng |
|---|---|---|
| `GET /ManageClass/DeThiPool?idlh={id}` | GET | Hiện modal quản lý pool |
| `POST /ManageClass/ThemDeVaoPool` | POST | Thêm 1 đề vào pool |
| `POST /ManageClass/XoaDeKhoiPool` | POST | Xóa 1 đề khỏi pool |

Khi thêm/xóa đề: tự động `DEL lophoc:{IDLH}` Redis cache để load lại pool mới.

---

### Bước 6 — View pool (partial)
**File mới:** `Views/ManageClass/DeThiPool.cshtml`

Hiển thị:
- Danh sách đề đang trong pool + nút xóa
- Dropdown chọn đề để thêm (lọc theo NDID của lớp)

---

### Bước 7 — Index.cshtml
**File:** `Views/ManageClass/Index.cshtml`

Thêm nút <i class="fa fa-random"></i> màu tím bên cạnh nút Edit.
Chỉ hiện với user có quyền EDIT.

---

## Cách hoạt động

```
Admin tạo lớp thi (IsCoCtdt = 0 = thi 1 lần):
    LopHoc.IDLH = 100, IddeThi = 5 (đề mặc định cũ)

Admin vào Pool đề → Thêm đề 6, 7, 8
    LopHocDeThiPool: (100,6), (100,7), (100,8)
    → Cache lophoc:100 bị xóa, load lại có pool = [6,7,8]

Thí sinh A (IDNV=1001) vào thi lần đầu:
    selectedDeThiId = pool[(1001 * 2654435761) % 3] = đề 7
    exam:q:100:7 (cache) → load câu hỏi đề 7
    exam:session:1001:100 → IDDeThi = 7

Thí sinh A reload trang:
    session HIT → IDDeThi = 7 (không đổi đề)

Thí sinh B (IDNV=1002) vào thi:
    selectedDeThiId = pool[(1002 * 2654435761) % 3] = đề 8
    → đề khác A, bảo mật hơn

Thí sinh A nộp bài:
    SET NX exam:submitted:1001:100 → chặn nộp lần 2
    exam:q:100:7 → lấy đáp án đúng để tính điểm
```

---

## Backward compatible

- Lớp cũ không có pool → `DeThiPool = []` → dùng `lh.IddeThi` như cũ
- `lophoc` cache key không đổi, chỉ thêm field `DeThiPool` vào JSON
- `exam:q:{LHID}` cũ sẽ không được dùng nữa → tự expire sau 2h
- `exam:q:{LHID}:{IDDeThi}` là format mới

---

## Lưu ý vận hành

| Tình huống | Hành vi |
|---|---|
| Admin thêm đề mới vào pool | Cache `lophoc:{IDLH}` bị xóa, thí sinh CHƯA vào thi sẽ có thể nhận đề mới |
| Admin xóa đề khỏi pool | Thí sinh đang thi đề đó vẫn tiếp tục bình thường (session giữ IDDeThi) |
| Pool rỗng | Fallback về `lh.IddeThi` như cũ |
| Tất cả đề trong pool có câu hỏi khác nhau | Mỗi user có set câu hỏi riêng → bảo mật cao |
| Tất cả đề cần cùng `ThoiGianLamBai` | Không bắt buộc về code, nhưng nên đồng nhất về nghiệp vụ |
