# TicketHub — Hệ Thống Quản Lý Bán Vé Sự Kiện Tích Hợp AI

Đề tài đồ án tốt nghiệp: **Xây dựng hệ thống quản lý bán vé sự kiện tích hợp AI dự đoán nhu cầu**

## Công Nghệ Sử Dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core 10 Razor Pages |
| ORM | Entity Framework Core 10 |
| Cơ sở dữ liệu | SQL Server (LocalDB cho dev, SQL Server 2022 cho Docker) |
| Xác thực | ASP.NET Core Identity |
| AI/ML | ML.NET 5.0 (SDCA Regression) |
| Biểu đồ | Chart.js 4.4 |
| Giao diện | Bootstrap 5 |
| Container | Docker + Docker Compose |

## Tính Năng

### Dành cho Quản Trị Viên (Admin)
- Quản lý sự kiện: tạo, chỉnh sửa, xem danh sách, tìm kiếm và lọc theo danh mục
- Quản lý loại vé và giá vé cho từng sự kiện
- Quản lý đơn hàng: xem danh sách, chi tiết, trạng thái thanh toán
- Quản lý khách hàng: danh sách, tìm kiếm, xem lịch sử đặt vé
- Báo cáo doanh thu: tổng quan, biểu đồ theo ngày/tháng/sự kiện (Chart.js)
- Dự đoán AI: mô hình ML.NET tự động huấn luyện, dự đoán lượng vé bán cho sự kiện mới

### Dành cho Người Dùng (User)
- Đăng ký / đăng nhập tài khoản
- Xem danh sách và chi tiết sự kiện, lọc theo danh mục
- Đặt vé trực tuyến, nhận mã xác nhận
- Xem lịch sử đơn hàng cá nhân

## Tài Khoản Demo

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@tickethub.vn | Admin@123456 |
| User | nguyen.van.an@email.com | User@123456 |

## Hướng Dẫn Cài Đặt và Chạy

### Yêu cầu
- .NET 10 SDK
- SQL Server LocalDB (cài cùng Visual Studio hoặc SQL Server Express)

### Chạy trực tiếp

```bash
cd src/EventTicketSystem.Web
dotnet run --urls http://localhost:5134
```

Truy cập: http://localhost:5134

### Chạy bằng Docker Compose

```bash
docker compose up --build
```

Truy cập: http://localhost:8080

> Lần đầu chạy Docker, SQL Server cần ~30 giây để khởi động trước khi web app kết nối được.

## Cấu Trúc Dự Án

```
EventTicketSystem/
├── src/EventTicketSystem.Web/
│   ├── Data/              # DbContext, migrations, DbSeeder
│   ├── Models/            # Entity classes, ApplicationUser
│   ├── Pages/             # Razor Pages (Admin + User)
│   │   ├── Events/        # Danh sách, chi tiết, tạo, sửa sự kiện
│   │   ├── Orders/        # Đặt vé, xác nhận, lịch sử, quản lý
│   │   ├── Customers/     # Quản lý khách hàng (Admin)
│   │   ├── Reports/       # Báo cáo doanh thu (Admin)
│   │   ├── AI/            # Dự đoán AI (Admin)
│   │   └── Account/       # Đăng nhập, đăng ký
│   ├── Services/          # TicketPredictionService (ML.NET)
│   ├── Dockerfile
│   └── appsettings.json
├── docker-compose.yml
└── README.md
```
