-- Thêm cột CCCD vào bảng NhanVien
-- DB: ELEARNING_DQ
-- Chạy 1 lần duy nhất

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'NhanVien' AND COLUMN_NAME = 'CCCD'
)
BEGIN
    ALTER TABLE NhanVien ADD CCCD NVARCHAR(20) NULL;
    PRINT 'Đã thêm cột CCCD vào bảng NhanVien';
END
ELSE
BEGIN
    PRINT 'Cột CCCD đã tồn tại, bỏ qua';
END
