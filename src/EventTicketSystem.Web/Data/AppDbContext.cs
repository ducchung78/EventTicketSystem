using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Event> Events { get; set; }
    public DbSet<TicketType> TicketTypes { get; set; }
    public DbSet<EventImage> EventImages { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<ContactMessage> ContactMessages { get; set; }
    public DbSet<ChatMessage>   ChatMessages     { get; set; }
    public DbSet<RefundRequest> RefundRequests   { get; set; }
    public DbSet<Seat>          Seats            { get; set; }
    public DbSet<AIConfig>      AIConfigs        { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TicketType>()
            .Property(t => t.Price).HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(o => o.UnitPrice).HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount).HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.OriginalAmount).HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.DiscountAmount).HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.Status).HasConversion<string>();

        modelBuilder.Entity<Order>()
            .HasOne(o => o.ApplicationUser)
            .WithMany()
            .HasForeignKey(o => o.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Coupon)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CouponId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RefundRequest>()
            .Property(r => r.Status).HasConversion<string>();

        modelBuilder.Entity<RefundRequest>()
            .Property(r => r.Reason).HasConversion<string>();

        modelBuilder.Entity<RefundRequest>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefundRequest>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Seat>()
            .Property(s => s.Status).HasConversion<string>();

        modelBuilder.Entity<Seat>()
            .HasOne(s => s.Event)
            .WithMany()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Seat>()
            .HasOne(s => s.TicketType)
            .WithMany()
            .HasForeignKey(s => s.TicketTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Seat>()
            .HasIndex(s => new { s.EventId, s.Zone, s.RowLabel, s.SeatNumber })
            .IsUnique();

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Seat)
            .WithMany()
            .HasForeignKey(oi => oi.SeatId)
            .OnDelete(DeleteBehavior.NoAction);

        // Single-row config with default values seeded at migration time
        modelBuilder.Entity<AIConfig>().HasData(new AIConfig { Id = 1 });

        modelBuilder.Entity<Coupon>()
            .Property(c => c.DiscountPercent).HasPrecision(5, 2);
        modelBuilder.Entity<Coupon>()
            .Property(c => c.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Coupon>()
            .Property(c => c.MinOrderValue).HasPrecision(18, 2);
        modelBuilder.Entity<Coupon>()
            .HasIndex(c => c.Code).IsUnique();

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = 1,
                Title = "Lễ Hội Âm Nhạc Rock 2026",
                Description = "Đêm âm nhạc rock đầy kịch tính với sự tham gia của các ban nhạc nổi tiếng trong và ngoài nước. Một trải nghiệm âm nhạc không thể bỏ qua!",
                StartDate = new DateTime(2026, 7, 15, 18, 0, 0, DateTimeKind.Utc),
                EndDate   = new DateTime(2026, 7, 15, 23, 0, 0, DateTimeKind.Utc),
                Venue = "Nhà Thi Đấu Phú Thọ, TP. Hồ Chí Minh",
                Category = "Âm nhạc",
                ImageUrl = "https://images.unsplash.com/photo-1540039155733-5bb30b53aa14?w=800",
                IsActive = true, IsHot = true, IsSpecial = false,
                CreatedAt = created
            },
            new Event
            {
                Id = 2,
                Title = "Hội Nghị Công Nghệ Việt Nam 2026",
                Description = "Khám phá những đổi mới mới nhất về Trí Tuệ Nhân Tạo, điện toán đám mây và phát triển phần mềm. Cơ hội kết nối với hàng trăm chuyên gia công nghệ hàng đầu.",
                StartDate = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
                EndDate   = new DateTime(2026, 8, 21, 18, 0, 0, DateTimeKind.Utc),
                Venue = "Trung Tâm Hội Nghị Quốc Gia, Hà Nội",
                Category = "Công nghệ",
                ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=800",
                IsActive = true, IsHot = true, IsSpecial = true,
                CreatedAt = created
            },
            new Event
            {
                Id = 3,
                Title = "Đêm Hài Kịch Độc Thoại",
                Description = "Bật cười thả ga cùng những diễn viên hài độc thoại xuất sắc nhất Việt Nam biểu diễn trực tiếp. Một đêm giải trí đáng nhớ cho cả gia đình.",
                StartDate = new DateTime(2026, 6, 10, 20, 0, 0, DateTimeKind.Utc),
                EndDate   = new DateTime(2026, 6, 10, 22, 30, 0, DateTimeKind.Utc),
                Venue = "Nhà Hát Bến Thành, TP. Hồ Chí Minh",
                Category = "Hài kịch",
                ImageUrl = "https://images.unsplash.com/photo-1503095396549-807759245b35?w=800",
                IsActive = true, IsHot = false, IsSpecial = false,
                CreatedAt = created
            },
            new Event
            {
                Id = 4,
                Title = "Triển Lãm Nghệ Thuật Đương Đại 2026",
                Description = "Trải nghiệm hơn 200 tác phẩm nghệ thuật độc đáo từ 50 nghệ sĩ tài năng trong và ngoài nước. Không gian nghệ thuật sống động và đầy cảm hứng.",
                StartDate = new DateTime(2026, 9, 5, 9, 0, 0, DateTimeKind.Utc),
                EndDate   = new DateTime(2026, 9, 7, 18, 0, 0, DateTimeKind.Utc),
                Venue = "Bảo Tàng Mỹ Thuật TP. Hồ Chí Minh",
                Category = "Nghệ thuật",
                ImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800",
                IsActive = true, IsHot = false, IsSpecial = true,
                CreatedAt = created
            },
            new Event
            {
                Id = 5,
                Title = "Giải Bóng Đá Giao Hữu Mùa Hè 2026",
                Description = "Theo dõi những trận đấu bóng đá kịch tính và đầy cảm xúc giữa các đội bóng hàng đầu. Sân khấu thể thao không thể bỏ lỡ mùa hè này.",
                StartDate = new DateTime(2026, 7, 25, 17, 0, 0, DateTimeKind.Utc),
                EndDate   = new DateTime(2026, 7, 25, 21, 0, 0, DateTimeKind.Utc),
                Venue = "Sân Vận Động Thống Nhất, TP. Hồ Chí Minh",
                Category = "Thể thao",
                ImageUrl = "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800",
                IsActive = true, IsHot = true, IsSpecial = false,
                CreatedAt = created
            }
        );

        modelBuilder.Entity<TicketType>().HasData(
            // Event 1 – Âm nhạc Rock
            new TicketType { Id = 1,  EventId = 1,  Name = "Vé Phổ Thông",           Description = "Khu vực đứng tự do",                                 Price = 50_000m,    TotalQuantity = 500, SoldQuantity = 0 },
            new TicketType { Id = 2,  EventId = 1,  Name = "Vé VIP",                  Description = "Ghế ngồi khu VIP + thẻ hậu trường",                  Price = 150_000m,   TotalQuantity = 100, SoldQuantity = 0 },
            // Event 2 – Công nghệ
            new TicketType { Id = 3,  EventId = 2,  Name = "Vé Tiêu Chuẩn",           Description = "Tham dự toàn bộ hội nghị",                           Price = 299_000m,   TotalQuantity = 300, SoldQuantity = 0 },
            new TicketType { Id = 4,  EventId = 2,  Name = "Vé Workshop",             Description = "Hội nghị + tất cả buổi workshop thực hành",          Price = 499_000m,   TotalQuantity = 50,  SoldQuantity = 0 },
            // Event 3 – Hài kịch
            new TicketType { Id = 5,  EventId = 3,  Name = "Vé Thường",               Description = "Chỗ ngồi khu vực tiêu chuẩn",                        Price = 25_000m,    TotalQuantity = 200, SoldQuantity = 0 },
            new TicketType { Id = 6,  EventId = 3,  Name = "Vé Cao Cấp",              Description = "Hàng ghế đầu + gặp gỡ nghệ sĩ",                     Price = 75_000m,    TotalQuantity = 30,  SoldQuantity = 0 },
            // Event 4 – Nghệ thuật
            new TicketType { Id = 7,  EventId = 4,  Name = "Vé Xem Triển Lãm",        Description = "Vào cửa tham quan triển lãm",                        Price = 10_000m,    TotalQuantity = 400, SoldQuantity = 0 },
            new TicketType { Id = 8,  EventId = 4,  Name = "Vé Đặc Biệt",             Description = "Tham quan + workshop nghệ thuật với nghệ sĩ",        Price = 35_000m,    TotalQuantity = 60,  SoldQuantity = 0 },
            // Event 5 – Thể thao
            new TicketType { Id = 9,  EventId = 5,  Name = "Vé Khán Đài",   Description = "Khu vực khán đài thường",       Price = 15_000m, TotalQuantity = 800, SoldQuantity = 0 },
            new TicketType { Id = 10, EventId = 5,  Name = "Vé VIP Sân Cỏ", Description = "Khu VIP có dịch vụ ăn uống",   Price = 50_000m, TotalQuantity = 80,  SoldQuantity = 0 }
        );

        var expiry2027 = new DateTime(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        modelBuilder.Entity<Coupon>().HasData(
            new Coupon { Id = 1, Code = "WELCOME10",    Description = "Giảm 10% cho đơn hàng đầu tiên",           DiscountPercent = 10, DiscountAmount = 0,      MinOrderValue = 50_000,    MaxUses = 100, UsedCount = 0, ExpiryDate = expiry2027, IsActive = true },
            new Coupon { Id = 2, Code = "SUMMER50K",    Description = "Giảm 50,000đ cho đơn từ 200,000đ",         DiscountPercent = 0,  DiscountAmount = 50_000, MinOrderValue = 200_000,   MaxUses = 50,  UsedCount = 0, ExpiryDate = expiry2027, IsActive = true },
            new Coupon { Id = 3, Code = "VIP20",        Description = "Giảm 20% dành cho thành viên VIP",         DiscountPercent = 20, DiscountAmount = 0,      MinOrderValue = 100_000,   MaxUses = 30,  UsedCount = 0, ExpiryDate = expiry2027, IsActive = true },
            new Coupon { Id = 4, Code = "TICKET100K",   Description = "Giảm 100,000đ cho đơn từ 500,000đ",       DiscountPercent = 0,  DiscountAmount = 100_000,MinOrderValue = 500_000,   MaxUses = 20,  UsedCount = 0, ExpiryDate = expiry2027, IsActive = true },
            new Coupon { Id = 5, Code = "FREESHIP",     Description = "Giảm 5% không giới hạn đơn tối thiểu",   DiscountPercent = 5,  DiscountAmount = 0,      MinOrderValue = 0,         MaxUses = 200, UsedCount = 0, ExpiryDate = expiry2027, IsActive = true }
        );
    }
}
