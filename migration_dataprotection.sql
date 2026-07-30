-- Tạo bảng lưu DataProtection keys cho ASP.NET Core
-- Chạy 1 lần trên database ELEARNING_DQ

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DataProtectionKeys')
BEGIN
    CREATE TABLE [dbo].[DataProtectionKeys] (
        [Id]           INT            NOT NULL IDENTITY(1,1),
        [FriendlyName] NVARCHAR(MAX)  NULL,
        [Xml]          NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY CLUSTERED ([Id])
    );
    PRINT 'Table DataProtectionKeys created.';
END
ELSE
BEGIN
    PRINT 'Table DataProtectionKeys already exists.';
END
