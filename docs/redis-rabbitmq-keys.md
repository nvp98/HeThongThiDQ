# Redis & RabbitMQ — Toàn bộ logic và key map

## Sơ đồ tổng quan

```
┌──────────────────────────────────────────────────────────────────┐
│                          REDIS                                   │
│                                                                  │
│  [Auth]          nv:lookup:{SDT}          user:session:{IDNV}   │
│  [Stats]         HPDQ:stats:logins:*                            │
│  [Online]        HPDQ:online:users        HPDQ:online:exams      │
│  [Exam]          lophoc:{LHID}            exam:q:{LHID}         │
│                  exam:session:{IDNV}:{IDLH}                      │
│  [Submit]        exam:submitted:{IDNV}:{IDLH}                    │
│                  exam:lanthi:{IDNV}:{IDLH}                       │
│                  exam:result:{IDNV}:{IDLH}                       │
│  [Dashboard]     HPDQ:trend:snapshots                            │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                        RABBITMQ                                  │
│                                                                  │
│  Queue: exam.submit  (durable, persistent)                       │
│  Publisher: RabbitMqPublisher  ──►  Consumer: ExamSubmitConsumer │
└──────────────────────────────────────────────────────────────────┘
```

---

## Redis — Danh sách key chi tiết

### 1. Auth & Session

| Key | Kiểu | TTL | Nơi ghi | Nơi đọc | Mục đích |
|---|---|---|---|---|---|
| `nv:lookup:{SoDienThoai}` | String (JSON NhanVien) | 10 phút | LoginController.LoginUser | LoginController.LoginUser | Cache thông tin nhân viên, tránh query DB khi login đồng thời |
| `user:session:{IDNV}` | String (Guid token) | 6 giờ sliding | LoginController.LoginUser | SessionGuardMiddleware | Xác thực session — nếu token trong Redis khác cookie → kick thiết bị cũ |

**Luồng:**
```
Login thành công
    → SET nv:lookup:{SDT}      ← NhanVien JSON, TTL 10m
    → SET user:session:{IDNV}  ← sessionToken (Guid), TTL 6h

Mỗi request tiếp theo (middleware)
    → GET user:session:{IDNV}
    → So sánh với NV_ST claim trong cookie
    → Khác nhau → SignOut (bị kick)

Đổi mật khẩu
    → DEL nv:lookup:{username}  ← buộc login lại lấy data mới

Logout
    → DEL user:session:{IDNV}
```

---

### 2. Thống kê đăng nhập

| Key | Kiểu | TTL | Nơi ghi | Nơi đọc | Mục đích |
|---|---|---|---|---|---|
| `HPDQ:stats:logins:total` | String (counter) | Không hết | LoginController.LoginUser | AdminDashboard | Tổng lượt đăng nhập toàn thời gian |
| `HPDQ:stats:logins:daily:{yyyyMMdd}` | String (counter) | 90 ngày | LoginController.LoginUser | AdminDashboard | Lượt đăng nhập theo ngày |
| `HPDQ:stats:logins:hourly:{yyyyMMdd}:{HH}` | String (counter) | 7 ngày | LoginController.LoginUser | AdminDashboard | Lượt đăng nhập theo giờ (biểu đồ) |

**Luồng:**
```
Login thành công
    → INCR HPDQ:stats:logins:total
    → INCR HPDQ:stats:logins:daily:{date}    EXPIRE 90d
    → INCR HPDQ:stats:logins:hourly:{date}:{hour}  EXPIRE 7d
```

---

### 3. Theo dõi online

| Key | Kiểu | TTL | Score | Nơi ghi | Nơi đọc |
|---|---|---|---|---|---|
| `HPDQ:online:users` | Sorted Set | Không hết (tự dọn) | timestamp ms | SessionGuardMiddleware, Logout | AdminDashboard |
| `HPDQ:online:exams` | Sorted Set | Không hết (tự dọn) | timestamp ms | STestController.Index, PingSession, SubmitExamAjax | AdminDashboard |

**Luồng:**
```
Mỗi request authenticated (middleware)
    → ZADD HPDQ:online:users {nowMs} {IDNV}    ← cập nhật timestamp

Vào trang thi / PingSession mỗi 8 câu
    → ZADD HPDQ:online:exams {nowMs} "{IDNV}:{IDLH}"

Nộp bài / Logout
    → ZREM HPDQ:online:exams "{IDNV}:{IDLH}"
    → ZREM HPDQ:online:users {IDNV}

Dashboard (mỗi lần load)
    → ZREMRANGEBYSCORE ... 0 {staleMs}   ← dọn entry > 5 phút không ping
    → ZCARD HPDQ:online:users            → OnlineCount
    → ZCARD HPDQ:online:exams            → ExamCount
```

---

### 4. Dữ liệu thi (Exam)

| Key | Kiểu | TTL | Nơi ghi | Nơi đọc | Mục đích |
|---|---|---|---|---|---|
| `lophoc:{LHID}` | String (JSON LopHocDto) | 10 phút | STestController.GetLopHocCachedAsync | STestController.GetLopHocCachedAsync | Cache thông tin lớp học + thời gian thi |
| `exam:q:{LHID}` | String (JSON List\<TestValidation\>) | 2 giờ | STestController.Index | STestController.Index, SubmitExamAjax | Cache toàn bộ câu hỏi + đáp án đúng của đề thi |
| `exam:session:{IDNV}:{IDLH}` | String (JSON ExamSession) | 30 phút sliding | STestController.Index, AutoSave | STestController.Index, AutoSave, SubmitExamAjax | Lưu trạng thái bài thi: thứ tự câu hỏi, câu đã chọn, timestamp |

**ExamSession chứa:**
```json
{
  "IDDeThi", "IDND", "IDLH", "IDNV",
  "TotalTimeSec", "StartTimestamp", "EndTimestamp", "SavedAt",
  "QuestionOrder": [IDCH1, IDCH2, ...],
  "Answers": { "IDCH": { "AnswerId": null, "ChosenAt": null } }
}
```

**Luồng:**
```
Vào trang thi (GET /STest/Index)
    → GET exam:session:{IDNV}:{LHID}
        HIT  → restore thứ tự câu hỏi + câu đã chọn, EXPIRE refresh
        MISS → GET exam:q:{LHID} (hoặc query DB) → shuffle → tạo session mới
             → SET exam:session:{IDNV}:{LHID}  TTL 30m sliding

Chọn đáp án (POST /STest/AutoSave)
    → GET exam:session  → cập nhật Answers[IDCH]  → SET lại

Nộp bài (POST /STest/SubmitExamAjax)
    → GET exam:q:{LHID}        ← đáp án đúng để tính điểm
    → GET exam:session          ← ThoiGianChon từng câu
    → DEL exam:session          ← dọn sau khi nộp
```

---

### 5. Submit bài thi

| Key | Kiểu | TTL | Nơi ghi | Nơi đọc | Mục đích |
|---|---|---|---|---|---|
| `exam:submitted:{IDNV}:{IDLH}` | String ("1") | 1 ngày | STestController.SubmitExamAjax | STestController.SubmitExamAjax | SET NX — chặn duplicate submit atomic |
| `exam:lanthi:{IDNV}:{IDLH}` | String (counter) | 30 ngày | STestController.SubmitExamAjax | STestController.SubmitExamAjax | Đếm số lần thi, INCR mỗi lần nộp |
| `exam:result:{IDNV}:{IDLH}` | String (IDbaiThi) | 2 giờ | ExamSubmitConsumer.ProcessAsync | STestController.ResultReady | Báo hiệu DB đã ghi xong, client polling nhận |

**Luồng chặn duplicate:**
```
SET NX exam:submitted:{IDNV}:{LHID} "1" EX 86400
    → isNew = true  → cho phép nộp
    → isNew = false → trả lỗi "Đã hoàn thành bài thi"
    (chỉ áp dụng khi IsCoCtdt = 0)
```

**Luồng đếm lần thi:**
```
EXISTS exam:lanthi:{IDNV}:{LHID}
    → false → COUNT từ DB → SET NX
    → true  → bỏ qua
INCR exam:lanthi  → lanthi (dùng làm LanThi trong BaiThi record)
```

---

### 6. Dashboard trend

| Key | Kiểu | TTL | Nơi ghi | Nơi đọc | Mục đích |
|---|---|---|---|---|---|
| `HPDQ:trend:snapshots` | List (JSON TrendPoint) | Không hết | AdminDashboard | AdminDashboard | Lịch sử online/exam count, capped 120 điểm (~60 phút) |

```
Mỗi lần load dashboard
    → RPUSH HPDQ:trend:snapshots {T, O, E}
    → LTRIM -120 -1      ← giữ 120 điểm gần nhất
    → LRANGE -60 -1      ← lấy 60 điểm để vẽ chart
```

---

## RabbitMQ

### Queue: `exam.submit`

| Thuộc tính | Giá trị |
|---|---|
| durable | true — tồn tại sau restart |
| autoDelete | false |
| Message persistent | true — ghi xuống disk |
| prefetchCount | 5 — consumer nhận tối đa 5 msg chưa ack |

### Publisher — `RabbitMqPublisher`

- Singleton, dùng chung 1 channel
- `lock (_lock)` đảm bảo thread-safe khi nhiều HTTP request publish đồng thời
- `AutomaticRecoveryEnabled = true` — tự reconnect khi mất kết nối

```csharp
// Gọi tại SubmitExamAjax sau khi tính điểm xong
await _queue.PublishAsync(new ExamSubmitMessage { ... });
// → HTTP trả về ngay, không chờ DB
```

### Consumer — `ExamSubmitConsumer` (BackgroundService)

- `SemaphoreSlim(8, 8)` — tối đa 8 SQL write đồng thời
- `BasicAck` khi xử lý thành công
- `BasicNack requeue=false` khi lỗi → vào dead-letter, không loop vô hạn

```
ProcessAsync(msg):
    1. INSERT BaiThi (EF)  → SaveChanges() → lấy IDbaiThi
    2. SqlBulkCopy CTBaiThi (N câu, 1 batch)
    3. SET exam:result:{IDNV}:{IDLH} = IDbaiThi  TTL 2h
```

### Tại sao dùng `SemaphoreSlim(8)` không phải cao hơn?

SQL Server connection pool `Max Pool Size = 1000` chia cho 3 app instance. Consumer chạy trong cùng process với web → cần nhường connection cho web requests. 8 SQL writes đồng thời là đủ để drain queue nhanh mà không cạnh tranh pool với các request login/load đề đang chạy.

---

## Fallback khi Redis/RabbitMQ down

Toàn bộ Redis operation được bọc trong `try { } catch { }` → app tiếp tục hoạt động, fallback về DB:

| Thành phần down | Hành vi |
|---|---|
| Redis down (login) | Bỏ qua cache → query DB trực tiếp |
| Redis down (exam session) | Tạo session mới mỗi lần load trang |
| Redis down (submit guard) | `catch {}` → cho phép submit (có thể duplicate) |
| RabbitMQ down | Publisher throw → HTTP 500, bài thi không được ghi |

> **Lưu ý:** RabbitMQ down là điểm dừng duy nhất không có fallback — nếu publish thất bại, bài thi mất. Cần monitor queue và alert khi RabbitMQ unavailable.
