# CI/CD Setup - Selenium Tests + Jira Integration

## Cấu trúc file

```
EventTicketSystem/
├── .github/
│   └── workflows/
│       └── selenium-tests.yml      # GitHub Actions workflow
├── tests/
│   └── selenium/
│       ├── TC01_homepage.test.js   # Test trang chủ
│       ├── TC02_login.test.js      # Test đăng nhập
│       └── TC03_events.test.js     # Test danh sách sự kiện
├── scripts/
│   └── create-jira-issues.js       # Script tạo Jira issue tự động
├── test-results/                   # Kết quả test (tự động tạo, không commit)
├── .env.example                    # Mẫu biến môi trường
└── package.json
```

## Cấu hình GitHub Secrets

Vào **Settings > Secrets and variables > Actions** trong repo GitHub, thêm 4 secrets:

| Secret | Giá trị |
|---|---|
| `JIRA_BASE_URL` | `https://your-domain.atlassian.net` |
| `JIRA_EMAIL` | Email tài khoản Jira |
| `JIRA_API_TOKEN` | API token lấy tại [Atlassian Account](https://id.atlassian.com/manage-profile/security/api-tokens) |
| `JIRA_PROJECT_KEY` | Key dự án Jira (ví dụ: `ETS`) |

## Chạy test local

### Bước 1: Cài packages
```bash
npm install
```

### Bước 2: Tạo file .env
```bash
cp .env.example .env
# Chỉnh sửa .env với giá trị thực
```

### Bước 3: Khởi động app ASP.NET Core
```bash
cd src/EventTicketSystem.Web
dotnet run
# App chạy tại http://localhost:5134
```

### Bước 4: Chạy Selenium tests (terminal khác)
```bash
mkdir test-results
npm test
```

### Bước 5: Xem kết quả
```
test-results/results.json
```

### Bước 6: Tạo Jira issue từ kết quả (tùy chọn)
```bash
node scripts/create-jira-issues.js
```

## Cách hoạt động của workflow

1. Trigger khi có push/PR vào nhánh `main` hoặc `master`
2. Khởi động SQL Server qua Docker service container
3. Build app ASP.NET Core và start ở background
4. Chờ app sẵn sàng (tối đa 90 giây)
5. Chạy 3 Selenium test case với Chrome headless
6. Nếu có test fail → gọi script tạo Jira issue
   - Issue chưa tồn tại → tạo Bug mới
   - Issue đã tồn tại → thêm comment với thông tin lần fail mới
7. Upload kết quả test lên GitHub Artifacts

## Jira issue logic

- Label `automated-test` được gắn vào tất cả issue do CI tạo
- Tiêu đề issue: `[Auto-Test FAIL] <tên test>`
- Tránh duplicate: kiểm tra issue cũ trước khi tạo mới
