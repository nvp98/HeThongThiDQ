using HeThongThiDQ.Data;
using HeThongThiDQ.Data.Models;
using HeThongThiDQ.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Data;
using System.Text;
using System.Text.Json;

namespace HeThongThiDQ.Services;

public class ExamSubmitConsumer : BackgroundService
{
    private readonly IServiceScopeFactory              _scopeFactory;
    private readonly IConfiguration                    _config;
    private readonly ILogger<ExamSubmitConsumer>       _logger;

    public ExamSubmitConsumer(
        IServiceScopeFactory        scopeFactory,
        IConfiguration              config,
        ILogger<ExamSubmitConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config       = config;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Đợi app khởi động xong
        await Task.Delay(4000, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExamConsumer] mất kết nối RabbitMQ, thử lại sau 5s");
                await Task.Delay(5000, ct);
            }
        }
    }

    // Giới hạn số SQL operations đồng thời từ consumer, tránh cạnh tranh connection pool với web requests
    private readonly SemaphoreSlim _sqlSem = new(5, 5);

    private async Task ConsumeAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_config.GetConnectionString("RabbitMQ")!),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval  = TimeSpan.FromSeconds(5),
            DispatchConsumersAsync   = true,
        };

        using var conn    = factory.CreateConnection("consumer");
        using var channel = conn.CreateModel();

        channel.QueueDeclare(RabbitMqPublisher.QueueName,
            durable: true, exclusive: false, autoDelete: false);
        // prefetchCount=5: mỗi lần lấy 5 message, tránh race condition BasicAck trên shared channel
        channel.BasicQos(0, prefetchCount: 5, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            await _sqlSem.WaitAsync(ct);
            try
            {
                var msg = JsonSerializer.Deserialize<ExamSubmitMessage>(
                    Encoding.UTF8.GetString(ea.Body.ToArray()));

                if (msg != null)
                    await ProcessAsync(msg);

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExamConsumer] lỗi xử lý message {Id}", ea.BasicProperties.MessageId);
                // requeue: false → vào dead-letter nếu đã cấu hình, không loop vô hạn
                channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
            finally
            {
                _sqlSem.Release();
            }
        };

        channel.BasicConsume(RabbitMqPublisher.QueueName, autoAck: false, consumer);

        // Giữ consumer chạy đến khi bị cancel
        await Task.Delay(Timeout.Infinite, ct);
    }

    private async Task ProcessAsync(ExamSubmitMessage msg)
    {
        using var scope = _scopeFactory.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<ELEARNINGEntities>();
        var mux = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();

        // 1. Insert BaiThi qua EF, lấy IdbaiThi
        var baiThi = new BaiThi
        {
            Idlh        = msg.IDLH,
            IddeThi     = msg.IDDeThi,
            Idnd        = msg.IDND,
            Idnv        = msg.IDNV,
            IdphongBan  = msg.IDPhongBan,
            IdviTri     = msg.IDViTri,
            DiemSo      = msg.DiemSo,
            NgayThi     = DateOnly.FromDateTime(DateTime.Now),
            TinhTrang   = true,
            LanThi      = msg.LanThi,
            ThoiGianThi = msg.ThoiGianSec > 0 ? msg.ThoiGianSec : null,
            GioBatDau   = msg.TGBDLamBaiThi > 0
                ? DateTimeOffset.FromUnixTimeMilliseconds(msg.TGBDLamBaiThi).LocalDateTime
                : null,
            GioKetThuc  = DateTime.Now,
        };
        db.BaiThis.Add(baiThi);
        await db.SaveChangesAsync();

        // 2. Bulk insert CTBaiThi bằng SqlBulkCopy
        var dt = new DataTable();
        dt.Columns.Add("IDBaiThi",     typeof(int));
        dt.Columns.Add("IDCauHoi",     typeof(int));
        dt.Columns.Add("IDDapAnDung",  typeof(int));
        dt.Columns.Add("IDDApAnNV",    typeof(int));
        dt.Columns.Add("Diem",         typeof(double));
        dt.Columns.Add("ThoiGianChon", typeof(DateTime));

        foreach (var a in msg.Answers)
        {
            var row = dt.NewRow();
            row["IDBaiThi"]     = baiThi.IdbaiThi;
            row["IDCauHoi"]     = (object?)a.IDCH      ?? DBNull.Value;
            row["IDDapAnDung"]  = (object?)a.DapAnDung ?? DBNull.Value;
            row["IDDApAnNV"]    = (object?)a.DapAnNv   ?? DBNull.Value;
            row["Diem"]         = a.Diem;
            row["ThoiGianChon"] = (object?)a.ThoiGianChon ?? DBNull.Value;
            dt.Rows.Add(row);
        }

        var connStr = _config.GetConnectionString("DbConnectionString")!;
        using var bulk = new SqlBulkCopy(connStr);
        bulk.DestinationTableName = "CTBaiThi";
        bulk.ColumnMappings.Add("IDBaiThi",     "IDBaiThi");
        bulk.ColumnMappings.Add("IDCauHoi",     "IDCauHoi");
        bulk.ColumnMappings.Add("IDDapAnDung",  "IDDapAnDung");
        bulk.ColumnMappings.Add("IDDApAnNV",    "IDDApAnNV");
        bulk.ColumnMappings.Add("Diem",         "Diem");
        bulk.ColumnMappings.Add("ThoiGianChon", "ThoiGianChon");
        await bulk.WriteToServerAsync(dt);

        // 3. Lưu IdbaiThi vào Redis để WaitResult page nhận
        var rdb = mux.GetDatabase();
        await rdb.StringSetAsync(
            $"exam:result:{msg.IDNV}:{msg.IDLH}",
            baiThi.IdbaiThi.ToString(),
            TimeSpan.FromHours(2));

        _logger.LogInformation(
            "[ExamConsumer] IDNV={IDNV} IDLH={IDLH} → IDBaiThi={ID} DiemSo={Diem}",
            msg.IDNV, msg.IDLH, baiThi.IdbaiThi, msg.DiemSo);
    }
}
