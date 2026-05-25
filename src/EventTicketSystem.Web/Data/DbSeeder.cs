using EventTicketSystem.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventTicketSystem.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // Roles
        foreach (var role in new[] { "Admin", "User" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Admin account
        const string adminEmail = "admin@tickethub.vn";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, Ho = "Quản Trị", Ten = "Viên", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Admin@123456");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Sample user account
        const string userEmail = "nguyen.van.an@email.com";
        ApplicationUser? sampleUser = await userManager.FindByEmailAsync(userEmail);
        if (sampleUser == null)
        {
            sampleUser = new ApplicationUser { UserName = userEmail, Email = userEmail, Ho = "Nguyễn Văn", Ten = "An", PhoneNumber = "0901234567", EmailConfirmed = true };
            await userManager.CreateAsync(sampleUser, "User@123456");
            await userManager.AddToRoleAsync(sampleUser, "User");
        }

        // More sample users
        var moreUsers = new[]
        {
            ("tran.thi.binh@email.com", "Trần Thị", "Bình", "0912345678"),
            ("le.van.cuong@email.com",  "Lê Văn",   "Cường", "0923456789"),
            ("pham.thi.dung@email.com", "Phạm Thị", "Dung",  "0934567890"),
        };
        foreach (var (email, ho, ten, phone) in moreUsers)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var u = new ApplicationUser { UserName = email, Email = email, Ho = ho, Ten = ten, PhoneNumber = phone, EmailConfirmed = true };
                await userManager.CreateAsync(u, "User@123456");
                await userManager.AddToRoleAsync(u, "User");
            }
        }

        // Seed extra events for new homepage sections (idempotent by title)
        await SeedExtraEventsAsync(db);

        // Seed gallery images for all events
        await SeedGalleryImagesAsync(db);

        // Sample orders (only seed if none exist)
        if (await db.Orders.AnyAsync()) return;

        var ticketTypes = await db.TicketTypes.ToListAsync();
        TicketType? GetTt(int id) => ticketTypes.FirstOrDefault(t => t.Id == id);

        var now = DateTime.UtcNow;
        var sampleUserId = (await userManager.FindByEmailAsync(userEmail))?.Id;

        var seedOrders = new List<(string name, string email, string? uid, int ttId, int qty, DateTime date)>
        {
            ("Nguyễn Văn An",  userEmail,                          sampleUserId, 3, 2, now.AddDays(-28)),
            ("Nguyễn Văn An",  userEmail,                          sampleUserId, 5, 4, now.AddDays(-20)),
            ("Trần Thị Bình",  "tran.thi.binh@email.com",          null,         1, 3, now.AddDays(-22)),
            ("Trần Thị Bình",  "tran.thi.binh@email.com",          null,         6, 2, now.AddDays(-15)),
            ("Lê Văn Cường",   "le.van.cuong@email.com",           null,         3, 1, now.AddDays(-18)),
            ("Phạm Thị Dung",  "pham.thi.dung@email.com",          null,         2, 1, now.AddDays(-10)),
            ("Hoàng Minh Tuấn","hoang.tuan@gmail.com",             null,         9, 5, now.AddDays(-8)),
            ("Võ Thị Hoa",     "vo.thi.hoa@gmail.com",             null,         7, 2, now.AddDays(-5)),
            ("Đặng Quốc Bảo",  "dang.quoc.bao@gmail.com",         null,         1, 2, now.AddDays(-3)),
            ("Nguyễn Văn An",  userEmail,                          sampleUserId, 4, 1, now.AddDays(-1)),
            // Older orders for monthly chart
            ("Mai Thị Lan",    "mai.thi.lan@email.com",            null,         3, 3, now.AddDays(-45)),
            ("Bùi Đức Long",   "bui.duc.long@email.com",           null,         5, 2, now.AddDays(-50)),
            ("Trương Hải Nam", "truong.hai.nam@email.com",         null,         1, 1, now.AddDays(-55)),
            ("Vũ Thị Minh",    "vu.thi.minh@email.com",            null,         9, 4, now.AddDays(-60)),
            ("Phan Văn Khoa",  "phan.van.khoa@email.com",          null,         3, 2, now.AddDays(-70)),
        };

        foreach (var (name, email, uid, ttId, qty, date) in seedOrders)
        {
            var tt = GetTt(ttId);
            if (tt == null) continue;

            var order = new Order
            {
                CustomerName = name,
                CustomerEmail = email,
                ApplicationUserId = uid,
                OrderDate = date,
                Status = OrderStatus.Confirmed,
                TotalAmount = tt.Price * qty
            };
            order.OrderItems.Add(new OrderItem { TicketTypeId = tt.Id, Quantity = qty, UnitPrice = tt.Price });
            tt.SoldQuantity += qty;
            db.Orders.Add(order);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedExtraEventsAsync(AppDbContext db)
    {
        var extraEvents = new[]
        {
            new
            {
                Title    = "Đêm Nhạc Acoustic Mùa Hè",
                ImageUrl = "https://images.unsplash.com/photo-1540039155733-5bb30b53aa14?w=800",
                Description = "Đêm nhạc acoustic lãng mạn bên bờ sông với những giai điệu nhẹ nhàng sâu lắng. Thư giãn và tận hưởng âm nhạc dưới bầu trời đêm.",
                StartDate = new DateTime(2026, 5, 31, 19, 0, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 5, 31, 22, 0, 0, DateTimeKind.Utc),
                Venue    = "Công Viên Bạch Đằng, TP. Hồ Chí Minh",
                Category = "Âm nhạc",
                IsHot = false, IsSpecial = false,
                Tickets = new[] {
                    ("Vé Phổ Thông", "Khu vực đứng tự do bên bờ sông",          80_000m,    300),
                    ("Vé VIP",       "Ghế ngồi hàng đầu + gặp gỡ nghệ sĩ",      200_000m,   50),
                }
            },
            new
            {
                Title    = "Hội Thảo Khởi Nghiệp 2026",
                ImageUrl = "https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=800",
                Description = "Hội tụ startup, nhà đầu tư và mentor hàng đầu. Học hỏi kinh nghiệm thực chiến và mở rộng mạng lưới kết nối.",
                StartDate = new DateTime(2026, 5, 28, 8, 30, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 5, 28, 17, 0, 0, DateTimeKind.Utc),
                Venue    = "WeWork Lê Duẩn, TP. Hồ Chí Minh",
                Category = "Hội thảo",
                IsHot = false, IsSpecial = false,
                Tickets = new[] {
                    ("Vé Tham Dự", "Toàn bộ hội thảo trong ngày",               50_000m,    200),
                    ("Vé Premium", "Hội thảo + tài liệu + bữa trưa networking", 150_000m,   30),
                }
            },
            new
            {
                Title    = "Tham Quan Làng Cổ Đường Lâm",
                ImageUrl = "https://images.unsplash.com/photo-1528360983277-13d401cdc186?w=800",
                Description = "Khám phá làng cổ Đường Lâm hơn 1000 năm tuổi với kiến trúc nhà cổ độc đáo, đình làng và nét văn hóa truyền thống đặc sắc.",
                StartDate = new DateTime(2026, 5, 31, 7, 0, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 5, 31, 17, 0, 0, DateTimeKind.Utc),
                Venue    = "Làng Cổ Đường Lâm, Hà Nội",
                Category = "Tham quan",
                IsHot = false, IsSpecial = false,
                Tickets = new[] {
                    ("Vé Người Lớn", "Tham quan + hướng dẫn viên chuyên nghiệp", 45_000m,   100),
                    ("Vé Trẻ Em",    "Dành cho trẻ dưới 12 tuổi",                25_000m,   60),
                }
            },
            new
            {
                Title    = "Festival Nghệ Thuật Đường Phố 2026",
                ImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800",
                Description = "Lễ hội nghệ thuật đường phố quy mô lớn với nhào lộn, graffiti, múa đương đại và hàng chục nghệ sĩ tài năng từ khắp nơi.",
                StartDate = new DateTime(2026, 6, 20, 15, 0, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 6, 20, 22, 0, 0, DateTimeKind.Utc),
                Venue    = "Phố Đi Bộ Nguyễn Huệ, TP. Hồ Chí Minh",
                Category = "Nghệ thuật",
                IsHot = true, IsSpecial = false,
                Tickets = new[] {
                    ("Vé Tự Do", "Vào cổng + xem tất cả biểu diễn",           30_000m,   500),
                    ("Vé VIP",   "Khu VIP + đồ uống miễn phí + meet&greet",   100_000m,  80),
                }
            },
            new
            {
                Title    = "Workshop Nhiếp Ảnh Nghệ Thuật",
                ImageUrl = "https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=800",
                Description = "Học kỹ thuật chụp ảnh chuyên nghiệp với DSLR và Mirrorless. Thực hành ngoài trời tại các địa điểm đẹp của thành phố.",
                StartDate = new DateTime(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 6, 14, 17, 0, 0, DateTimeKind.Utc),
                Venue    = "Studio Ánh Sáng, Quận 3, TP. Hồ Chí Minh",
                Category = "Hội thảo",
                IsHot = false, IsSpecial = false,
                Tickets = new[] {
                    ("Vé Cơ Bản", "Workshop 4 giờ + tài liệu học tập",         120_000m,  20),
                    ("Vé Pro",    "Workshop + mentoring 1-1 + in ảnh kỷ niệm", 250_000m,  10),
                }
            },
            new
            {
                Title    = "Giải Chạy Marathon Xanh 2026",
                ImageUrl = "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800",
                Description = "Giải chạy bộ vì môi trường xanh với cự ly 5km, 10km và 21km. Cùng chạy vì thành phố xanh sạch đẹp hơn!",
                StartDate = new DateTime(2026, 6, 7, 5, 30, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc),
                Venue    = "Công Viên Gia Định, TP. Hồ Chí Minh",
                Category = "Thể thao",
                IsHot = false, IsSpecial = false,
                Tickets = new[] {
                    ("Cự Ly 5km",  "Chạy 5km + áo đồng phục + huy chương",            100_000m, 300),
                    ("Cự Ly 21km", "Chạy 21km + áo + huy chương + bộ kit đầy đủ",     250_000m, 100),
                }
            },
            new
            {
                Title    = "Tour Khám Phá Sapa",
                ImageUrl = "https://images.unsplash.com/photo-1528360983277-13d401cdc186?w=800",
                Description = "Hành trình khám phá núi rừng Tây Bắc, thăm bản làng H'Mông, Dao đỏ và chinh phục đỉnh Fansipan huyền thoại.",
                StartDate = new DateTime(2026, 7, 12, 6, 0, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 7, 14, 18, 0, 0, DateTimeKind.Utc),
                Venue    = "Sapa, Lào Cai",
                Category = "Tham quan",
                IsHot = false, IsSpecial = true,
                Tickets = new[] {
                    ("Gói 2 Ngày 1 Đêm",     "Xe + khách sạn 3* + ăn sáng + HDV",          1_500_000m, 30),
                    ("Gói 3 Ngày 2 Đêm VIP",  "Xe riêng + resort 4* + full board + HDV riêng", 2_500_000m, 15),
                }
            },
            new
            {
                Title    = "Vũ Kịch Hồ Thiên Nga",
                ImageUrl = "https://images.unsplash.com/photo-1503095396549-807759245b35?w=800",
                Description = "Vở vũ kịch kinh điển Hồ Thiên Nga của Tchaikovsky dàn dựng hoành tráng bởi đoàn nghệ thuật Ba Lan. Một đêm nghệ thuật đỉnh cao.",
                StartDate = new DateTime(2026, 6, 25, 19, 30, 0, DateTimeKind.Utc),
                EndDate   = (DateTime?)new DateTime(2026, 6, 25, 22, 0, 0, DateTimeKind.Utc),
                Venue    = "Nhà Hát Lớn Hà Nội, Hà Nội",
                Category = "Nghệ thuật",
                IsHot = false, IsSpecial = true,
                Tickets = new[] {
                    ("Vé Hạng Thường",    "Ghế khu vực B và C",                                  200_000m, 150),
                    ("Vé Hạng Đặc Biệt",  "Ghế hàng đầu + chương trình đặc biệt + quà lưu niệm", 500_000m, 40),
                }
            },
        };

        foreach (var e in extraEvents)
        {
            if (await db.Events.AnyAsync(x => x.Title == e.Title)) continue;

            var evt = new Event
            {
                Title    = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate   = e.EndDate,
                Venue     = e.Venue,
                Category  = e.Category,
                ImageUrl  = e.ImageUrl,
                IsActive  = true,
                IsHot     = e.IsHot,
                IsSpecial = e.IsSpecial,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            };
            foreach (var (name, desc, price, qty) in e.Tickets)
                evt.TicketTypes.Add(new TicketType { Name = name, Description = desc, Price = price, TotalQuantity = qty, SoldQuantity = 0 });

            db.Events.Add(evt);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedGalleryImagesAsync(AppDbContext db)
    {
        var gallery = new Dictionary<string, (string url, string caption)[]>
        {
            ["Lễ Hội Âm Nhạc Rock 2026"] = [
                ("https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=600", "Không khí sôi động sân khấu chính"),
                ("https://images.unsplash.com/photo-1501386761578-eaa54b1b62cc?w=600", "Ánh đèn rực rỡ đêm nhạc"),
                ("https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=600", "Nghệ sĩ biểu diễn"),
            ],
            ["Hội Nghị Công Nghệ Việt Nam 2026"] = [
                ("https://images.unsplash.com/photo-1587825140708-dfaf72ae4b04?w=600", "Hội trường hội nghị"),
                ("https://images.unsplash.com/photo-1551818255-e6e10975bc17?w=600", "Phiên thảo luận chuyên gia"),
                ("https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=600", "Networking sau hội thảo"),
            ],
            ["Đêm Hài Kịch Độc Thoại"] = [
                ("https://images.unsplash.com/photo-1460881680858-30d872d5b530?w=600", "Sân khấu nhà hát"),
                ("https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=600", "Nghệ sĩ trên sân khấu"),
                ("https://images.unsplash.com/photo-1503095396549-807759245b35?w=600", "Biểu diễn hài kịch"),
            ],
            ["Triển Lãm Nghệ Thuật Đương Đại 2026"] = [
                ("https://images.unsplash.com/photo-1536924940846-227afb31e2a5?w=600", "Không gian triển lãm"),
                ("https://images.unsplash.com/photo-1513364776144-60967b0f800f?w=600", "Tác phẩm nghệ thuật đương đại"),
                ("https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=600", "Nghệ sĩ và tác phẩm"),
            ],
            ["Giải Bóng Đá Giao Hữu Mùa Hè 2026"] = [
                ("https://images.unsplash.com/photo-1579952363873-27f3bade9f55?w=600", "Sân vận động rực rỡ"),
                ("https://images.unsplash.com/photo-1522778119026-d647f0596c20?w=600", "Trận đấu kịch tính"),
                ("https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=600", "Cầu thủ thi đấu"),
            ],
            ["Đêm Nhạc Acoustic Mùa Hè"] = [
                ("https://images.unsplash.com/photo-1510915361894-db8b60106cb1?w=600", "Nhạc cụ acoustic"),
                ("https://images.unsplash.com/photo-1471478331149-c72f17e33c73?w=600", "Guitar bên bờ sông"),
                ("https://images.unsplash.com/photo-1540039155733-5bb30b53aa14?w=600", "Đêm nhạc lãng mạn"),
            ],
            ["Hội Thảo Khởi Nghiệp 2026"] = [
                ("https://images.unsplash.com/photo-1559136555-9303baea8ebd?w=600", "Pitching startup"),
                ("https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=600", "Thảo luận nhóm"),
                ("https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=600", "Toàn cảnh hội thảo"),
            ],
            ["Tham Quan Làng Cổ Đường Lâm"] = [
                ("https://images.unsplash.com/photo-1528360983277-13d401cdc186?w=600", "Kiến trúc làng cổ"),
                ("https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600", "Phong cảnh thiên nhiên"),
                ("https://images.unsplash.com/photo-1490642914619-7955a3fd483c?w=600", "Văn hóa truyền thống"),
            ],
            ["Festival Nghệ Thuật Đường Phố 2026"] = [
                ("https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=600", "Lễ hội đường phố"),
                ("https://images.unsplash.com/photo-1504609773096-104ff2c73ba4?w=600", "Biểu diễn ngoài trời"),
                ("https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?w=600", "Nghệ thuật đương đại"),
            ],
            ["Workshop Nhiếp Ảnh Nghệ Thuật"] = [
                ("https://images.unsplash.com/photo-1471341971476-ae15ff5dd4ea?w=600", "Kỹ thuật chụp ảnh"),
                ("https://images.unsplash.com/photo-1495745966610-2a67f2297e5e?w=600", "Nhiếp ảnh gia thực hành"),
                ("https://images.unsplash.com/photo-1517048676732-d65bc937f952?w=600", "Lớp học workshop"),
            ],
            ["Giải Chạy Marathon Xanh 2026"] = [
                ("https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=600", "Vận động viên xuất phát"),
                ("https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=600", "Đường chạy marathon"),
                ("https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=600", "Về đích rực rỡ"),
            ],
            ["Tour Khám Phá Sapa"] = [
                ("https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=600", "Đỉnh Fansipan hùng vĩ"),
                ("https://images.unsplash.com/photo-1528360983277-13d401cdc186?w=600", "Bản làng H'Mông"),
                ("https://images.unsplash.com/photo-1490642914619-7955a3fd483c?w=600", "Ruộng bậc thang Sapa"),
            ],
            ["Vũ Kịch Hồ Thiên Nga"] = [
                ("https://images.unsplash.com/photo-1518834107812-67b0b7c58434?w=600", "Vũ công ballet"),
                ("https://images.unsplash.com/photo-1547153760-18fc86324498?w=600", "Biểu diễn trên sân khấu"),
                ("https://images.unsplash.com/photo-1503095396549-807759245b35?w=600", "Nhà hát lớn"),
            ],
        };

        var allTitles = gallery.Keys.ToList();
        var events    = await db.Events.Where(e => allTitles.Contains(e.Title)).ToListAsync();

        bool changed = false;
        foreach (var evt in events)
        {
            if (!gallery.TryGetValue(evt.Title, out var imgs)) continue;
            if (await db.EventImages.AnyAsync(i => i.EventId == evt.Id)) continue;

            for (int s = 0; s < imgs.Length; s++)
                db.EventImages.Add(new EventImage { EventId = evt.Id, ImagePath = imgs[s].url, Caption = imgs[s].caption, SortOrder = s });
            changed = true;
        }

        if (changed) await db.SaveChangesAsync();
    }
}
