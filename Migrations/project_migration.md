---
name: project-migration
description: "Tiến độ migration từ HeThongThiTD (ASP.NET MVC5) sang HeThongThiDQ (ASP.NET Core 8 MVC), danh sách file đã hoàn thành và còn lại"
metadata: 
  node_type: memory
  type: project
  originSessionId: 7c687e10-43f0-4190-b0da-9de3367c4b22
---

# Migration HeThongThiTD → HeThongThiDQ

**Source:** `E:/Software/25.QDAY/HeThongThiTD/E-Learning/`
**Target:** `E:/Software/25.QDAY/HeThongThiDQ/`

**Why:** Nâng cấp từ .NET 4.7.2 MVC5 lên .NET 8 Core MVC, giữ nguyên toàn bộ logic nghiệp vụ.

**Scope restriction (CRITICAL):** Chỉ chuyển đổi các controller/view NGOÀI các thư mục: `DinhBien`, `KCCD`, `KNL`, `QTHD`. 4 module này không dùng đến, bỏ qua hoàn toàn.

---

## CHECKLIST TỔNG THỂ (cập nhật 2026-07-28) — **HOÀN THÀNH 100%**

| Hạng mục | Trạng thái |
|---|---|
| Controllers (11 cái trong scope) | ✅ Hoàn thành |
| Views EClassroom (3 views) | ✅ Hoàn thành |
| Views Notification (3 views) | ✅ Hoàn thành |
| Views Account (5 views) | ✅ Hoàn thành |
| Views ManageClass (3 views) | ✅ Hoàn thành |
| Views ManageETContent (3 views) | ✅ Hoàn thành |
| Views ManageTestExam (7 views) | ✅ Hoàn thành |
| Views Question (4 views) | ✅ Hoàn thành |
| Views Statistic (1 view) | ✅ Hoàn thành |
| Views STest (1 view) | ✅ Hoàn thành |
| Views SHistory (1 view) | ✅ Hoàn thành |
| App_Data templates | ✅ Hoàn thành |
| Bug fixes (6 bugs) | ✅ Hoàn thành |
| ConfirmEStudyController + Views (4 views) | ✅ Hoàn thành |

---

## CONTROLLERS ĐÃ HOÀN THÀNH (12/12)

| Controller | Ghi chú quan trọng |
|---|---|
| `HomeController` | GetPermisionCN, đăng ký Scoped |
| `AccountController` | Login, Excel import, reset pass |
| `EClassroomController` | Thêm BaiThi pre-compute (không tính inline trong view) |
| `NotificationController` | CRUD thông báo |
| `ManageClassController` | Quản lý lớp học; ExportToExcel dùng `_env.ContentRootPath` |
| `ManageETContentController` | File upload dùng `_env.WebRootPath` (đã fix Directory.GetCurrentDirectory) |
| `ManageTestExamController` | ViewQuestion action có ViewBag.TenDe/NoiDung |
| `QuestionController` | DownloadExcel dùng `_env.ContentRootPath`; Upload dùng `_env.WebRootPath` |
| `StatisticController` | Báo cáo thống kê, 4 AJAX endpoints (GetLVDT, GetNDDTBP, GetKQHT, GetKQDT) |
| `STestController` | Trang thi: ViewBag.ThoiGianThi/IDNV/IDLH; Confirm tính điểm |
| `SHistoryController` | PartialView; thêm pre-compute KetQuaThi/DiemText từ BaiThis |
| `ConfirmEStudyController` | Index(id=IDLH), Create, Delete, ImportExcel, GetNhanVien, ExportToExcel, TestView; EF Core trực tiếp (không stored proc); skip PDF export |

---

## VIEWS ĐÃ HOÀN THÀNH (36/36)

### EClassroom (3)
- `Views/EClassroom/Index.cshtml` — `List<EClassroomValidation>`, 4 section, JS localStorage
- `Views/EClassroom/HistoryTest.cshtml` — `IPagedList<HistoryTestView>`
- `Views/EClassroom/ViewResult.cshtml` — ViewBag.DiemThi/IDLH/IDBaiThi/IDNV

### Notification (3)
- `Views/Notification/Index.cshtml` — `IPagedList<NotificationView>`, modal Create/Edit
- `Views/Notification/Create.cshtml` — TinyMCE + triggerSave on submit
- `Views/Notification/Edit.cshtml` — TinyMCE + triggerSave, SelectList TinhTrang

### Account (5)
- `Views/Account/Index.cshtml` — DataTables AJAX `/Account/GetData`
- `Views/Account/Create.cshtml` — Excel upload form
- `Views/Account/Edit.cshtml` — Email, SoDienThoai, IDQuyen (ViewBag.PList)
- `Views/Account/Resetpass.cshtml` — HoTen readonly, MatKhau
- `Views/Account/ResetListPass.cshtml` — Textarea MaNV, Check → /FPermision/CheckLSNV

### ManageClass (3)
- `Views/ManageClass/Index.cshtml` — `IPagedList<ManageClassValidation>`, filter dropdowns
- `Views/ManageClass/Create.cshtml` — Dynamic DeThi AJAX, IsThiNhieuLan, file upload
- `Views/ManageClass/Edit.cshtml` — Pre-populated, ViewBag.TGBDLH/TGKTLH datetime-local

### ManageETContent (3)
- `Views/ManageETContent/Index.cshtml` — `IPagedList`, IDLVDT/CTLVDTID filter + chosen
- `Views/ManageETContent/Create.cshtml` — BP/LVDT dropdowns, dynamic CTLVDT AJAX, TinyMCE
- `Views/ManageETContent/Edit.cshtml` — ViewBag.CTLVList, hiện FileDinhKem hiện tại

### ManageTestExam (7)
- `Views/ManageTestExam/Index.cshtml` — `IPagedList<ManageTestExamValidation>`, IDND filter
- `Views/ManageTestExam/Create.cshtml` — ViewBag.BDList
- `Views/ManageTestExam/Edit.cshtml` — Hidden IDDeThi, ViewBag.BDList
- `Views/ManageTestExam/Question.cshtml` — `List<ManageQuestionValidation>`, ViewBag.TenDe/NoiDung/IDDeThi
- `Views/ManageTestExam/AddQuestion.cshtml` — CauHoiDeThiValidation, ViewBag.CHList/IDND
- `Views/ManageTestExam/XuLyCauHoi.cshtml` — LoginValidation, MaNV textarea, hidden IDDethi
- `Views/ManageTestExam/ViewQuestion.cshtml` — `List<ManageQuestionValidation>`, hiển thị câu hỏi theo đề thi

### Question (4)
- `Views/Question/Index.cshtml` — `IPagedList<ManageQuestionValidation>`, IDND (chosen) + IDDaoCH filter, modal Import/Create
- `Views/Question/Create.cshtml` — Partial (modal), TinyMCE + triggerSave, ViewBag.DSList cho đáp án đúng
- `Views/Question/Edit.cshtml` — Partial (modal), TinyMCE + triggerSave, ViewBag.IDDADung
- `Views/Question/ImportExcel.cshtml` — Partial (modal), file upload + download template

### Statistic (1)
- `Views/Statistic/Index.cshtml` — `StatisticValidation`, 4 Highcharts AJAX (pie LVDT, column NDDT/BP, column KQHT, drilldown KQDT)

### STest (1)
- `Views/STest/Index.cshtml` — `List<TestValidation>`, timer localStorage, nav buttons, shuffle đáp án nếu IsDao, auto submit khi hết giờ

### SHistory (1)
- `Views/SHistory/Index.cshtml` — `IPagedList<ConfirmEStudyValidation>`, hiển thị KetQuaThi/DiemText được tính sẵn trong controller

### ConfirmEStudy (4)
- `Views/ConfirmEStudy/Index.cshtml` — `IPagedList<ConfirmEStudyValidation>`, filter PB+search, buttons Import/Export/Add (ẩn nếu lớp đã kết thúc), pre-compute KetQuaThi/SoLanThi/DiemText
- `Views/ConfirmEStudy/Create.cshtml` — Partial (modal), PBID chosen → AJAX GetNhanVien → populate NVID
- `Views/ConfirmEStudy/ImportExcel.cshtml` — Partial (modal), file upload excel
- `Views/ConfirmEStudy/TestView.cshtml` — `TestViewModel`, header bài thi, bảng câu hỏi tô màu đáp án đúng/sai

---

## MODELS MỚI/CẬP NHẬT

- `Models/TestViewModel.cs` — **TẠO MỚI**: IDBaiThi, IDLH, MaNV, HoTen, DiemSo, NgayThi... + `List<ManageQuestionValidation> CauHois`
- `Models/ConfirmEStudyValidation.cs` — Thêm: `KetQuaThi`, `DiemText`, `SoLanThi`, `IDBaiThi`, `TongCongTy`, `CongTyCapC2`, `DonViToChucC4`, `Email`, `DienThoai`, `NgaySinhNV`
- `Models/ManageTestExamValidation.cs` — Thêm `DapAnHV` vào `ManageQuestionValidation` (cần cho TestView màu đáp án)
- `App_Data/BM_XDSHT.xlsx` — Copy từ source

## KHÔNG CÒN VIỆC GÌ CÒN LẠI ✅

---

## BUG ĐÃ SỬA (2026-07-28)

| # | Triệu chứng | Nguyên nhân | Fix |
|---|---|---|---|
| 1 | Tìm kiếm không hoạt động | `chosen.js` không load → JS exception → btn-search handler không đăng ký | Thêm `chosen.css` + `chosen.jquery.js` vào `_Layout.cshtml` |
| 2 | TinyMCE content không lưu | TinyMCE dùng iframe, không sync lại textarea khi submit | Thêm `tinymce.triggerSave()` trong submit handler (`Notification/Create`, `Notification/Edit`) |
| 3 | ManageETContent/Create - MaND luôn rỗng | `<input readonly>` thiếu `value="@ViewBag.MaND"` | Thêm `value` attribute |
| 4 | AJAX dropdown không populate | ASP.NET Core JSON mặc định camelCase → `IDDeThi` thành `iDDeThi` → JS property name không khớp | `Program.cs`: `PropertyNamingPolicy = null` (PascalCase) |
| 5 | GetLVChiTiet không trả data | Controller trả `Idctlvdt/TenCtlvdt` nhưng JS dùng `IDCTLVDT/TenCTLVDT` | Explicit naming trong anonymous object |
| 6 | Template download không hoạt động | `App_Data` chưa tạo; `Directory.GetCurrentDirectory()` không tin cậy trong Core 8 | Tạo `App_Data`, copy file, thêm vào `.csproj`; dùng `_env.ContentRootPath` / `_env.WebRootPath` |

---

## LƯU Ý KỸ THUẬT QUAN TRỌNG

### Path / File
- `Directory.GetCurrentDirectory()` → KHÔNG dùng trong Core 8. Inject `IWebHostEnvironment _env`:
  - App_Data (templates): `_env.ContentRootPath`
  - wwwroot (uploads): `_env.WebRootPath`
- App_Data phải được khai báo trong `.csproj` với `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`

### JSON / AJAX
- `AddControllersWithViews().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null)` — bắt buộc để giữ PascalCase cho AJAX responses

### View chuyển đổi
- `Html.BeginForm()` → `<form asp-action="..." asp-controller="..." method="post">`
- `Html.EditorFor/TextBoxFor/TextAreaFor` → `<input asp-for="...">` / `<textarea asp-for="...">`
- `Html.DropDownListFor` → `<select asp-for="..." asp-items="(SelectList)ViewBag.X">`
- `Html.CheckBoxFor` → `<input asp-for="..." type="checkbox">`
- `Html.ValidationMessageFor` → `<span asp-validation-for="..." class="text-danger">`
- `Html.HiddenFor` → `<input asp-for="..." type="hidden">`
- `Html.DisplayFor(m => m[i].X)` → `@Model[i].X` trực tiếp
- `Html.Raw(TempData["msg"])` → giữ nguyên

### Paging
- `PagedList.IPagedList<>` → `IPagedList<>` với `@using X.PagedList`
- `@using PagedList.Mvc` → `@using X.PagedList.Mvc.Core`

### Routing / Query
- `Request.QueryString["key"]` → `Request.Query["key"]`
- `@Url.RequestContext.RouteData.Values["id"]` → dùng `ViewBag` (controller cung cấp)

### Model / Property
- `DapAnĐung` (tên có ký tự tiếng Việt) → `DapAnDung`
- Inline DB query trong view (`new ELEARNINGEntities()`) → chuyển lên controller, pass qua ViewBag hoặc enriched model
- CONSTKEY available globally qua `_ViewImports.cshtml` (`@using HeThongThiDQ.Common`)

### TinyMCE
- Luôn thêm `$(document).on('submit', 'form', function() { tinymce.triggerSave(); })` cho các form có TinyMCE
- `tinymce.remove('.myTextarea')` trước khi load partial view vào modal

### chosen.js
- Phải load `chosen.css` + `chosen.jquery.js` trong `_Layout.cshtml` (global), không load per-page
