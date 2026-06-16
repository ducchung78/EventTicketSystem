CREATE TABLE [AIConfigs] (
    [Id] int NOT NULL IDENTITY,
    [AutoRefundThresholdMinutes] int NOT NULL,
    [MinTrainingSamples] int NOT NULL,
    [Iterations] int NOT NULL,
    [L1Regularization] real NOT NULL,
    [L2Regularization] real NOT NULL,
    [LastTrainedAt] datetime2 NULL,
    [LastModelMAE] float NULL,
    [LastModelRMSE] float NULL,
    [LastModelR2] float NULL,
    CONSTRAINT [PK_AIConfigs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [Ho] nvarchar(max) NOT NULL,
    [Ten] nvarchar(max) NOT NULL,
    [NgayTao] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ContactMessages] (
    [Id] int NOT NULL IDENTITY,
    [HoTen] nvarchar(150) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [Subject] nvarchar(200) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Coupons] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NULL,
    [DiscountPercent] decimal(5,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [MinOrderValue] decimal(18,2) NOT NULL,
    [MaxUses] int NOT NULL,
    [UsedCount] int NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Coupons] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Events] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [Venue] nvarchar(300) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [ImageUrl] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [IsHot] bit NOT NULL,
    [IsSpecial] bit NOT NULL,
    [HasSeatMap] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ChatMessages] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [Response] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatMessages_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [CustomerName] nvarchar(100) NOT NULL,
    [CustomerEmail] nvarchar(200) NOT NULL,
    [CustomerPhone] nvarchar(20) NULL,
    [OrderDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [OriginalAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [CouponId] int NULL,
    [ConfirmationCode] nvarchar(max) NOT NULL,
    [ApplicationUserId] nvarchar(450) NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_AspNetUsers_ApplicationUserId] FOREIGN KEY ([ApplicationUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Orders_Coupons_CouponId] FOREIGN KEY ([CouponId]) REFERENCES [Coupons] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [EventImages] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [ImagePath] nvarchar(500) NOT NULL,
    [Caption] nvarchar(200) NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_EventImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventImages_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PredictionLogs] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [PredictedAt] datetime2 NOT NULL,
    [PredictedTicketsSold] real NOT NULL,
    [PredictedRevenue] decimal(18,2) NOT NULL,
    [ActualTicketsSold] int NULL,
    [ActualRevenue] decimal(18,2) NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_PredictionLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PredictionLogs_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [TicketTypes] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [TotalQuantity] int NOT NULL,
    [SoldQuantity] int NOT NULL,
    CONSTRAINT [PK_TicketTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TicketTypes_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RefundRequests] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [UserId] nvarchar(450) NULL,
    [Email] nvarchar(200) NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Status] nvarchar(max) NOT NULL,
    [AutoProcessed] bit NOT NULL,
    [AIDecisionReason] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ProcessedAt] datetime2 NULL,
    [ProcessedBy] nvarchar(450) NULL,
    [AdminNote] nvarchar(1000) NULL,
    CONSTRAINT [PK_RefundRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefundRequests_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RefundRequests_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Seats] (
    [Id] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [TicketTypeId] int NULL,
    [Zone] nvarchar(50) NOT NULL,
    [RowLabel] nvarchar(10) NOT NULL,
    [SeatNumber] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [SeatType] nvarchar(max) NOT NULL DEFAULT N'Normal',
    [GridRow] int NOT NULL,
    [GridCol] int NOT NULL,
    [ReservedUntil] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Seats] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Seats_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Seats_TicketTypes_TicketTypeId] FOREIGN KEY ([TicketTypeId]) REFERENCES [TicketTypes] ([Id])
);
GO


CREATE TABLE [OrderItems] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [TicketTypeId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [SeatId] int NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Seats_SeatId] FOREIGN KEY ([SeatId]) REFERENCES [Seats] ([Id]),
    CONSTRAINT [FK_OrderItems_TicketTypes_TicketTypeId] FOREIGN KEY ([TicketTypeId]) REFERENCES [TicketTypes] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AutoRefundThresholdMinutes', N'Iterations', N'L1Regularization', N'L2Regularization', N'LastModelMAE', N'LastModelR2', N'LastModelRMSE', N'LastTrainedAt', N'MinTrainingSamples') AND [object_id] = OBJECT_ID(N'[AIConfigs]'))
    SET IDENTITY_INSERT [AIConfigs] ON;
INSERT INTO [AIConfigs] ([Id], [AutoRefundThresholdMinutes], [Iterations], [L1Regularization], [L2Regularization], [LastModelMAE], [LastModelR2], [LastModelRMSE], [LastTrainedAt], [MinTrainingSamples])
VALUES (1, 60, 100, CAST(0 AS real), CAST(0.1 AS real), NULL, NULL, NULL, NULL, 60);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AutoRefundThresholdMinutes', N'Iterations', N'L1Regularization', N'L2Regularization', N'LastModelMAE', N'LastModelR2', N'LastModelRMSE', N'LastTrainedAt', N'MinTrainingSamples') AND [object_id] = OBJECT_ID(N'[AIConfigs]'))
    SET IDENTITY_INSERT [AIConfigs] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'DiscountAmount', N'DiscountPercent', N'ExpiryDate', N'IsActive', N'MaxUses', N'MinOrderValue', N'UsedCount') AND [object_id] = OBJECT_ID(N'[Coupons]'))
    SET IDENTITY_INSERT [Coupons] ON;
INSERT INTO [Coupons] ([Id], [Code], [Description], [DiscountAmount], [DiscountPercent], [ExpiryDate], [IsActive], [MaxUses], [MinOrderValue], [UsedCount])
VALUES (1, N'WELCOME10', N'Giảm 10% cho đơn hàng đầu tiên', 0.0, 10.0, '2027-12-31T23:59:59.0000000Z', CAST(1 AS bit), 100, 50000.0, 0),
(2, N'SUMMER50K', N'Giảm 50,000đ cho đơn từ 200,000đ', 50000.0, 0.0, '2027-12-31T23:59:59.0000000Z', CAST(1 AS bit), 50, 200000.0, 0),
(3, N'VIP20', N'Giảm 20% dành cho thành viên VIP', 0.0, 20.0, '2027-12-31T23:59:59.0000000Z', CAST(1 AS bit), 30, 100000.0, 0),
(4, N'TICKET100K', N'Giảm 100,000đ cho đơn từ 500,000đ', 100000.0, 0.0, '2027-12-31T23:59:59.0000000Z', CAST(1 AS bit), 20, 500000.0, 0),
(5, N'FREESHIP', N'Giảm 5% không giới hạn đơn tối thiểu', 0.0, 5.0, '2027-12-31T23:59:59.0000000Z', CAST(1 AS bit), 200, 0.0, 0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'Description', N'DiscountAmount', N'DiscountPercent', N'ExpiryDate', N'IsActive', N'MaxUses', N'MinOrderValue', N'UsedCount') AND [object_id] = OBJECT_ID(N'[Coupons]'))
    SET IDENTITY_INSERT [Coupons] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'EndDate', N'HasSeatMap', N'ImageUrl', N'IsActive', N'IsHot', N'IsSpecial', N'StartDate', N'Title', N'Venue') AND [object_id] = OBJECT_ID(N'[Events]'))
    SET IDENTITY_INSERT [Events] ON;
INSERT INTO [Events] ([Id], [Category], [CreatedAt], [Description], [EndDate], [HasSeatMap], [ImageUrl], [IsActive], [IsHot], [IsSpecial], [StartDate], [Title], [Venue])
VALUES (1, N'Âm nhạc', '2026-01-01T00:00:00.0000000Z', N'Đêm âm nhạc rock đầy kịch tính với sự tham gia của các ban nhạc nổi tiếng trong và ngoài nước. Một trải nghiệm âm nhạc không thể bỏ qua!', '2026-07-15T23:00:00.0000000Z', CAST(0 AS bit), N'https://images.unsplash.com/photo-1540039155733-5bb30b53aa14?w=800', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), '2026-07-15T18:00:00.0000000Z', N'Lễ Hội Âm Nhạc Rock 2026', N'Nhà Thi Đấu Phú Thọ, TP. Hồ Chí Minh'),
(2, N'Công nghệ', '2026-01-01T00:00:00.0000000Z', N'Khám phá những đổi mới mới nhất về Trí Tuệ Nhân Tạo, điện toán đám mây và phát triển phần mềm. Cơ hội kết nối với hàng trăm chuyên gia công nghệ hàng đầu.', '2026-08-21T18:00:00.0000000Z', CAST(0 AS bit), N'https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), '2026-08-20T09:00:00.0000000Z', N'Hội Nghị Công Nghệ Việt Nam 2026', N'Trung Tâm Hội Nghị Quốc Gia, Hà Nội'),
(3, N'Hài kịch', '2026-01-01T00:00:00.0000000Z', N'Bật cười thả ga cùng những diễn viên hài độc thoại xuất sắc nhất Việt Nam biểu diễn trực tiếp. Một đêm giải trí đáng nhớ cho cả gia đình.', '2026-06-10T22:30:00.0000000Z', CAST(0 AS bit), N'https://images.unsplash.com/photo-1503095396549-807759245b35?w=800', CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), '2026-06-10T20:00:00.0000000Z', N'Đêm Hài Kịch Độc Thoại', N'Nhà Hát Bến Thành, TP. Hồ Chí Minh'),
(4, N'Nghệ thuật', '2026-01-01T00:00:00.0000000Z', N'Trải nghiệm hơn 200 tác phẩm nghệ thuật độc đáo từ 50 nghệ sĩ tài năng trong và ngoài nước. Không gian nghệ thuật sống động và đầy cảm hứng.', '2026-09-07T18:00:00.0000000Z', CAST(0 AS bit), N'https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800', CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), '2026-09-05T09:00:00.0000000Z', N'Triển Lãm Nghệ Thuật Đương Đại 2026', N'Bảo Tàng Mỹ Thuật TP. Hồ Chí Minh'),
(5, N'Thể thao', '2026-01-01T00:00:00.0000000Z', N'Theo dõi những trận đấu bóng đá kịch tính và đầy cảm xúc giữa các đội bóng hàng đầu. Sân khấu thể thao không thể bỏ lỡ mùa hè này.', '2026-07-25T21:00:00.0000000Z', CAST(0 AS bit), N'https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), '2026-07-25T17:00:00.0000000Z', N'Giải Bóng Đá Giao Hữu Mùa Hè 2026', N'Sân Vận Động Thống Nhất, TP. Hồ Chí Minh');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'EndDate', N'HasSeatMap', N'ImageUrl', N'IsActive', N'IsHot', N'IsSpecial', N'StartDate', N'Title', N'Venue') AND [object_id] = OBJECT_ID(N'[Events]'))
    SET IDENTITY_INSERT [Events] OFF;
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'EventId', N'Name', N'Price', N'SoldQuantity', N'TotalQuantity') AND [object_id] = OBJECT_ID(N'[TicketTypes]'))
    SET IDENTITY_INSERT [TicketTypes] ON;
INSERT INTO [TicketTypes] ([Id], [Description], [EventId], [Name], [Price], [SoldQuantity], [TotalQuantity])
VALUES (1, N'Khu vực đứng tự do', 1, N'Vé Phổ Thông', 50000.0, 0, 500),
(2, N'Ghế ngồi khu VIP + thẻ hậu trường', 1, N'Vé VIP', 150000.0, 0, 100),
(3, N'Tham dự toàn bộ hội nghị', 2, N'Vé Tiêu Chuẩn', 299000.0, 0, 300),
(4, N'Hội nghị + tất cả buổi workshop thực hành', 2, N'Vé Workshop', 499000.0, 0, 50),
(5, N'Chỗ ngồi khu vực tiêu chuẩn', 3, N'Vé Thường', 25000.0, 0, 200),
(6, N'Hàng ghế đầu + gặp gỡ nghệ sĩ', 3, N'Vé Cao Cấp', 75000.0, 0, 30),
(7, N'Vào cửa tham quan triển lãm', 4, N'Vé Xem Triển Lãm', 10000.0, 0, 400),
(8, N'Tham quan + workshop nghệ thuật với nghệ sĩ', 4, N'Vé Đặc Biệt', 35000.0, 0, 60),
(9, N'Khu vực khán đài thường', 5, N'Vé Khán Đài', 15000.0, 0, 800),
(10, N'Khu VIP có dịch vụ ăn uống', 5, N'Vé VIP Sân Cỏ', 50000.0, 0, 80);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'EventId', N'Name', N'Price', N'SoldQuantity', N'TotalQuantity') AND [object_id] = OBJECT_ID(N'[TicketTypes]'))
    SET IDENTITY_INSERT [TicketTypes] OFF;
GO


CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO


CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO


CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO


CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO


CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO


CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO


CREATE INDEX [IX_ChatMessages_UserId] ON [ChatMessages] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Coupons_Code] ON [Coupons] ([Code]);
GO


CREATE INDEX [IX_EventImages_EventId] ON [EventImages] ([EventId]);
GO


CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
GO


CREATE INDEX [IX_OrderItems_SeatId] ON [OrderItems] ([SeatId]);
GO


CREATE INDEX [IX_OrderItems_TicketTypeId] ON [OrderItems] ([TicketTypeId]);
GO


CREATE INDEX [IX_Orders_ApplicationUserId] ON [Orders] ([ApplicationUserId]);
GO


CREATE INDEX [IX_Orders_CouponId] ON [Orders] ([CouponId]);
GO


CREATE INDEX [IX_PredictionLogs_EventId_PredictedAt] ON [PredictionLogs] ([EventId], [PredictedAt]);
GO


CREATE INDEX [IX_RefundRequests_OrderId] ON [RefundRequests] ([OrderId]);
GO


CREATE INDEX [IX_RefundRequests_UserId] ON [RefundRequests] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Seats_EventId_Zone_RowLabel_SeatNumber] ON [Seats] ([EventId], [Zone], [RowLabel], [SeatNumber]);
GO


CREATE INDEX [IX_Seats_TicketTypeId] ON [Seats] ([TicketTypeId]);
GO


CREATE INDEX [IX_TicketTypes_EventId] ON [TicketTypes] ([EventId]);
GO


