# Luồng Nộp Bài Thi — Kiến Trúc Async (RabbitMQ + Redis)

## Tổng quan

HTTP handler trả kết quả về client **ngay lập tức** (~5–50ms), việc ghi database được đẩy sang consumer chạy nền qua RabbitMQ.

---

## Sơ đồ luồng

```
BROWSER                  ASP.NET Core (IIS)              REDIS
───────                  ──────────────────              ─────
   │
   │── POST /STest/SubmitExamAjax ──▶│
   │                                  │── GET lophoc:{LHID} ──────────▶│
   │                                  │◀─ LopHocDto (cache, TTL 10m) ──│
   │                                  │
   │                                  │── SET NX exam:submitted:{IDNV}:{IDLH} ▶│
   │                                  │◀─ OK → tiếp tục / FAIL → báo lỗi ──────│
   │                                  │
   │                                  │── GET exam:q:{LHID} ──────────▶│
   │                                  │◀─ danh sách câu hỏi (TTL 2h) ──│
   │                                  │
   │                                  │  [Tính điểm trong RAM]
   │                                  │  [Xóa exam session Redis]
   │                                  │
   │                                  │                    RABBITMQ
   │                                  │                    ────────
   │                                  │── Publish(ExamSubmitMessage) ──▶│
   │                                  │                                  │ queue: exam.submit
   │◀─ {success, score,  ─────────────│                                  │ durable, persistent
   │    redirectUrl: /WaitResult}      │                            ══════╪══════
   │                             (~5–50ms)                               │
   │                                                        CONSUMER (BackgroundService)
   │                                                               │
   │── GET /STest/WaitResult ─────────▶│                          │◀── Dequeue
   │◀─ Hiện điểm ngay + spinner ───────│                          │
   │                                   │                          │── INSERT BaiThi (EF Core)
   │                                   │                          │       ↓ IDBaiThi
   │── poll /STest/ResultReady (1s) ──▶│                          │── SqlBulkCopy CTBaiThi
   │                                   │── GET exam:result:… ────▶│
   │                                   │◀─ null (chưa xong) ──────│
   │   [tiếp tục poll, tối đa 30s]     │                          │── SET exam:result:{IDNV}:{IDLH}
   │                                   │                          │       = IDBaiThi (TTL 2h)
   │── poll /STest/ResultReady (1s) ──▶│
   │                                   │── GET exam:result:… ────▶│
   │                                   │◀─ IDBaiThi (đã có!) ─────│
   │◀─ {ready:true, redirectUrl} ──────│
   │
   │── redirect /STest/ViewResult?id=IDBaiThi ──▶│── SELECT BaiThi+CTBaiThi ──▶ SQL SERVER
   │◀─ Trang kết quả chi tiết ───────────────────│◀─────────────────────────────
```

---

## Vai trò từng thành phần

| Thành phần | Vai trò |
|---|---|
| **Redis** | Cache câu hỏi (`exam:q:{LHID}`, TTL 2h), cache LopHoc (`lophoc:{LHID}`, TTL 10m), chặn duplicate submit (`SET NX exam:submitted:{IDNV}:{IDLH}`), lưu kết quả IDBaiThi (`exam:result:{IDNV}:{IDLH}`, TTL 2h) |
| **RabbitMQ** | Buffer bất đồng bộ giữa HTTP handler và SQL — nhận message ngay, xếp hàng chờ consumer xử lý, không làm block HTTP request |
| **Consumer** | `BackgroundService` chạy nền; nhận message, INSERT `BaiThi` (EF Core), `SqlBulkCopy` `CTBaiThi`, SET Redis result sau khi ghi xong |
| **SQL Server** | Chỉ nhận ghi từ consumer (không từ HTTP) → không bị connection pool bùng nổ khi nhiều user submit đồng thời |
| **WaitResult** | Trang trung gian hiển thị điểm ngay, polling `/ResultReady` mỗi 1s (tối đa 30s) cho đến khi consumer ghi xong |

---

## Thứ tự xử lý

```
[HTTP Handler]  →  Redis (check duplicate + lấy cache)
                →  Tính điểm trong RAM
                →  RabbitMQ Publish
                →  Trả về {success, score} ngay (~5–50ms)

[Consumer]      →  SQL INSERT BaiThi  →  lấy IDBaiThi
                →  SqlBulkCopy CTBaiThi
                →  Redis SET exam:result = IDBaiThi

[Client]        →  Hiển thị điểm ngay trên WaitResult
                →  Polling /ResultReady mỗi 1s
                →  Khi Redis có result → redirect ViewResult
```

---

## Các file liên quan

| File | Mô tả |
|---|---|
| `Controllers/STestController.cs` | HTTP handler: `SubmitExamAjax`, `WaitResult`, `ResultReady` |
| `Services/RabbitMqPublisher.cs` | Singleton publisher, kết nối RabbitMQ, publish message |
| `Services/ExamSubmitConsumer.cs` | BackgroundService consumer, ghi DB + set Redis result |
| `Models/ExamSubmitMessage.cs` | DTO message truyền qua RabbitMQ |
| `Services/IExamQueuePublisher.cs` | Interface publisher |
| `Views/STest/WaitResult.cshtml` | Trang polling kết quả |
| `appsettings.json` | Connection string RabbitMQ: `amqp://guest:guest@localhost:5672` |

---

## Cấu hình RabbitMQ queue

```csharp
channel.QueueDeclare(
    queue:      "exam.submit",
    durable:    true,       // queue tồn tại sau khi restart RabbitMQ
    exclusive:  false,
    autoDelete: false,
    arguments:  null        // thêm x-dead-letter-exchange nếu cần DLX
);

channel.BasicQos(
    prefetchSize:  0,
    prefetchCount: 5,       // consumer xử lý tối đa 5 message đồng thời
    global:        false
);
```

---

## Lưu ý khi scale

- **Nhiều IIS instance**: publisher và consumer chạy trên mỗi instance — queue `exam.submit` được chia đều tự động bởi RabbitMQ (competing consumers).
- **Duplicate submit**: Redis `SET NX` kiểm tra atomic trước khi publish, đảm bảo mỗi user chỉ nộp được 1 lần cho 1 lớp học.
- **Consumer chết giữa chừng**: message chưa ACK sẽ được RabbitMQ requeue tự động — nên đảm bảo `INSERT BaiThi` idempotent hoặc thêm DLX để xử lý message lỗi.
