-- Bước 1: Tạo bảng pool đề thi cho lớp học
-- Mục đích: Cho phép 1 lớp học có nhiều đề thi, mỗi thí sinh được random 1 đề cố định
-- Chạy script này trực tiếp trên SQL Server

CREATE TABLE LopHocDeThiPool (
    ID      INT IDENTITY(1,1) PRIMARY KEY,
    IDLH    INT NOT NULL,
    IDDeThi INT NOT NULL,
    CONSTRAINT UQ_LopHocDeThiPool UNIQUE (IDLH, IDDeThi),
    CONSTRAINT FK_LopHocDeThiPool_LopHoc FOREIGN KEY (IDLH)    REFERENCES LopHoc(IDLH),
    CONSTRAINT FK_LopHocDeThiPool_DeThi  FOREIGN KEY (IDDeThi) REFERENCES DeThi(IDDeThi)
);

-- Tạo index để query theo IDLH nhanh
CREATE INDEX IX_LopHocDeThiPool_IDLH ON LopHocDeThiPool(IDLH);
