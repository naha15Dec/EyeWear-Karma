-- ============================================================
-- MIGRATION: Thêm tính năng Đánh giá sản phẩm + Trả hàng
-- Database: BanMatKinh_V2
-- ============================================================

USE [BanMatKinh_V2]
GO

-- ============================================================
-- 1. Mở rộng CHECK constraint TrangThai trên DonHang
--    Thêm status 9 (Yêu cầu trả hàng) và 10 (Đã trả hàng)
-- ============================================================

ALTER TABLE [dbo].[DonHang] DROP CONSTRAINT [CK_DonHang_TrangThai]
GO

ALTER TABLE [dbo].[DonHang] ADD CONSTRAINT [CK_DonHang_TrangThai]
    CHECK ([TrangThai] IN (1,2,3,4,5,6,7,8,9,10))
GO

-- ============================================================
-- 2. Bảng DanhGiaSanPham (Đánh giá sản phẩm)
--    - Mỗi ChiTietDonHang chỉ được đánh giá 1 lần
--    - Chỉ được đánh giá khi đơn hàng ở trạng thái DELIVERED (6)
--    - Admin duyệt trước khi hiển thị
-- ============================================================

CREATE TABLE [dbo].[DanhGiaSanPham] (
    [DanhGiaId]        INT            IDENTITY(1,1) NOT NULL,
    [ChiTietDonHangId] INT            NOT NULL,
    [KhachHangId]      INT            NOT NULL,
    [SanPhamId]        INT            NOT NULL,
    [SoSao]            TINYINT        NOT NULL,
    [NoiDung]          NVARCHAR(1000) NULL,
    [TrangThai]        INT            NOT NULL DEFAULT(1),
    -- 1 = Chờ duyệt, 2 = Đã duyệt, 3 = Từ chối
    [DuyetBoiId]       INT            NULL,
    [NgayDuyet]        DATETIME       NULL,
    [LyDoTuChoi]       NVARCHAR(500)  NULL,
    [CreatedAt]        DATETIME       NOT NULL DEFAULT(GETDATE()),
    [UpdatedAt]        DATETIME       NULL,

    CONSTRAINT [PK_DanhGiaSanPham] PRIMARY KEY CLUSTERED ([DanhGiaId] ASC),
    CONSTRAINT [UQ_DanhGia_ChiTietDonHang] UNIQUE ([ChiTietDonHangId]),
    CONSTRAINT [CK_DanhGia_SoSao] CHECK ([SoSao] BETWEEN 1 AND 5),
    CONSTRAINT [CK_DanhGia_TrangThai] CHECK ([TrangThai] IN (1, 2, 3))
)
GO

ALTER TABLE [dbo].[DanhGiaSanPham]
    ADD CONSTRAINT [FK_DanhGia_ChiTietDonHang]
    FOREIGN KEY ([ChiTietDonHangId]) REFERENCES [dbo].[ChiTietDonHang]([ChiTietDonHangId])
GO

ALTER TABLE [dbo].[DanhGiaSanPham]
    ADD CONSTRAINT [FK_DanhGia_KhachHang]
    FOREIGN KEY ([KhachHangId]) REFERENCES [dbo].[KhachHang]([KhachHangId])
GO

ALTER TABLE [dbo].[DanhGiaSanPham]
    ADD CONSTRAINT [FK_DanhGia_SanPham]
    FOREIGN KEY ([SanPhamId]) REFERENCES [dbo].[SanPham]([SanPhamId])
GO

ALTER TABLE [dbo].[DanhGiaSanPham]
    ADD CONSTRAINT [FK_DanhGia_DuyetBoi]
    FOREIGN KEY ([DuyetBoiId]) REFERENCES [dbo].[TaiKhoan]([TaiKhoanId])
GO

-- ============================================================
-- 3. Bảng YeuCauTraHang (Yêu cầu trả hàng)
-- ============================================================

CREATE TABLE [dbo].[YeuCauTraHang] (
    [YeuCauId]        INT           IDENTITY(1,1) NOT NULL,
    [MaYeuCau]        VARCHAR(25)   NOT NULL,
    [DonHangId]       INT           NOT NULL,
    [KhachHangId]     INT           NOT NULL,
    [LyDo]            NVARCHAR(500) NOT NULL,
    [GhiChuKhachHang] NVARCHAR(500) NULL,
    [TrangThai]       INT           NOT NULL DEFAULT(1),
    -- 1 = Chờ duyệt, 2 = Đã duyệt (chờ shipper lấy),
    -- 3 = Shipper đang lấy, 4 = Đã nhận hàng về, 5 = Từ chối
    [ShipperId]       INT           NULL,
    [DuyetBoiId]      INT           NULL,
    [NgayYeuCau]      DATETIME      NOT NULL DEFAULT(GETDATE()),
    [NgayDuyet]       DATETIME      NULL,
    [NgayShipperLay]  DATETIME      NULL,
    [NgayNhanVe]      DATETIME      NULL,
    [GhiChuAdmin]     NVARCHAR(500) NULL,
    [CreatedAt]       DATETIME      NOT NULL DEFAULT(GETDATE()),
    [UpdatedAt]       DATETIME      NULL,

    CONSTRAINT [PK_YeuCauTraHang] PRIMARY KEY CLUSTERED ([YeuCauId] ASC),
    CONSTRAINT [UQ_YeuCau_Ma] UNIQUE ([MaYeuCau]),
    CONSTRAINT [UQ_YeuCau_DonHang] UNIQUE ([DonHangId]),
    -- Mỗi đơn hàng chỉ có 1 yêu cầu trả hàng active
    CONSTRAINT [CK_YeuCau_TrangThai] CHECK ([TrangThai] IN (1, 2, 3, 4, 5))
)
GO

ALTER TABLE [dbo].[YeuCauTraHang]
    ADD CONSTRAINT [FK_YeuCau_DonHang]
    FOREIGN KEY ([DonHangId]) REFERENCES [dbo].[DonHang]([DonHangId])
GO

ALTER TABLE [dbo].[YeuCauTraHang]
    ADD CONSTRAINT [FK_YeuCau_KhachHang]
    FOREIGN KEY ([KhachHangId]) REFERENCES [dbo].[KhachHang]([KhachHangId])
GO

ALTER TABLE [dbo].[YeuCauTraHang]
    ADD CONSTRAINT [FK_YeuCau_Shipper]
    FOREIGN KEY ([ShipperId]) REFERENCES [dbo].[TaiKhoan]([TaiKhoanId])
GO

ALTER TABLE [dbo].[YeuCauTraHang]
    ADD CONSTRAINT [FK_YeuCau_DuyetBoi]
    FOREIGN KEY ([DuyetBoiId]) REFERENCES [dbo].[TaiKhoan]([TaiKhoanId])
GO

-- ============================================================
-- 4. Bảng ChiTietTraHang (Chi tiết sản phẩm trong yêu cầu trả)
-- ============================================================

CREATE TABLE [dbo].[ChiTietTraHang] (
    [ChiTietTraHangId] INT           IDENTITY(1,1) NOT NULL,
    [YeuCauId]         INT           NOT NULL,
    [ChiTietDonHangId] INT           NOT NULL,
    [SoLuongTra]       INT           NOT NULL,
    [LyDoChiTiet]      NVARCHAR(255) NULL,

    CONSTRAINT [PK_ChiTietTraHang] PRIMARY KEY CLUSTERED ([ChiTietTraHangId] ASC),
    CONSTRAINT [CK_ChiTiet_SoLuongTra] CHECK ([SoLuongTra] > 0)
)
GO

ALTER TABLE [dbo].[ChiTietTraHang]
    ADD CONSTRAINT [FK_ChiTietTra_YeuCau]
    FOREIGN KEY ([YeuCauId]) REFERENCES [dbo].[YeuCauTraHang]([YeuCauId])
GO

ALTER TABLE [dbo].[ChiTietTraHang]
    ADD CONSTRAINT [FK_ChiTietTra_ChiTietDonHang]
    FOREIGN KEY ([ChiTietDonHangId]) REFERENCES [dbo].[ChiTietDonHang]([ChiTietDonHangId])
GO

PRINT 'Migration hoàn tất. Đã tạo: DanhGiaSanPham, YeuCauTraHang, ChiTietTraHang.'
PRINT 'Đã mở rộng CHECK constraint TrangThai trên DonHang (thêm 9, 10).'
GO
