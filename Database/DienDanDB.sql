CREATE DATABASE DienDan

use DienDan

CREATE TABLE LoaiND
(
	MaLoaiND INT IDENTITY(1,1) PRIMARY KEY,
	TenLoai NVARCHAR(20)
)

CREATE TABLE NguoiDung
(
	MaND INT IDENTITY(1,1) PRIMARY KEY,
	HoTen NVARCHAR(80),
	AnhDaiDien VARCHAR(50),
	AnhBia VARCHAR (50),
	Email VARCHAR(50),
	GioiTinh NVARCHAR(3),
	SDT VARCHAR(11),
	NgaySinh DATE,
	NgayThamGia DATE,
	TenDangNhap VARCHAR(15),
	MatKhau VARCHAR(60),
	MaLoaiND INT FOREIGN KEY (MaLoaiND) REFERENCES LoaiND(MaLoaiND),
	SoLanDNThatBai INT DEFAULT 0,
	LanDNThatBaiCuoi DATETIME NULL,
	KhoaDenKhi DATETIME NULL,
	ResetToken NVARCHAR(MAX) NULL, 
	TokenExpiry DATETIME NULL, 
	TrangThai BIT NOT NULL DEFAULT 1
)

CREATE TABLE LoaiCD
(
	MaLoai INT IDENTITY(1,1) PRIMARY KEY,
	TenLoai NVARCHAR(50)
)

CREATE TABLE ChuDe
(
	MaCD INT IDENTITY(1,1) PRIMARY KEY,
	TenCD NVARCHAR(50),
	MaLoai INT FOREIGN KEY (MaLoai) REFERENCES LoaiCD(MaLoai)
)

CREATE TABLE BaiViet
(
	MaBV INT IDENTITY(1,1) PRIMARY KEY,
	TieuDeBV NVARCHAR(60),
	NoiDung xml,
	NgayDang DATETIME,
	TrangThai NVARCHAR(20),
	MaCD INT FOREIGN KEY (MaCD) REFERENCES ChuDe(MaCD),
	MaND INT FOREIGN KEY (MaND) REFERENCES NguoiDung(MaND)
)

CREATE TABLE BinhLuan
(
	MaBL INT IDENTITY(1,1) PRIMARY KEY,
	IDCha INT,
	NoiDung xml,
	NgayGui DATETIME,
	TrangThai NVARCHAR(20),
	MaBV INT FOREIGN KEY (MaBV) REFERENCES BaiViet(MaBV),
	MaND INT FOREIGN KEY (MaND) REFERENCES NguoiDung(MaND)
)

CREATE TABLE GopY
(
	ID INT PRIMARY KEY IDENTITY(1,1),
	NoiDung xml,
	NgayGui DATETIME,
	TrangThai BIT,
	MaND INT FOREIGN KEY (MaND) REFERENCES NguoiDung(MaND)
)
CREATE TABLE LoaiTB
(
	MaLoaiTB INT IDENTITY(1,1) PRIMARY KEY,
	TenLoai NVARCHAR(MAX)
)
CREATE TABLE ThongBao
(
	MaTB INT IDENTITY(1,1) PRIMARY KEY,
	NoiDung xml,
	NgayTB DATETIME,
	MaND INT FOREIGN KEY (MaND) REFERENCES NguoiDung(MaND),
	MaLoaiTB INT FOREIGN KEY (MaLoaiTB) REFERENCES LoaiTB(MaLoaiTB),
	MaDoiTuong INT,
	TrangThai BIT
)

-- Dữ liệu cho bảng LoaiND
INSERT INTO LoaiND(TenLoai) VALUES
(N'admin'),
(N'user');

-- Dữ liệu cho bảng ThanhVien
INSERT INTO NguoiDung(HoTen, AnhDaiDien, AnhBia, Email, GioiTinh, SDT, NgaySinh, NgayThamGia, TenDangNhap, MatKhau, MaLoaiND) VALUES
--pass chung cho admin ad123456
(N'Nguyễn Văn A', N'avatar.jpg', N'default-bg.jpg','nva@gmail.com', N'Nam' ,'0912345678', '1980-05-15', GETDATE(), 'nguyenvana', '$2a$11$bJjUvKBItEjHIzcNh/jSce0efbG/gwU38VsI.IInXmuz7ZW3ZLJ3m', 1),
(N'Trần Thị B', N'avatar.jpg', N'default-bg.jpg','ttb@gmail.com', N'Nữ','0987654321', '1985-11-25', GETDATE(), 'tranthib', '$2a$11$bJjUvKBItEjHIzcNh/jSce0efbG/gwU38VsI.IInXmuz7ZW3ZLJ3m', 1),
(N'Lê Quốc Cường', N'avatar.jpg', N'default-bg.jpg', 'lqc@gmail.com', N'Nam', '0912233445', '1975-02-20', GETDATE(), 'lequocc', '$2a$11$bJjUvKBItEjHIzcNh/jSce0efbG/gwU38VsI.IInXmuz7ZW3ZLJ3m', 1),
(N'Nguyễn Thị Thanh', N'avatar.jpg', N'default-bg.jpg', 'ntt@gmail.com', N'Nữ', '0912345690', '1982-07-15', GETDATE(), 'nguyentt', '$2a$11$bJjUvKBItEjHIzcNh/jSce0efbG/gwU38VsI.IInXmuz7ZW3ZLJ3m', 1),
(N'Phạm Quang Huy', N'avatar.jpg', N'default-bg.jpg', 'pqh@gmail.com', N'Nam', '0923456781', '1979-03-19', GETDATE(), 'phamqh', '$2a$11$bJjUvKBItEjHIzcNh/jSce0efbG/gwU38VsI.IInXmuz7ZW3ZLJ3m', 1),
(N'Ngô Mỹ Dung', N'avatar.jpg', N'default-bg.jpg', 'nmd@gmail.com', N'Nữ', '0934567892', '1983-12-01', GETDATE(), 'ngomy', '$2a$11$bJjUvKBItEjHIzcNh/jSce0efbG/gwU38VsI.IInXmuz7ZW3ZLJ3m', 1),

--pass chung cho thành viên 12345678
(N'Lê Văn C', N'avatar.jpg', N'default-bg.jpg','lvc@gmail.com', N'Nam', '0911222333', '1999-03-21', '2023-01-01', 'levanc', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Phạm Thị D', N'avatar.jpg', N'default-bg.jpg','ptd@gmail.com', N'Nữ', '0922333444', '2000-08-10', '2023-02-15', 'phamthid', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Tạ Gia Bảo', N'avatar2.jpg', N'default-bg.jpg','baotg@gmail.com', N'Nam', '0909123456', '2003-01-01', '2023-04-22', 'banphuf29966', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Nguyễn Văn Phong', N'avatar.jpg', N'default-bg.jpg', 'nvp@gmail.com', N'Nam', '0931234567', '2001-06-05', '2023-03-10', 'nguyenphong', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Hoàng Thị Vân', N'avatar.jpg', N'default-bg.jpg', 'htv@gmail.com', N'Nữ', '0932345678', '1998-09-09', '2023-04-05', 'hoangtv', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Lê Minh Tuấn', N'avatar.jpg', N'default-bg.jpg', 'lmt@gmail.com', N'Nam', '0933456789', '1997-07-19', '2023-05-02', 'leminht', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Phạm Văn Hậu', N'avatar.jpg', N'default-bg.jpg', 'pvh@gmail.com', N'Nam', '0934567890', '2002-05-22', '2023-06-15', 'phamvh', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Võ Thị Hồng', N'avatar.jpg', N'default-bg.jpg', 'vth@gmail.com', N'Nữ', '0935678901', '2000-10-10', '2023-07-08', 'vothong', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Nguyễn Nhật Nam', N'avatar.jpg', N'default-bg.jpg', 'nnn@gmail.com', N'Nam', '0936789012', '2002-12-12', '2023-08-03', 'nguyennn', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2),
(N'Phạm Thuỳ Linh', N'avatar.jpg', N'default-bg.jpg', 'ptl@gmail.com', N'Nữ', '0937890123', '2003-01-01', '2023-09-14', 'phamlinh', '$2a$11$v/0O4f5Ya2.WljHWHZ9y5O5q6htDckM.P0mokifPOv8hRaUO.g9tu', 2);

-- Dữ liệu cho bảng LoaiCD
INSERT INTO LoaiCD (TenLoai) VALUES
(N'Ngôn ngữ lập trình'),
(N'Bảo mật và an ninh mạng'),
(N'Trí tuệ nhân tạo và Học máy'),
(N'Cơ sở dữ liệu và Hệ quản trị CSDL'),
(N'Phát triển phần mềm và Quản lý dự án'),
(N'Hệ thống nhúng và IoT');

-- Dữ liệu cho bảng ChuDe
INSERT INTO ChuDe (TenCD, MaLoai) VALUES
-- Chủ đề thuộc Loại Ngôn ngữ lập trình
(N'Lập trình Python', 1),
(N'Lập trình Java', 1),
(N'Lập trình C++', 1),
(N'Lập trình JavaScript', 1),

-- Chủ đề thuộc Loại Bảo mật và an ninh mạng
(N'Bảo mật hệ thống mạng', 2),
(N'An ninh mạng trong doanh nghiệp', 2),
(N'Tấn công mạng và cách phòng chống', 2),
(N'Phòng thủ mạng với Firewall', 2),

-- Chủ đề thuộc Loại Trí tuệ nhân tạo và Học máy
(N'Machine Learning cơ bản', 3),
(N'Deep Learning với TensorFlow', 3),
(N'Xử lý ngôn ngữ tự nhiên (NLP)', 3),
(N'Thị giác máy tính (Computer Vision)', 3),

-- Chủ đề thuộc Loại Cơ sở dữ liệu và Hệ quản trị CSDL
(N'Quản trị cơ sở dữ liệu SQL', 4),
(N'Cơ sở dữ liệu NoSQL', 4),
(N'Tối ưu hóa truy vấn SQL', 4),
(N'Thiết kế cơ sở dữ liệu', 4),

-- Chủ đề thuộc Loại Phát triển phần mềm và Quản lý dự án
(N'Phát triển phần mềm Agile', 5),
(N'Quản lý dự án Scrum', 5),
(N'Phần mềm quản lý dự án Jira', 5),
(N'Kiểm thử phần mềm', 5),

-- Chủ đề thuộc Loại Hệ thống nhúng và IoT
(N'Cảm biến trong IoT', 6),
(N'Hệ điều hành thời gian thực (RTOS)', 6),
(N'Giao thức truyền thông trong IoT', 6),
(N'Phát triển ứng dụng IoT với Arduino', 6);

-- Dữ liệu cho bảng BaiViet
INSERT INTO BaiViet (TieuDeBV, NoiDung, NgayDang, TrangThai, MaCD, MaND) VALUES
(N'Học lập trình Python cơ bản',N'<NoiDung>Bài viết về Python dành cho người mới bắt đầu</NoiDung>', '2023-09-01', N'Đã duyệt', 1, 7),
(N'Các phương pháp bảo mật mạng', N'<NoiDung>Những cách bảo vệ hệ thống mạng khỏi tấn công mạng</NoiDung>', N'2023-09-10', N'Đã duyệt', 5, 8),
(N'Giới thiệu về Machine Learning', N'<NoiDung>Bài viết về Machine Learning cơ bản</NoiDung>', '2023-09-15', N'Đã duyệt', 9, 7),
(N'Quản trị SQL Server', N'<NoiDung>Cách quản trị cơ sở dữ liệu bằng SQL Server</NoiDung>', '2023-09-18', N'Đã duyệt', 13, 8),
(N'Tối ưu hóa mã Python', N'<NoiDung>Các phương pháp tối ưu hóa mã Python</NoiDung>', '2023-09-20', N'Đã duyệt', 1, 9),
(N'Java cơ bản cho người mới', N'<NoiDung>Giới thiệu ngôn ngữ lập trình Java cơ bản</NoiDung>', '2023-09-25', N'Đã duyệt', 2, 10),
(N'Hệ thống bảo mật mạng', N'<NoiDung>Chiến lược bảo mật cho mạng doanh nghiệp</NoiDung>', '2023-09-30', N'Đã duyệt', 5, 13),
(N'Học C++ qua các ví dụ', N'<NoiDung>Ví dụ minh họa cho người học C++</NoiDung>', '2023-10-05', N'Đã duyệt', 3, 13),
(N'Các Nguyên Tắc Thiết Kế Cơ Sở Dữ Liệu', N'<NoiDung>Hướng dẫn thiết kế cơ sở dữ liệu hiệu quả</NoiDung>', '2023-10-07', N'Đã duyệt', 16, 14),
(N'Quản lý dự án Agile', N'<NoiDung>Áp dụng phương pháp Agile trong phát triển phần mềm</NoiDung>', '2023-10-10', N'Đã duyệt', 17, 15),
(N'Xử lý ngôn ngữ tự nhiên', N'<NoiDung>Tìm hiểu về xử lý ngôn ngữ tự nhiên</NoiDung>', '2023-10-15', N'Đã duyệt', 11, 16),
(N'Cảm biến trong IoT', N'<NoiDung>Các loại cảm biến sử dụng trong IoT</NoiDung>', '2023-10-20', N'Đã duyệt', 21, 12),
(N'Kỹ thuật phòng thủ mạng nâng cao', N'<NoiDung>Kỹ thuật phòng thủ mạng cho doanh nghiệp</NoiDung>', '2023-10-25', N'Đã duyệt', 7, 7),
(N'Kiểm thử phần mềm với JUnit', N'<NoiDung>Hướng dẫn kiểm thử phần mềm bằng JUnit</NoiDung>', '2023-10-28', N'Đã duyệt', 20, 8),
(N'Lập trình JavaScript cơ bản', N'<NoiDung>Những khái niệm cơ bản về JavaScript</NoiDung>', '2023-11-01', N'Đã duyệt', 4, 9),
(N'Quản trị NoSQL', N'<NoiDung>Hướng dẫn quản trị cơ sở dữ liệu NoSQL</NoiDung>', '2023-11-05', N'Đã duyệt', 14, 10),
(N'Trung tâm dữ liệu thời gian thực', N'<NoiDung>Xây dựng trung tâm dữ liệu hiệu quả</NoiDung>', '2023-11-08', N'Đã duyệt', 12, 12),
(N'Giới thiệu về TensorFlow', N'<NoiDung>Bài viết giới thiệu TensorFlow</NoiDung>', '2023-11-10', N'Đã duyệt', 10, 13),
(N'Lập trình Arduino cơ bản', N'<NoiDung>Hướng dẫn lập trình với Arduino cho người mới</NoiDung>', '2023-11-15', N'Đã duyệt', 24, 14),
(N'Kỹ thuật tối ưu SQL', N'<NoiDung>Các kỹ thuật tối ưu truy vấn SQL</NoiDung>', '2023-11-18', N'Đã duyệt', 15, 15),
(N'Phân tích dữ liệu với Python', N'<NoiDung>Những công cụ phân tích dữ liệu Python</NoiDung>', '2023-11-20', N'Đã duyệt', 1, 16),
(N'Tự động hóa kiểm thử phần mềm', N'<NoiDung>Cách sử dụng công cụ kiểm thử tự động</NoiDung>', '2023-11-22', N'Đã duyệt', 20, 11),
(N'An toàn hệ thống với Firewall', N'<NoiDung>Cấu hình và bảo vệ hệ thống với Firewall</NoiDung>', '2023-11-25', N'Đã duyệt', 8, 7),
(N'Học sâu cơ bản', N'<NoiDung>Những kiến thức cơ bản về học sâu</NoiDung>', '2023-11-28', N'Đã duyệt', 10, 8);

-- Dữ liệu cho bảng BinhLuan
INSERT INTO BinhLuan (IDCha, NoiDung, NgayGui, TrangThai, MaBV, MaND) VALUES
(NULL, N'<NoiDung>Bài viết rất hữu ích</NoiDung>', '2023-09-02', N'Hiển thị', 1, 8),
(NULL, N'<NoiDung>Tôi đã học được nhiều điều mới</NoiDung>', '2024-10-02', N'Hiển thị', 2, 7),
(NULL, N'<NoiDung>Tôi đồng ý với quan điểm của bạn</NoiDung>', '2023-09-03', N'Hiển thị', 1, 9),
(NULL, N'<NoiDung>Những kiến thức này rất hữu ích</NoiDung>', '2023-09-04', N'Hiển thị', 2, 10),
(NULL, N'<NoiDung>Tôi cần tìm hiểu thêm về chủ đề này</NoiDung>', '2023-09-05', N'Hiển thị', 3, 11),
(NULL, N'<NoiDung>Cảm ơn bạn vì thông tin hữu ích</NoiDung>', '2023-09-06', N'Hiển thị', 4, 13),
(NULL, N'<NoiDung>Rất bổ ích!</NoiDung>', '2023-09-07', N'Hiển thị', 1, 14),
(NULL, N'<NoiDung>Những điểm này cần bổ sung thêm</NoiDung>', '2023-09-08', N'Hiển thị', 2, 15),
(NULL, N'<NoiDung>Chờ đợi những phần tiếp theo</NoiDung>', '2023-09-09', N'Hiển thị', 3, 16),
(NULL, N'<NoiDung>Bài viết rất chi tiết và dễ hiểu</NoiDung>', '2023-09-10', N'Hiển thị', 4, 12),
(NULL, N'<NoiDung>Bài viết này đã giải đáp thắc mắc của tôi</NoiDung>', '2023-09-11', N'Hiển thị', 1, 8),
(NULL, N'<NoiDung>Học hỏi được nhiều điều qua bài viết</NoiDung>', '2023-09-12', N'Hiển thị', 2, 7),
(NULL, N'<NoiDung>Tôi rất thích cách diễn đạt của bạn</NoiDung>', '2023-09-13', N'Hiển thị', 3, 9),
(NULL, N'<NoiDung>Cần thêm nhiều ví dụ minh hoạ</NoiDung>', '2023-09-14', N'Hiển thị', 4, 10),
(NULL, N'<NoiDung>Chủ đề này rất hay</NoiDung>', '2023-09-15', N'Hiển thị', 1, 11),
(NULL, N'<NoiDung>Hy vọng có thêm nhiều bài viết về chủ đề này</NoiDung>', '2024-10-02', N'Hiển thị', 4, 13);

-- Dữ liệu cho bảng GopY
INSERT INTO GopY (NoiDung, NgayGui, TrangThai, MaND) VALUES
(N'<NoiDung>Giao diện trang web cần cải thiện</NoiDung>', '2023-09-05', 1, 1),
(N'<NoiDung>Tốc độ tải web cần được cải thiện</NoiDung>', '2023-09-12', 1, 1);

--Dữ liệu cho bảng loại thông báo
INSERT INTO LoaiTB (TenLoai)
VALUES 
(N'Thông báo hệ thống'),
(N'Thông báo bình luận'),
(N'Thông báo xóa bình luận'),
(N'Thông báo duyệt bài viết'),
(N'Thông báo xóa bài viết'),
(N'Thông báo từ chối bài viết'),
(N'Thông báo tố cáo');

-- Dữ liệu cho bảng ThongBao
INSERT INTO ThongBao (NoiDung, NgayTB, MaND, MaLoaiTB, MaDoiTuong, TrangThai)
VALUES
    (N'<NoiDung>Hệ thống sẽ bảo trì vào lúc 22h.</NoiDung>', GETDATE(), NULL, 1, NULL, 1),
    (N'<NoiDung>CHÀO MỪNG ĐẠI LỄ 30/4 - NGÀY GIẢI PHÓNG MIÊN NAM, THỐNG NHẤT ĐẤT NƯỚC.</NoiDung>', GETDATE(), NULL, 1, NULL, 1);

USE [DienDan]
GO
/****** Object:  StoredProcedure [dbo].[AutoBackup]    Script Date: 5/1/2025 11:09:46 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[AutoBackup]
AS
BEGIN
    DECLARE @filename NVARCHAR(200)
    SET @filename = 'D:\Backups\DienDanDB_' + 
        CONVERT(VARCHAR(10), GETDATE(), 120) + '.bak'

    BACKUP DATABASE DienDan
    TO DISK = @filename
    WITH INIT, FORMAT, MEDIANAME = 'DienDanBackup', NAME = 'Full Backup'
END