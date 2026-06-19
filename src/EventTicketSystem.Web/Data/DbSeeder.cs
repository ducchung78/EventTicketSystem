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
        foreach (var role in new[] { "SuperAdmin", "Admin", "User" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Admin account — also gets SuperAdmin (root account)
        const string adminEmail = "admin@tickethub.vn";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, Ho = "Quản Trị", Ten = "Viên", EmailConfirmed = true, LockoutEnabled = true };
            await userManager.CreateAsync(admin, "Admin@123456");
            await userManager.AddToRoleAsync(admin, "Admin");
            await userManager.AddToRoleAsync(admin, "SuperAdmin");
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        }

        // Ensure LockoutEnabled = true for all existing users
        await userManager.Users
            .Where(u => !u.LockoutEnabled)
            .ForEachAsync(async u => await userManager.SetLockoutEnabledAsync(u, true));

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
                var u = new ApplicationUser { UserName = email, Email = email, Ho = ho, Ten = ten, PhoneNumber = phone, EmailConfirmed = true, LockoutEnabled = true };
                await userManager.CreateAsync(u, "User@123456");
                await userManager.AddToRoleAsync(u, "User");
            }
        }

        // Seed footer settings (idempotent)
        await SeedFooterSettingsAsync(db);

        // Seed payment methods
        await SeedPaymentMethodsAsync(db);

        // Seed extra events for new homepage sections (idempotent by title)
        await SeedExtraEventsAsync(db);

        // Seed real Ticketbox events with real images
        await SeedTicketboxEventsAsync(db);

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

    private static async Task SeedPaymentMethodsAsync(AppDbContext db)
    {
        if (await db.PaymentMethods.AnyAsync()) return;

        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.PaymentMethods.AddRange(
            new PaymentMethod { Name = "VNPAY / Ứng dụng ngân hàng", IsActive = true, SortOrder = 1, CreatedAt = created },
            new PaymentMethod { Name = "VietQR",                       IsActive = true, SortOrder = 2, CreatedAt = created },
            new PaymentMethod { Name = "ShopeePay",                    IsActive = true, SortOrder = 3, CreatedAt = created },
            new PaymentMethod { Name = "ZaloPay",                      IsActive = true, SortOrder = 4, CreatedAt = created },
            new PaymentMethod { Name = "Thẻ ghi nợ / Thẻ tín dụng",  IsActive = true, SortOrder = 5, CreatedAt = created }
        );
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

    private static async Task SeedTicketboxEventsAsync(AppDbContext db)
    {
        var events = new[]
        {
            // ========== NHẠC SỐNG ==========
            new {
                Title    = "[BẾN THÀNH] Đêm nhạc Hoài Lâm - Special Guest: Thanh Tú",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/49/0a/e0/3f216efbfc7bc38d1866a3d648a5e302.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Đêm nhạc đặc biệt của ca sĩ Hoài Lâm với sự góp mặt của Special Guest Thanh Tú. Những bản tình ca bất hủ trong không gian ấm cúng của Nhà hát Bến Thành.",
                Start    = new DateTime(2026, 6, 27, 13, 0, 0, DateTimeKind.Utc),
                Venue    = "Nhà hát Bến Thành, TP. Hồ Chí Minh",
                Category = "Âm nhạc",
                IsHot = true, IsSpecial = true,
                Tickets  = new[] {
                    ("Vé Thường",    "Khu vực hạng C",              500_000m,  200),
                    ("Vé VIP",       "Khu vực hàng đầu + quà tặng", 800_000m,   80),
                }
            },
            new {
                Title    = "[BẾN THÀNH] Đêm Nhạc Thái Lê Minh Hiếu - Hồ Đông Quan - Swan",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/e5/fe/b7/33efa622db34102a4b0d9899c0e0511d.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Ba nghệ sĩ Thái Lê Minh Hiếu, Hồ Đông Quan và Swan hội tụ trong một đêm nhạc đỉnh cao tại sân khấu Nhà hát Bến Thành.",
                Start    = new DateTime(2026, 6, 25, 13, 0, 0, DateTimeKind.Utc),
                Venue    = "Nhà hát Bến Thành, TP. Hồ Chí Minh",
                Category = "Âm nhạc",
                IsHot = false, IsSpecial = true,
                Tickets  = new[] {
                    ("Vé Hạng B",  "Khu ngồi hàng B",       300_000m, 150),
                    ("Vé Hạng A",  "Khu ngồi hàng A VIP",   500_000m,  60),
                }
            },
            new {
                Title    = "LIVESHOW ĐAN TRƯỜNG",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/6a/c5/8b/7daf1cbbe685eb191ebe1a8bb56f6307.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/31/08/08/91679d2bd0ebcae1c7184955ef96d7b8.png",
                Desc     = "Liveshow hoành tráng của danh ca Đan Trường với hàng trăm bản hit trải dài hơn 3 thập kỷ ca hát. Một đêm nhạc không thể nào quên.",
                Start    = new DateTime(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc),
                Venue    = "The Grand Ballroom, TP. Hồ Chí Minh",
                Category = "Âm nhạc",
                IsHot = true, IsSpecial = true,
                Tickets  = new[] {
                    ("Vé Thường",       "Khu đứng xem",                   1_130_000m, 500),
                    ("Vé Hạng Nhất",    "Ghế ngồi + Welcome Drink",       1_580_000m, 200),
                    ("Vé SVIP",         "Bàn VIP + gặp gỡ nghệ sĩ",       2_300_000m,  50),
                }
            },
            new {
                Title    = "[CAT&MOUSE] THÀNH VÁ ft. HIẾU MINH",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/a6/ec/ae/e394a14c5091867eca5627d1cbc030fb.png",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Đêm diễn đặc biệt của THÀNH VÁ với sự kết hợp cùng HIẾU MINH tại Cat&Mouse Live Music – không gian âm nhạc underground số 1 Sài Gòn.",
                Start    = new DateTime(2026, 7, 10, 13, 0, 0, DateTimeKind.Utc),
                Venue    = "Cat&Mouse Live Music, TP. Hồ Chí Minh",
                Category = "Âm nhạc",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Standing",  "Khu đứng xem",         500_000m, 300),
                    ("VIP Table", "Bàn VIP 4 người",     2_400_000m,  20),
                }
            },
            new {
                Title    = "PHƯƠNG PHƯƠNG THẢO & NGUYỄN ĐÌNH TUẤN DŨNG - NGƯỢC CHIỀU NẮNG ĐỔ",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/fd/c9/36/85373673804a49d3d45dcac5590b06b7.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/10/f1/b2/a317ce6bb7f43ac5ea637e795d66623c.jpg",
                Desc     = "Hai nghệ sĩ Phương Phương Thảo và Nguyễn Đình Tuấn Dũng cùng nhau kể chuyện qua âm nhạc trong show diễn đầy cảm xúc 'Ngược Chiều Nắng Đổ'.",
                Start    = new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc),
                Venue    = "Nhà hát Âu Cơ, Hà Nội",
                Category = "Âm nhạc",
                IsHot = false, IsSpecial = true,
                Tickets  = new[] {
                    ("Vé Hạng C",  "Khu vực C",             800_000m, 200),
                    ("Vé Hạng A",  "Khu vực A + quà tặng", 1_200_000m,  80),
                }
            },
            new {
                Title    = "2026 SEO IN GUK FANMEETING TOUR in Vietnam",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/f8/cd/d1/dfe7b30dbadf530f2346f45891d5e673.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/31/08/08/91679d2bd0ebcae1c7184955ef96d7b8.png",
                Desc     = "Fan meeting tour đặc biệt của nam diễn viên kiêm ca sĩ Seo In Guk tại Việt Nam. Cơ hội gặp gỡ thần tượng xứ Hàn trong tầm tay!",
                Start    = new DateTime(2026, 8, 15, 11, 0, 0, DateTimeKind.Utc),
                Venue    = "Nhà thi đấu Phú Thọ, TP. Hồ Chí Minh",
                Category = "Âm nhạc",
                IsHot = true, IsSpecial = true,
                Tickets  = new[] {
                    ("Zone C",      "Khu vực đứng",                           1_200_000m, 800),
                    ("Zone B",      "Khu vực ngồi tầng 1",                    1_800_000m, 400),
                    ("Zone A VIP",  "Hàng ghế đầu + Photocard ký tên",        2_500_000m, 100),
                }
            },
            // ========== SÂN KHẤU & NGHỆ THUẬT ==========
            new {
                Title    = "SÂN KHẤU THIÊN ĐĂNG: THIÊN ĐƯỜNG PROMAX",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/6d/b0/79/3b1c19c2992180609951eadf199be126.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Show diễn âm nhạc và nghệ thuật đặc biệt của sân khấu Thiên Đăng với chủ đề THIÊN ĐƯỜNG PROMAX. Những tiết mục hoành tráng, đỉnh cao.",
                Start    = new DateTime(2026, 6, 28, 11, 0, 0, DateTimeKind.Utc),
                Venue    = "Sân khấu Thiên Đăng, TP. Hồ Chí Minh",
                Category = "Nghệ thuật",
                IsHot = false, IsSpecial = true,
                Tickets  = new[] {
                    ("Vé Thường", "Khu vực ngồi",               200_000m, 300),
                    ("Vé VIP",    "Ghế VIP + gói đồ uống",      400_000m,  80),
                }
            },
            new {
                Title    = "AQUAFINA VIETNAM INTERNATIONAL FASHION WEEK XUÂN HÈ 2026",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/2e/c8/27/9fa5f626a8f318ad157a053b27953b68.png",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/10/f1/b2/a317ce6bb7f43ac5ea637e795d66623c.jpg",
                Desc     = "Tuần lễ thời trang quốc tế Việt Nam mùa Xuân Hè 2026 với sự tham gia của hàng chục nhà thiết kế nổi tiếng trong và ngoài nước.",
                Start    = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc),
                Venue    = "Trung tâm Hội nghị Quốc gia, Hà Nội",
                Category = "Nghệ thuật",
                IsHot = true, IsSpecial = true,
                Tickets  = new[] {
                    ("Vé Ngày",     "Xem 1 buổi diễn",                300_000m, 500),
                    ("Vé Trọn Gói", "Toàn bộ tuần lễ + VIP lounge",   800_000m, 100),
                }
            },
            new {
                Title    = "Show Thực Cảnh Anh Hùng Cờ Lau - Đinh Bộ Lĩnh",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/4f/4d/31/6da02c3b396bc1b68fc3e487a9cb1fab.png",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Show thực cảnh hoành tráng tái hiện câu chuyện về người anh hùng Đinh Bộ Lĩnh với hàng trăm diễn viên, kỹ xảo ánh sáng và âm thanh đỉnh cao.",
                Start    = new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc),
                Venue    = "Khu du lịch Hoa Lư, Ninh Bình",
                Category = "Nghệ thuật",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Vé Thường",    "Khu khán đài B",              150_000m, 600),
                    ("Vé Đặc Biệt", "Khán đài A + quà lưu niệm",   250_000m, 200),
                }
            },
            new {
                Title    = "BASTILLE DAY 2026",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/ae/53/48/5c7cfeb926a57844df50914c440010a6.png",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/10/f1/b2/a317ce6bb7f43ac5ea637e795d66623c.jpg",
                Desc     = "Ngày Quốc khánh Pháp 2026 được tổ chức tại L'Espace với nhiều hoạt động văn hóa nghệ thuật Pháp-Việt đặc sắc. Sự kiện miễn phí mở cửa cho tất cả.",
                Start    = new DateTime(2026, 7, 14, 9, 0, 0, DateTimeKind.Utc),
                Venue    = "L'Espace, Hà Nội",
                Category = "Nghệ thuật",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Vé Miễn Phí", "Tham dự toàn bộ sự kiện", 0m, 1000),
                }
            },
            // ========== HỘI THẢO & WORKSHOP ==========
            new {
                Title    = "[GARDEN ART] - ART WORKSHOP VẼ TRANH ACRYLIC HIỆU ỨNG 3D",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/d5/de/ef/2d0fdcb95a34f44fe154e1bdaf93928b.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Workshop vẽ tranh Acrylic với hiệu ứng 3D độc đáo tại Garden Art Studio. Không cần kinh nghiệm trước, hướng dẫn từng bước bởi giảng viên chuyên nghiệp.",
                Start    = new DateTime(2026, 6, 21, 4, 0, 0, DateTimeKind.Utc),
                Venue    = "Garden Art Studio, TP. Hồ Chí Minh",
                Category = "Hội thảo",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Workshop 3 Tiếng", "Dụng cụ + vật liệu đầy đủ + tranh mang về", 390_000m, 20),
                }
            },
            new {
                Title    = "ART WORKSHOP THÊU TÚI HOA TRONG LÒNG",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/77/ea/a8/f76f65d79ac1cf7fe14efba408a86bbb.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Workshop thêu túi vải hoa độc đáo tại Flower 1969's Studio. Học kỹ thuật thêu tay truyền thống và tạo ra chiếc túi thêu cá nhân của riêng bạn.",
                Start    = new DateTime(2026, 6, 20, 4, 0, 0, DateTimeKind.Utc),
                Venue    = "Flower 1969's Studio, TP. Hồ Chí Minh",
                Category = "Hội thảo",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Vé Workshop", "Dụng cụ + nguyên liệu + sản phẩm mang về", 370_000m, 15),
                }
            },
            new {
                Title    = "[DẾ GARDEN] Stress Breaker Workshop",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/d0/09/6e/24e090ca92d3670405d9ea4f37a60665.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Workshop giải tỏa stress với các hoạt động thư giãn sáng tạo tại Dế Garden. Vẽ mandala, làm nến thơm và nhiều trải nghiệm thú vị khác.",
                Start    = new DateTime(2026, 6, 17, 4, 0, 0, DateTimeKind.Utc),
                Venue    = "Dế Garden, TP. Hồ Chí Minh",
                Category = "Hội thảo",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Standard", "Toàn bộ workshop + đồ uống + vật liệu", 375_000m, 20),
                }
            },
            new {
                Title    = "[WORKSHOP] PORTFOLIO & DU HỌC THIẾT KẾ - QUẢNG CÁO TOÀN CẦU",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/e0/58/23/105c3f975e76ce22e1d2e858902141c3.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Workshop online miễn phí hướng dẫn xây dựng Portfolio thiết kế và định hướng du học ngành thiết kế - quảng cáo tại các trường quốc tế uy tín.",
                Start    = new DateTime(2026, 6, 20, 8, 0, 0, DateTimeKind.Utc),
                Venue    = "Online (Zoom)",
                Category = "Hội thảo",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Vé Miễn Phí", "Tham dự online qua Zoom", 0m, 200),
                }
            },
            // ========== THAM QUAN & TRẢI NGHIỆM ==========
            new {
                Title    = "VinWonders Nha Trang",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/d2/62/f6/bee2171a7ea04782f698a7ad7ea667d4.jpeg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/10/f1/b2/a317ce6bb7f43ac5ea637e795d66623c.jpg",
                Desc     = "Thiên đường vui chơi giải trí VinWonders Nha Trang với hàng trăm trò chơi hấp dẫn, công viên nước và các hoạt động trải nghiệm độc đáo bên bờ biển.",
                Start    = new DateTime(2026, 7, 1, 2, 0, 0, DateTimeKind.Utc),
                Venue    = "VinWonders, Nha Trang",
                Category = "Tham quan",
                IsHot = false, IsSpecial = true,
                Tickets  = new[] {
                    ("Vé Người Lớn",  "Vào cửa toàn khu + trò chơi không giới hạn", 500_000m, 1000),
                    ("Vé Trẻ Em",    "Dành cho trẻ dưới 140cm",                     350_000m,  500),
                }
            },
            new {
                Title    = "Ngắm nhìn bầu trời đêm - Đài Thiên Văn Nha Trang",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/Upload/eventcover/2024/03/18/437B6C.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Trải nghiệm ngắm bầu trời đêm qua kính thiên văn chuyên nghiệp tại Đài Thiên Văn Nha Trang. Hướng dẫn viên khoa học giải thích về các chòm sao và hành tinh.",
                Start    = new DateTime(2026, 6, 20, 13, 0, 0, DateTimeKind.Utc),
                Venue    = "Đài Thiên Văn, Nha Trang",
                Category = "Tham quan",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Vé Người Lớn", "Ngắm sao + thuyết minh khoa học", 139_000m, 50),
                    ("Vé Trẻ Em",    "Dành cho trẻ dưới 12 tuổi",        99_000m, 30),
                }
            },
            new {
                Title    = "DU LỊCH VĂN HÓA SUỐI TIÊN - LỄ HỘI TRÁI CÂY NAM BỘ 2026",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/3a/5b/ac/2a1770e5ad677efac8d68ffd9dd48ea0.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/10/f1/b2/a317ce6bb7f43ac5ea637e795d66623c.jpg",
                Desc     = "Lễ hội trái cây Nam Bộ 2026 tại Khu Du lịch Văn hóa Suối Tiên với hàng trăm loại trái cây đặc sản, các tiết mục văn nghệ và trò chơi dân gian.",
                Start    = new DateTime(2026, 6, 25, 3, 0, 0, DateTimeKind.Utc),
                Venue    = "Khu Du lịch Suối Tiên, TP. Hồ Chí Minh",
                Category = "Tham quan",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Vé Người Lớn", "Vào cửa toàn khu lễ hội",          60_000m, 3000),
                    ("Vé Trẻ Em",    "Dành cho trẻ dưới 10 tuổi",         30_000m, 1500),
                }
            },
            new {
                Title    = "ĐUA XE GO-KART CITY PARK",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/d7/e7/f7/44b4ab809ab3e945f80730175c343f31.jpg",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/df/73/58/48093f2ebde108ffb8ae51fe702b1fcb.jpg",
                Desc     = "Trải nghiệm đua xe Go-Kart tốc độ cao tại City Park. Đường đua chuyên nghiệp, xe hiện đại, phù hợp cho mọi lứa tuổi từ 8 tuổi trở lên.",
                Start    = new DateTime(2026, 7, 5, 2, 0, 0, DateTimeKind.Utc),
                Venue    = "City Park, TP. Hồ Chí Minh",
                Category = "Tham quan",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("10 vòng",  "Đua 10 vòng đường đua tiêu chuẩn",   120_000m, 100),
                    ("20 vòng",  "Đua 20 vòng + 1 lần pit stop",        220_000m,  50),
                }
            },
            // ========== THỂ THAO ==========
            new {
                Title    = "VBA 2026 - Saigon Heat vs Da Nang Dragons",
                ImageUrl = "https://images.tkbcdn.com/2/608/332/ts/ds/94/57/44/ba7b3310617240a4c5777bd92152347c.png",
                BannerUrl = "https://salt.tkbcdn.com/ts/ds/31/08/08/91679d2bd0ebcae1c7184955ef96d7b8.png",
                Desc     = "Trận đấu bóng rổ hấp dẫn trong khuôn khổ VBA 2026 giữa Saigon Heat và Da Nang Dragons. Đến sân cổ vũ cho đội nhà trong bầu không khí sôi động!",
                Start    = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
                Venue    = "Nhà thi đấu Phú Thọ, TP. Hồ Chí Minh",
                Category = "Thể thao",
                IsHot = false, IsSpecial = false,
                Tickets  = new[] {
                    ("Vé Phổ Thông", "Khán đài thường",                  100_000m, 800),
                    ("Vé VIP",       "Ghế VIP khu trung tâm + đồ uống",  250_000m, 100),
                }
            },
        };

        foreach (var e in events)
        {
            if (await db.Events.AnyAsync(x => x.Title == e.Title)) continue;

            var imageUrl = !string.IsNullOrEmpty(e.ImageUrl) ? e.ImageUrl : e.BannerUrl;
            var evt = new Event
            {
                Title       = e.Title,
                Description = e.Desc,
                StartDate   = e.Start,
                Venue       = e.Venue,
                Category    = e.Category,
                ImageUrl    = imageUrl,
                IsActive    = true,
                IsHot       = e.IsHot,
                IsSpecial   = e.IsSpecial,
                CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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

    private static async Task SeedFooterSettingsAsync(AppDbContext db)
    {
        if (await db.FooterSettings.AnyAsync()) return;

        db.FooterSettings.Add(new FooterSettings
        {
            Hotline = "1900 6208",
            SupportHours = "8:00 – 22:00 (Thứ 2 – CN)",
            Email = "support@tickethub.vn",
            Address = "Hà Nội & TP. Hồ Chí Minh, Việt Nam",
            BusinessLicense = "0123456789",
            LicenseDate = "01/01/2024",
            LicensePlace = "Hà Nội",
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
