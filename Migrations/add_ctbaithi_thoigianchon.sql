-- Migration: Thêm cột ThoiGianChon vào bảng CTBaiThi
-- Mục đích: Lưu thời điểm thí sinh chọn từng đáp án (Unix timestamp → DateTime)
--           Được ghi khi nộp bài, lấy từ Redis session
-- DB: ELEARNING_DQ

USE [ELEARNING_DQ]
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CTBaiThi' AND COLUMN_NAME = 'ThoiGianChon'
)
BEGIN
    ALTER TABLE [dbo].[CTBaiThi]
    ADD [ThoiGianChon] DATETIME NULL;

    PRINT 'Da them cot ThoiGianChon vao bang CTBaiThi';
END
ELSE
BEGIN
    PRINT 'Cot ThoiGianChon da ton tai, bo qua.';
END
GO
