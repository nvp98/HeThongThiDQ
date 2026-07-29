---
name: performance-optimization
description: "Tối ưu hiệu năng HeThongThiDQ (Core 8): async/await, N+1 fixes, caching — danh sách file đã xong và còn pending"
metadata: 
  node_type: memory
  type: project
  originSessionId: 7c687e10-43f0-4190-b0da-9de3367c4b22
---

# Tối ưu hiệu năng HeThongThiDQ

**Mục tiêu:** async/await xuyên suốt, fix N+1 queries, caching sidebar permission.

**Why:** Source MVC5 gốc dùng blocking sync I/O và load toàn bảng rồi filter C#, gây thread pool starvation và DB overhead cao.

**How to apply:** Khi tiếp tục tối ưu, đọc danh sách pending bên dưới; áp dụng 3 pattern chính (async, AsNoTracking, SQL-side aggregation/filter).

---

## PATTERN CHUNG ĐÃ ÁP DỤNG

| Pattern | Cách làm |
|---|---|
| Async | Tất cả public action → `async Task<IActionResult>`, dùng `ToListAsync / FirstOrDefaultAsync / AnyAsync / CountAsync / SumAsync / SaveChangesAsync` |
| Read-only | Thêm `.AsNoTracking()` cho mọi query chỉ đọc |
| SQL aggregation | Thay `.ToList()` + C# `.Count()/.Sum()` → `CountAsync() / SumAsync() / GroupBy().Select(g => new { Count = g.Count() }).ToListAsync()` |
| Filter pushdown | Đưa filter (role, search, FK) vào LINQ WHERE thay vì load hết rồi lọc C# |
| N+1 fix | Collect keys → `Where(Contains).ToDictionaryAsync()` trước loop → dùng dict trong loop → single `SaveChangesAsync()` sau loop |

---

## FILES ĐÃ HOÀN THÀNH (11/15)

### Program.cs ✅
- Thêm Response Compression (Brotli + Gzip), `app.UseResponseCompression()` TRƯỚC `UseStaticFiles()`
- EF Core retry: `EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)`
- `AddMemoryCache()`

### HomeController.cs ✅
- `GetPermisionCN` + `GetPermisionControll` → `async Task<List<string>>`
- Tất cả caller phải `await` — cascade sang mọi admin controller

### _Layout.cshtml ✅
- Permission sidebar query chạy **mỗi request** (sync, DB hit/page) → **IMemoryCache 5 phút per role**
- Cache key: `perm_ctrl_{idQuyen}`
- `GetOrCreate<T>()` trả `T?` → thêm `?? new List<string>()` tránh null

### StatisticController.cs ✅ *(tối ưu lớn nhất)*
- `Index()`: thay `XnhocTaps.ToList()` → `CountAsync()` + `CountAsync(predicate)` từng stat
- `GetKQHT()`: 3 `CountAsync()` SQL thay vì 3 C# `.Count()` sau `ToList()`
- `GetKQDT()`: `BaiThis.GroupBy(x => new { x.IdphongBan, x.TinhTrang }).Select(g => new { Count = g.Count() }).ToListAsync()`
- `GetLVDT()`, `GetNDDTBP()`: tương tự GroupBy SQL-side

### STestController.cs ✅
- Fix **duplicate LopHoc query**: load `lh` + `dethi` trước → dùng ID trong query CauHoiDeThi chính
- `Confirm()`: thay `.ToList().ForEach()` → `FirstOrDefaultAsync()` + direct update + `SaveChangesAsync()`

### EClassroomController.cs ✅
- Full async + `AsNoTracking()` trên baiThis, ctBaiThis, cauHoiDeThis

### SHistoryController.cs ✅
- `Index()` async + `AsNoTracking()` trên BaiThis query

### NotificationController.cs ✅
- Full async + `AsNoTracking()` trên `Index()` và `Details()`

### ManageTestExamController.cs ✅ *(N+1 fix)*
- `XuLyCauHoi POST` + `CheckCauHoi`: thay N queries (1/mã Excel) → `.Where(x => nvs.Contains(x.MaCh)).ToDictionaryAsync(x => x.MaCh!)` → dùng dict trong loop → single `SaveChangesAsync()`
- `Index()`: filter GV (`c.Gvid == id`) và IDND pushdown vào SQL WHERE

### QuestionController.cs ✅
- `Index()`: thay load toàn bộ CauHoi → push tất cả filter (role=GV, IDND, IsDao) vào LINQ WHERE → SQL

### ManageETContentController.cs ✅
- Full async + `AsNoTracking()`
- SLLH: thay N queries → `LopHocs.GroupBy(l => l.Ndid).Select(g => new { Count = g.Count() }).ToListAsync()` → lookup dict

---

## FILES CÒN PENDING (4/15)

### ManageClassController.cs ⏳
**Vấn đề N+1 trong `ImportHocVien`:**
```
for each row:
  IsHVAvailable(maLH, maNV)  // join query — 1 DB hit/row
  CheckMaNV(maNV)             // Any query — 1 DB hit/row  
  NhanViens.FirstOrDefault()  // select — 1 DB hit/row
```
**Fix cần làm:**
1. Parse tất cả maNV từ Excel trước loop
2. Pre-load: `_db.NhanViens.Where(x => maNVList.Contains(x.MaNv)).ToDictionaryAsync(x => x.MaNv!)`
3. Pre-load existing: `_db.XnhocTaps.Where(h => h.Lhid == lhid).Select(h => h.Nvid).ToHashSetAsync()`  
   *(tránh join với LopHoc, dùng lhid trực tiếp)*
4. Loop dùng dict + hashset, không query DB
5. Single `SaveChangesAsync()` sau loop

**Các fix khác:**
- `Index()`: full async, push filter IDPB / V_GV vào SQL, IDLH/IDND filter vào SQL
- `Create/Edit/Delete`: async + `AsNoTracking()` trên read-only queries

### AccountController.cs ⏳
**Vấn đề:**
- `Create POST` (Excel import): `GetOrCreateViTri()` + `GetOrCreatePhongBan()` gọi `SaveChanges()` mỗi khi tạo mới bản ghi tham chiếu (có thể chấp nhận), NHƯNG có outer `SaveChanges()` per iteration
- **Fix:** Bỏ outer `SaveChanges()` trong loop; thêm single `await _db.SaveChangesAsync()` sau loop
- `ResetListPass`: `NhanViens.FirstOrDefault(x => x.MaNv == item)` gọi per item → **fix:** `.Where(x => nvs.Contains(x.MaNv)).ToDictionaryAsync(x => x.MaNv!)`
- Full async conversion

### ConfirmEStudyController.cs ⏳
**Vấn đề N+1 trong `ImportExcel`:**
```
for each row:
  NhanViens.FirstOrDefault(x => x.MaNv.ToLower() == maNV.ToLower())  // 1 hit/row
  XnhocTaps.Any(h => h.Lhid == id && h.Nvid == nv.Id)               // 1 hit/row
```
**Fix cần làm:**
1. Pre-load: `_db.NhanViens.ToDictionaryAsync(x => x.MaNv!.ToLower())`
2. Pre-load existing: `_db.XnhocTaps.Where(h => h.Lhid == id).Select(h => h.Nvid).ToHashSetAsync()`
3. Loop dùng dict + hashset
4. `SaveChanges()` đã nằm ngoài loop ✓ — chỉ đổi thành `SaveChangesAsync()`
- Full async conversion

---

## LƯU Ý KỸ THUẬT

- **EF Core parallel queries bị cấm:** Không dùng `Task.WhenAll()` với nhiều EF Core queries trên cùng 1 Scoped DbContext → phải `await` tuần tự
- **IMemoryCache.GetOrCreate<T>()** trả `T?` → luôn thêm `?? default` hoặc `?? new List<>()`
- **ToHashSet từ async:** EF Core không có `ToHashSetAsync()` built-in → dùng `.ToListAsync()` rồi `.ToHashSet()`, hoặc `(await query.ToListAsync()).ToHashSet()`
