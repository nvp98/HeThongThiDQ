# Kiến trúc RabbitMQ — Hệ thống nộp bài thi

## Tổng quan

Khi thí sinh nộp bài, HTTP request trả về ngay lập tức sau khi tính điểm và publish message vào queue. Việc ghi DB diễn ra bất đồng bộ qua `ExamSubmitConsumer`.

```
Client ──► SubmitExamAjax ──► RabbitMQ Queue ──► ExamSubmitConsumer ──► SQL Server
                │                                                              │
          trả kết quả                                                    Redis SET
          ngay (~2ms)                                                  exam:result
```

---

## Luồng chi tiết

### 1. Publisher — STestController.SubmitExamAjax

```
POST /STest/SubmitExamAjax
        │
        ├─ [Redis SET NX] exam:submitted:IDNV:IDLH
        │       └─ Đã tồn tại → trả về lỗi "Đã hoàn thành bài thi"
        │
        ├─ [Redis INCR]   exam:lanthi:IDNV:IDLH   → lấy số lần thi
        │
        ├─ [Redis GET]    exam:q:IDLH              → load đáp án đúng
        │
        ├─ [Redis GET]    exam:session:IDNV:IDLH   → load timestamp từng câu
        │
        ├─ Tính điểm in-memory (không cần DB)
        │
        ├─ PublishAsync(ExamSubmitMessage)          → ~1–2ms
        │
        ├─ [Redis DEL]    exam:session:IDNV:IDLH   → dọn session
        │
        └─ return { success: true, score, redirectUrl: /WaitResult }
```

### 2. Queue — RabbitMQ

| Thuộc tính | Giá trị | Ý nghĩa |
|---|---|---|
| Tên queue | `exam.submit` | — |
| `durable` | `true` | Queue tồn tại sau khi restart RabbitMQ |
| `persistent` | `true` | Message lưu xuống disk, không mất khi crash |
| `prefetchCount` | `5` | Consumer nhận tối đa 5 message chưa ack |

### 3. Consumer — ExamSubmitConsumer (BackgroundService)

```
RabbitMQ Queue
        │
        │ prefetchCount = 5
        ▼
SemaphoreSlim(8, 8) ← giới hạn 8 SQL operations đồng thời
        │
        ▼
ProcessAsync(ExamSubmitMessage)
        │
        ├─ INSERT BaiThi (EF Core)
        │       └─ SaveChanges() → lấy IDbaiThi
        │
        ├─ SqlBulkCopy → INSERT CTBaiThi (N câu trả lời, 1 batch)
        │
        ├─ [Redis SET] exam:result:IDNV:IDLH = IDbaiThi  TTL 2h
        │
        ├─ OK  → BasicAck         (xóa khỏi queue)
        └─ Lỗi → BasicNack requeue=false  (→ dead-letter, không loop)
```

### 4. Client polling kết quả

```
Sau khi nhận redirectUrl → /STest/WaitResult
        │
        └─ Polling GET /STest/ResultReady?IDLH=...
                │
                └─ [Redis GET] exam:result:IDNV:IDLH
                        ├─ Có giá trị → redirect /EClassroom/ViewResult
                        └─ Chưa có    → chờ 2s, poll lại
```

---

## ExamSubmitMessage

```csharp
{
    MessageId,      // Guid — định danh duy nhất mỗi message
    IDLH,           // ID lớp học
    IDDeThi,        // ID đề thi
    IDND,           // ID nội dung
    IDNV,           // ID nhân viên (thí sinh)
    IDPhongBan,
    IDViTri,
    LanThi,         // Số lần thi (từ Redis INCR)
    ThoiGianSec,    // Thời gian làm bài (giây)
    TGBDLamBaiThi,  // Timestamp bắt đầu (ms)
    DiemSo,         // Điểm đã tính sẵn in-memory
    Answers: [
        { IDCH, DapAnDung, DapAnNv, Diem, ThoiGianChon }
    ]
}
```

---

## Cơ chế bảo vệ

| Cơ chế | Vị trí | Mục đích |
|---|---|---|
| `durable + persistent` | Queue & Message | Không mất bài thi khi restart |
| `Redis SET NX` | SubmitExamAjax | Chặn duplicate submit |
| `lock (_lock)` | RabbitMqPublisher | Thread-safe khi 1500+ request publish đồng thời |
| `SemaphoreSlim(8,8)` | ExamSubmitConsumer | Giới hạn 8 SQL write song song, tránh cạnh tranh connection pool |
| `prefetchCount = 5` | Consumer channel | Tránh race condition BasicAck trên shared channel |
| `BasicNack requeue=false` | ProcessAsync catch | Lỗi vào dead-letter, không loop vô hạn |
| `AutomaticRecoveryEnabled` | Publisher + Consumer | Tự reconnect khi mất kết nối RabbitMQ |

---

## So sánh trước / sau RabbitMQ

| | Trước (đồng bộ) | Sau (RabbitMQ async) |
|---|---|---|
| HTTP response time | 50–200ms (chờ DB) | ~2ms (publish xong trả về) |
| SQL concurrent writes | = số user nộp đồng thời | Cố định tối đa 8 |
| Nguy cơ mất dữ liệu | Thấp (DB transaction) | Thấp (persistent message) |
| SQL Server pressure ở 1500 VU | ~1500 concurrent writes | ~8 concurrent writes |
| Submit p95 (k6 test) | ~200–500ms (ước tính) | **15–38ms** (đo thực tế) |
