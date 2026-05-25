# TESTCASE ĐẦY ĐỦ HỆ THỐNG TICKETHUB

**Dự án:** EventTicketSystem – TicketHub  
**Công nghệ:** ASP.NET Core 10 · Razor Pages · EF Core · SQL Server LocalDB · ASP.NET Core Identity  
**Phiên bản:** 1.0  
**Ngày:** 25/05/2026  
**Người thực hiện:** ___________________

---

> **Quy ước ký hiệu**
> - **C** = Điều kiện (Condition)
> - **A** = Hành động kết quả (Action)
> - **T** = True (điều kiện đúng)
> - **F** = False (điều kiện sai)
> - **B** = Boundary (giá trị biên)
> - **-** = Không quan tâm (don't care)
> - **Kết quả thực tế:** ✅ Đạt / ❌ Không đạt / ⚠️ Lỗi khác
> - **URL gốc:** http://localhost:5134

---

## MỤC LỤC

1. [Đăng Ký Tài Khoản](#1-đăng-ký-tài-khoản)
2. [Đăng Nhập](#2-đăng-nhập)
3. [Đổi Mật Khẩu](#3-đổi-mật-khẩu)
4. [Tạo Sự Kiện Mới](#4-tạo-sự-kiện-mới)
5. [Chỉnh Sửa Sự Kiện](#5-chỉnh-sửa-sự-kiện)
6. [Xóa Sự Kiện](#6-xóa-sự-kiện)
7. [Đặt Vé](#7-đặt-vé)
8. [Giỏ Hàng](#8-giỏ-hàng)
9. [Mã Giảm Giá](#9-mã-giảm-giá)
10. [Quản Lý Khách Hàng](#10-quản-lý-khách-hàng)
11. [Báo Cáo Doanh Thu](#11-báo-cáo-doanh-thu)
12. [Gửi Liên Hệ](#12-gửi-liên-hệ)
13. [Hòm Thư Admin](#13-hòm-thư-admin)
14. [AI Dự Đoán Nhu Cầu](#14-ai-dự-đoán-nhu-cầu)

---

---

## 1. Đăng Ký Tài Khoản

**URL:** `/Account/Register`  
**Phương thức:** GET (hiển thị form) / POST (xử lý đăng ký)  
**Quyền truy cập:** Tất cả (chưa đăng nhập)

### 1.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Họ và tên không trống | Có nhập | Để trống | — |
| C2 | Email có định dạng hợp lệ | abc@email.com | abc@, abcemail | — |
| C3 | Email chưa tồn tại trong DB | Email mới | Email đã dùng | — |
| C4 | Mật khẩu ≥ 6 ký tự | "Abc@12" (6 ký tự) | "Ab@1" (4 ký tự) | Đúng 6 ký tự |
| C5 | Mật khẩu có chữ hoa (A–Z) | "Abc@12" | "abc@12" | — |
| C6 | Mật khẩu có chữ số (0–9) | "Abc@12" | "Abc@ab" | — |
| C7 | Mật khẩu có ký tự đặc biệt (!@#…) | "Abc@12" | "Abcde1" | — |
| C8 | Xác nhận mật khẩu khớp với mật khẩu | Giống nhau | Khác nhau | — |

### 1.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Tạo tài khoản, gán role "User", đăng nhập tự động, chuyển hướng → `/` |
| A2 | Hiển thị lỗi: "Vui lòng nhập họ và tên" |
| A3 | Hiển thị lỗi: "Vui lòng nhập email" hoặc "Email không hợp lệ" |
| A4 | Hiển thị lỗi từ Identity (email đã tồn tại) |
| A5 | Hiển thị lỗi mật khẩu không đủ điều kiện (thiếu hoa/số/ký tự đặc biệt/quá ngắn) |
| A6 | Hiển thị lỗi: "Mật khẩu xác nhận không khớp" |

### 1.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | C6 | C7 | C8 | Hành động |
|----|----|----|----|----|----|----|----|----|-----------|
| TH1 | T | T | T | T | T | T | T | T | A1 |
| TH2 | F | T | T | T | T | T | T | T | A2 |
| TH3 | T | F | — | — | — | — | — | — | A3 |
| TH4 | T | T | F | T | T | T | T | T | A4 |
| TH5 | T | T | T | F | — | — | — | T | A5 |
| TH6 | T | T | T | T | F | — | — | T | A5 |
| TH7 | T | T | T | T | T | F | — | T | A5 |
| TH8 | T | T | T | T | T | T | F | T | A5 |
| TH9 | T | T | T | T | T | T | T | F | A6 |
| TH10 | T | T | T | B(6) | T | T | T | T | A1 |

### 1.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-01-01 | Đăng ký thành công | Nhập: HoTen="Nguyễn Văn A", Email="newuser@test.com", MK="Test@123", XácNhận="Test@123" → Nhấn Đăng Ký | Tài khoản được tạo, tự đăng nhập, chuyển về trang chủ `/` | | TH1 |
| TC-01-02 | Bỏ trống Họ và Tên | Để trống HoTen, nhập email+mật khẩu hợp lệ → Nhấn Đăng Ký | Hiển thị lỗi "Vui lòng nhập họ và tên", không tạo tài khoản | | TH2 |
| TC-01-03 | Email không hợp lệ – thiếu @ | Nhập Email="abcemail.com" → Nhấn Đăng Ký | Hiển thị lỗi "Email không hợp lệ" | | TH3 |
| TC-01-04 | Email không hợp lệ – để trống | Để trống Email → Nhấn Đăng Ký | Hiển thị lỗi "Vui lòng nhập email" | | TH3 |
| TC-01-05 | Email đã tồn tại | Nhập Email đã có trong DB (vd: admin@tickethub.vn) → Nhấn Đăng Ký | Hiển thị lỗi do Identity (email đã được sử dụng) | | TH4 |
| TC-01-06 | Mật khẩu quá ngắn (5 ký tự) | Nhập MK="Ab@1x" (5 ký tự) → Nhấn Đăng Ký | Hiển thị lỗi "Mật khẩu ít nhất 6 ký tự" | | TH5 |
| TC-01-07 | Mật khẩu đúng 6 ký tự (biên) | Nhập MK="Ab@123" (6 ký tự) → Nhấn Đăng Ký | Đăng ký thành công | | TH10 – Biên dưới |
| TC-01-08 | Mật khẩu thiếu chữ hoa | Nhập MK="abc@123" → Nhấn Đăng Ký | Hiển thị lỗi yêu cầu chữ hoa | | TH6 |
| TC-01-09 | Mật khẩu thiếu chữ số | Nhập MK="Abcdef@" → Nhấn Đăng Ký | Hiển thị lỗi yêu cầu chữ số | | TH7 |
| TC-01-10 | Mật khẩu thiếu ký tự đặc biệt | Nhập MK="Abcdef1" → Nhấn Đăng Ký | Hiển thị lỗi yêu cầu ký tự đặc biệt | | TH8 |
| TC-01-11 | Xác nhận mật khẩu không khớp | MK="Test@123", XácNhận="Test@456" → Nhấn Đăng Ký | Hiển thị lỗi "Mật khẩu xác nhận không khớp" | | TH9 |
| TC-01-12 | Bỏ trống toàn bộ form | Không nhập gì → Nhấn Đăng Ký | Hiển thị lỗi cho tất cả trường bắt buộc | | Edge case |
| TC-01-13 | Số điện thoại để trống (tùy chọn) | Để trống SĐT, các trường khác hợp lệ → Nhấn Đăng Ký | Đăng ký thành công (SĐT là tùy chọn) | | Optional field |
| TC-01-14 | Số điện thoại có giá trị | Nhập SĐT="0901234567", các trường khác hợp lệ → Nhấn Đăng Ký | Đăng ký thành công, SĐT được lưu | | Optional field |

---

## 2. Đăng Nhập

**URL:** `/Account/Login`  
**Phương thức:** GET / POST  
**Quyền truy cập:** Tất cả (chưa đăng nhập)

### 2.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Email không trống | Có nhập | Để trống | — |
| C2 | Email có định dạng hợp lệ | abc@email.com | abcmail | — |
| C3 | Email tồn tại trong hệ thống | Email đã đăng ký | Email không có | — |
| C4 | Mật khẩu không trống | Có nhập | Để trống | — |
| C5 | Mật khẩu đúng với tài khoản | Đúng | Sai | — |
| C6 | Chọn "Ghi nhớ đăng nhập" | Có check | Không check | — |
| C7 | Có returnUrl | Có URL | Không có | — |

### 2.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Đăng nhập thành công, chuyển hướng về returnUrl hoặc `/` |
| A2 | Hiển thị lỗi: "Vui lòng nhập email" |
| A3 | Hiển thị lỗi: "Email không hợp lệ" |
| A4 | Hiển thị lỗi: "Vui lòng nhập mật khẩu" |
| A5 | Hiển thị lỗi: "Email hoặc mật khẩu không đúng." |
| A6 | Đăng nhập thành công, cookie persistent (lâu dài) |
| A7 | Đăng nhập thành công, chuyển hướng về returnUrl |

### 2.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | Hành động |
|----|----|----|----|----|----|----|
| TH1 | T | T | T | T | T | A1 |
| TH2 | F | — | — | — | — | A2 |
| TH3 | T | F | — | — | — | A3 |
| TH4 | T | T | — | F | — | A4 |
| TH5 | T | T | F | T | — | A5 |
| TH6 | T | T | T | T | F | A5 |

### 2.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-02-01 | Đăng nhập thành công – User | Email="nguyen.van.an@email.com", MK="User@123456" → Nhấn Đăng Nhập | Đăng nhập thành công, về trang chủ, hiển thị tên user | | TH1 |
| TC-02-02 | Đăng nhập thành công – Admin | Email="admin@tickethub.vn", MK="Admin@123456" → Nhấn Đăng Nhập | Đăng nhập thành công, có nút "+ Tạo sự kiện" | | TH1 – Admin role |
| TC-02-03 | Để trống Email | Để trống Email, nhập MK → Nhấn Đăng Nhập | Hiển thị lỗi "Vui lòng nhập email" | | TH2 |
| TC-02-04 | Email sai định dạng | Email="abcdef", MK="Test@123" → Nhấn Đăng Nhập | Hiển thị lỗi "Email không hợp lệ" | | TH3 |
| TC-02-05 | Để trống mật khẩu | Email hợp lệ, để trống MK → Nhấn Đăng Nhập | Hiển thị lỗi "Vui lòng nhập mật khẩu" | | TH4 |
| TC-02-06 | Email không tồn tại | Email="notexist@abc.com", MK="Test@123" → Nhấn Đăng Nhập | Hiển thị lỗi "Email hoặc mật khẩu không đúng." | | TH5 |
| TC-02-07 | Mật khẩu sai | Email đúng, MK sai → Nhấn Đăng Nhập | Hiển thị lỗi "Email hoặc mật khẩu không đúng." | | TH6 |
| TC-02-08 | Ghi nhớ đăng nhập | Email+MK đúng, check "Ghi nhớ đăng nhập" → Nhấn Đăng Nhập | Đăng nhập thành công, cookie persistent | | C6=T |
| TC-02-09 | Không ghi nhớ | Email+MK đúng, không check "Ghi nhớ" → Nhấn Đăng Nhập | Đăng nhập thành công, session cookie | | C6=F |
| TC-02-10 | Đăng nhập với returnUrl | Truy cập `/Account/ChangePassword` khi chưa login → hệ thống redirect về Login với returnUrl → Đăng nhập | Sau khi đăng nhập chuyển về `/Account/ChangePassword` | | C7=T |
| TC-02-11 | Bỏ trống toàn bộ form | Không nhập gì → Nhấn Đăng Nhập | Hiển thị lỗi cho email và mật khẩu | | Edge case |
| TC-02-12 | Phân biệt hoa thường MK | Email đúng, MK đúng nhưng đổi case ("test@123" thay vì "Test@123") → Nhấn Đăng Nhập | Hiển thị lỗi "Email hoặc mật khẩu không đúng." | | Case-sensitive |

---

## 3. Đổi Mật Khẩu

**URL:** `/Account/ChangePassword`  
**Phương thức:** GET / POST  
**Quyền truy cập:** [Authorize] – phải đăng nhập

### 3.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng đã đăng nhập | Có | Không | — |
| C2 | Mật khẩu hiện tại không trống | Có nhập | Để trống | — |
| C3 | Mật khẩu hiện tại đúng với DB | Đúng | Sai | — |
| C4 | Mật khẩu mới không trống | Có nhập | Để trống | — |
| C5 | Mật khẩu mới ≥ 6 ký tự | "Ab@123" | "Ab@1" | Đúng 6 ký tự |
| C6 | Mật khẩu mới có chữ hoa | T | F | — |
| C7 | Mật khẩu mới có chữ số | T | F | — |
| C8 | Mật khẩu mới có ký tự đặc biệt | T | F | — |
| C9 | Xác nhận MK mới khớp | T | F | — |

### 3.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Đổi mật khẩu thành công, hiển thị trạng thái thành công trên cùng trang |
| A2 | Chuyển hướng về `/Account/Login` (chưa đăng nhập) |
| A3 | Hiển thị lỗi: "Vui lòng nhập mật khẩu hiện tại." |
| A4 | Hiển thị lỗi: "Mật khẩu hiện tại không đúng." |
| A5 | Hiển thị lỗi: "Vui lòng nhập mật khẩu mới." |
| A6 | Hiển thị lỗi mật khẩu mới không đủ điều kiện |
| A7 | Hiển thị lỗi: "Mật khẩu xác nhận không khớp." |

### 3.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | C6 | C7 | C8 | C9 | Hành động |
|----|----|----|----|----|----|----|----|----|----|----|
| TH1 | T | T | T | T | T | T | T | T | T | A1 |
| TH2 | F | — | — | — | — | — | — | — | — | A2 |
| TH3 | T | F | — | — | — | — | — | — | — | A3 |
| TH4 | T | T | F | T | T | T | T | T | T | A4 |
| TH5 | T | T | T | F | — | — | — | — | — | A5 |
| TH6 | T | T | T | T | F | — | — | — | T | A6 |
| TH7 | T | T | T | T | T | F | — | — | T | A6 |
| TH8 | T | T | T | T | T | T | F | — | T | A6 |
| TH9 | T | T | T | T | T | T | T | F | T | A6 |
| TH10 | T | T | T | T | T | T | T | T | F | A7 |
| TH11 | T | T | T | B(6) | T | T | T | T | T | A1 |

### 3.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-03-01 | Đổi mật khẩu thành công | Đăng nhập → `/Account/ChangePassword` → MKHiện="User@123456", MKMới="NewPass@789", XácNhận="NewPass@789" → Nhấn Đổi | Hiển thị thông báo "Đổi mật khẩu thành công!" | | TH1 |
| TC-03-02 | Chưa đăng nhập truy cập | Truy cập `/Account/ChangePassword` khi chưa đăng nhập | Chuyển hướng về `/Account/Login?returnUrl=...` | | TH2 |
| TC-03-03 | Để trống MK hiện tại | Để trống "Mật khẩu hiện tại" → Nhấn Đổi | Hiển thị lỗi "Vui lòng nhập mật khẩu hiện tại." | | TH3 |
| TC-03-04 | MK hiện tại sai | Nhập MK hiện tại="SaiMatKhau@1" → Nhấn Đổi | Hiển thị lỗi "Mật khẩu hiện tại không đúng." | | TH4 |
| TC-03-05 | Để trống MK mới | MK hiện tại đúng, để trống MK mới → Nhấn Đổi | Hiển thị lỗi "Vui lòng nhập mật khẩu mới." | | TH5 |
| TC-03-06 | MK mới quá ngắn (5 ký tự) | MK mới="Ab@1x" (5 ký tự) → Nhấn Đổi | Hiển thị lỗi "Mật khẩu mới phải có ít nhất 6 ký tự." | | TH6 |
| TC-03-07 | MK mới đúng 6 ký tự (biên) | MK mới="Ab@123" (6 ký tự) → Nhấn Đổi | Đổi thành công | | TH11 – Biên |
| TC-03-08 | MK mới thiếu chữ hoa | MK mới="abc@123" → Nhấn Đổi | Hiển thị lỗi "Mật khẩu phải chứa ít nhất một chữ hoa (A–Z)." | | TH7 |
| TC-03-09 | MK mới thiếu chữ số | MK mới="Abcdef@" → Nhấn Đổi | Hiển thị lỗi "Mật khẩu phải chứa ít nhất một chữ số (0–9)." | | TH8 |
| TC-03-10 | MK mới thiếu ký tự đặc biệt | MK mới="Abcdef1" → Nhấn Đổi | Hiển thị lỗi "Mật khẩu phải chứa ít nhất một ký tự đặc biệt (!, @, #…)." | | TH9 |
| TC-03-11 | Xác nhận MK không khớp | MKMới="Test@123", XácNhận="Test@456" → Nhấn Đổi | Hiển thị lỗi "Mật khẩu xác nhận không khớp." | | TH10 |
| TC-03-12 | Đăng nhập lại sau đổi MK | Sau TC-03-01 → Đăng xuất → Đăng nhập lại với MK mới "NewPass@789" | Đăng nhập thành công với MK mới | | Kiểm tra tính bền vững |
| TC-03-13 | Đăng nhập MK cũ sau đổi | Sau TC-03-01 → Đăng xuất → Đăng nhập bằng MK cũ "User@123456" | Lỗi "Email hoặc mật khẩu không đúng." | | Bảo mật |
| TC-03-14 | Bỏ trống toàn bộ form | Không nhập gì → Nhấn Đổi | Hiển thị lỗi cho tất cả trường bắt buộc | | Edge case |

---

## 4. Tạo Sự Kiện Mới

**URL:** `/Events/Create`  
**Phương thức:** GET / POST  
**Quyền truy cập:** [Authorize(Roles = "Admin")] – chỉ Admin

### 4.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng là Admin | T | F (User thường) | — |
| C2 | Tên sự kiện không trống | Có nhập | Để trống | 200 ký tự |
| C3 | Ngày bắt đầu không trống | Có chọn | Để trống | — |
| C4 | Địa điểm không trống | Có nhập | Để trống | 300 ký tự |
| C5 | Có ít nhất một loại vé với tên hợp lệ | T | F | — |
| C6 | Giá vé hợp lệ (0–100,000,000) | Trong khoảng | Âm số | 0 và 100,000,000 |
| C7 | Số lượng vé hợp lệ (1–1,000,000) | Trong khoảng | < 1 | 1 và 1,000,000 |
| C8 | Ảnh banner được tải lên | Có file | Không có | — |
| C9 | Ảnh banner đúng định dạng (.jpg/.jpeg/.png/.gif/.webp) | T | .pdf, .exe | — |

### 4.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Tạo sự kiện thành công, chuyển hướng → `/Events/Details?id={id}` |
| A2 | Chuyển hướng về trang đăng nhập (không phải Admin) |
| A3 | Hiển thị lỗi: "Vui lòng nhập tên sự kiện" |
| A4 | Hiển thị lỗi: "Vui lòng chọn ngày bắt đầu" |
| A5 | Hiển thị lỗi: "Vui lòng nhập địa điểm" |
| A6 | Hiển thị lỗi: "Vui lòng thêm ít nhất một loại vé." |
| A7 | Hiển thị lỗi: "Chỉ chấp nhận file ảnh (.jpg, .png, .gif, .webp)." |

### 4.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | C8 | C9 | Hành động |
|----|----|----|----|----|----|----|----|----|
| TH1 | T | T | T | T | T | F | — | A1 |
| TH2 | T | T | T | T | T | T | T | A1 |
| TH3 | F | — | — | — | — | — | — | A2 |
| TH4 | T | F | T | T | T | — | — | A3 |
| TH5 | T | T | F | T | T | — | — | A4 |
| TH6 | T | T | T | F | T | — | — | A5 |
| TH7 | T | T | T | T | F | — | — | A6 |
| TH8 | T | T | T | T | T | T | F | A7 |

### 4.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-04-01 | Tạo sự kiện đủ thông tin (không có ảnh) | Đăng nhập Admin → `/Events/Create` → Nhập: Tên="Festival Âm Nhạc 2026", NgàyBĐ=tương lai, ĐiạĐiểm="Hà Nội", 1 loại vé "VIP" giá 500000 SL=100 → Lưu | Sự kiện được tạo, chuyển về trang chi tiết | | TH1 |
| TC-04-02 | Tạo sự kiện có ảnh banner hợp lệ | Thêm ảnh banner .jpg → Lưu | Sự kiện tạo thành công, banner hiển thị | | TH2 |
| TC-04-03 | Truy cập khi không phải Admin | Đăng nhập User thường → Truy cập `/Events/Create` | Chuyển hướng về trang login hoặc Access Denied | | TH3 |
| TC-04-04 | Để trống tên sự kiện | Admin → Để trống Tên → Nhấn Lưu | Hiển thị lỗi "Vui lòng nhập tên sự kiện" | | TH4 |
| TC-04-05 | Để trống ngày bắt đầu | Admin → Để trống NgàyBĐ → Nhấn Lưu | Hiển thị lỗi "Vui lòng chọn ngày bắt đầu" | | TH5 |
| TC-04-06 | Để trống địa điểm | Admin → Để trống ĐịaĐiểm → Nhấn Lưu | Hiển thị lỗi "Vui lòng nhập địa điểm" | | TH6 |
| TC-04-07 | Không có loại vé | Admin → Nhập đủ thông tin sự kiện, xóa tất cả loại vé → Nhấn Lưu | Hiển thị lỗi "Vui lòng thêm ít nhất một loại vé." | | TH7 |
| TC-04-08 | Ảnh banner sai định dạng | Admin → Upload file .pdf làm banner → Nhấn Lưu | Hiển thị lỗi "Chỉ chấp nhận file ảnh (.jpg, .png, .gif, .webp)." | | TH8 |
| TC-04-09 | Giá vé = 0 (biên dưới) | Admin → Nhập giá vé = 0 → Lưu | Tạo thành công (miễn phí) | | Boundary C6 |
| TC-04-10 | Số lượng = 1 (biên dưới) | Admin → Nhập SL vé = 1 → Lưu | Tạo thành công | | Boundary C7 |
| TC-04-11 | Tên sự kiện đúng 200 ký tự (biên) | Admin → Nhập Tên = 200 ký tự → Lưu | Tạo thành công | | Boundary C2 |
| TC-04-12 | Tên sự kiện 201 ký tự (vượt biên) | Admin → Nhập Tên = 201 ký tự → Lưu | Lỗi validation MaxLength | | Over boundary |
| TC-04-13 | Nhiều loại vé | Admin → Thêm 3 loại vé: Thường/VIP/VVIP → Lưu | Tạo thành công với 3 loại vé | | Multiple tickets |
| TC-04-14 | Upload nhiều ảnh gallery | Admin → Upload 3 ảnh gallery .jpg → Lưu | Sự kiện tạo thành công, 3 ảnh gallery lưu | | Gallery images |
| TC-04-15 | Ảnh gallery sai định dạng | Admin → Upload ảnh gallery .bmp → Lưu | File .bmp bị bỏ qua (skip), sự kiện vẫn tạo thành công | | Soft fail |

---

## 5. Chỉnh Sửa Sự Kiện

**URL:** `/Events/Edit?id={id}`  
**Phương thức:** GET / POST  
**Quyền truy cập:** [Authorize(Roles = "Admin")]

### 5.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng là Admin | T | F | — |
| C2 | Sự kiện tồn tại theo ID | T | F | — |
| C3 | Tên sự kiện không trống | T | F | 200 ký tự |
| C4 | Ngày bắt đầu không trống | T | F | — |
| C5 | Địa điểm không trống | T | F | — |
| C6 | Ảnh mới đúng định dạng | T | F (.pdf) | — |
| C7 | Chọn "Xóa ảnh hiện tại" | T | F | — |
| C8 | Loại vé có SoldQuantity > 0 khi xóa | T | F | — |
| C9 | TotalQuantity mới < SoldQuantity | T | F | = SoldQuantity |

### 5.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Lưu chỉnh sửa thành công, chuyển về trang chi tiết |
| A2 | Từ chối truy cập (không phải Admin) |
| A3 | NotFound (sự kiện không tồn tại) |
| A4 | Hiển thị lỗi trường bắt buộc |
| A5 | Hiển thị lỗi định dạng ảnh |
| A6 | Xóa ảnh, set imageUrl = null |
| A7 | Loại vé có SoldQty > 0 KHÔNG bị xóa (bảo toàn) |
| A8 | TotalQuantity được clamped = Max(mới, SoldQuantity) |

### 5.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | C6 | C7 | C8 | Hành động |
|----|----|----|----|----|----|----|----|----|-----------|
| TH1 | T | T | T | T | T | — | F | — | A1 |
| TH2 | F | — | — | — | — | — | — | — | A2 |
| TH3 | T | F | — | — | — | — | — | — | A3 |
| TH4 | T | T | F | — | — | — | — | — | A4 |
| TH5 | T | T | T | — | — | F | — | — | A5 |
| TH6 | T | T | T | T | T | — | T | — | A6 |
| TH7 | T | T | T | T | T | — | — | T | A7 |

### 5.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-05-01 | Chỉnh sửa thành công | Admin → `/Events/Edit?id={id}` → Đổi Tên → Nhấn Lưu | Thông tin được cập nhật, chuyển về Details | | TH1 |
| TC-05-02 | Truy cập khi không phải Admin | User thường → `/Events/Edit?id={id}` | Từ chối truy cập | | TH2 |
| TC-05-03 | Sự kiện không tồn tại | Admin → `/Events/Edit?id=99999` | Trả về 404 Not Found | | TH3 |
| TC-05-04 | Xóa tên sự kiện | Admin → Xóa nội dung Tên → Nhấn Lưu | Lỗi "Vui lòng nhập tên sự kiện" | | TH4 |
| TC-05-05 | Upload ảnh sai định dạng | Admin → Upload banner .exe → Nhấn Lưu | Lỗi "Chỉ chấp nhận .jpg .png .gif .webp" | | TH5 |
| TC-05-06 | Xóa ảnh hiện tại | Admin → Check "Xóa ảnh hiện tại" → Nhấn Lưu | Ảnh bị xóa, sự kiện không có banner | | TH6 |
| TC-05-07 | Xóa loại vé đã bán | Admin → Đánh dấu xóa loại vé có SoldQty > 0 → Nhấn Lưu | Loại vé KHÔNG bị xóa (bảo toàn dữ liệu) | | TH7 |
| TC-05-08 | Xóa loại vé chưa bán | Admin → Đánh dấu xóa loại vé SoldQty=0 → Nhấn Lưu | Loại vé bị xóa thành công | | SoldQty=0 |
| TC-05-09 | Giảm SL xuống dưới đã bán | Admin → Đặt TotalQty thấp hơn SoldQty (vd: đã bán 50, đặt TotalQty=30) → Nhấn Lưu | TotalQty được set = SoldQty (50), không thấp hơn | | TH C9=T |
| TC-05-10 | Thêm loại vé mới khi chỉnh sửa | Admin → Thêm 1 loại vé mới trong form Edit → Nhấn Lưu | Loại vé mới được thêm vào sự kiện | | Add new ticket |
| TC-05-11 | Bật/Tắt trạng thái IsActive | Admin → Uncheck "Hiển thị sự kiện" → Nhấn Lưu | Sự kiện ẩn khỏi danh sách công khai | | IsActive toggle |
| TC-05-12 | Thêm ảnh gallery mới | Admin → Upload 2 ảnh gallery mới → Nhấn Lưu | 2 ảnh được thêm vào gallery | | Gallery |
| TC-05-13 | Xóa ảnh gallery | Admin → Check xóa 1 ảnh gallery → Nhấn Lưu | Ảnh được xóa khỏi gallery và file system | | Gallery delete |

---

## 6. Xóa Sự Kiện

**URL:** POST `/Events/Index?handler=Delete`  
**Phương thức:** POST  
**Quyền truy cập:** [Authorize(Roles = "Admin")]

### 6.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng là Admin | T | F | — |
| C2 | Sự kiện tồn tại | T | F | — |
| C3 | Sự kiện có OrderItems liên quan | T | F | — |
| C4 | Sự kiện có ảnh banner | T | F | — |
| C5 | Sự kiện có ảnh gallery | T | F | — |

### 6.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Xóa sự kiện thành công (kèm OrderItems, ảnh), TempData success, chuyển về Index |
| A2 | Từ chối truy cập |
| A3 | Redirect về Index (sự kiện không tồn tại – silent fail) |
| A4 | Xóa cả OrderItems trước khi xóa sự kiện |
| A5 | Xóa file ảnh vật lý |

### 6.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | Hành động |
|----|----|----|----|----|----|----|
| TH1 | T | T | F | F | F | A1 |
| TH2 | T | T | T | T | T | A1 + A4 + A5 |
| TH3 | F | — | — | — | — | A2 |
| TH4 | T | F | — | — | — | A3 |

### 6.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-06-01 | Xóa sự kiện không có đơn hàng | Admin → `/Events/Index` → Nhấn Xóa sự kiện chưa có đơn hàng nào | Sự kiện bị xóa, hiển thị thông báo "Đã xóa sự kiện..." | | TH1 |
| TC-06-02 | Xóa sự kiện có đơn hàng và ảnh | Admin → Xóa sự kiện đã có vé bán, có banner + gallery | Xóa thành công: OrderItems, banner, gallery, sự kiện đều bị xóa | | TH2 |
| TC-06-03 | User thường xóa sự kiện | User thường → POST delete request | Bị từ chối truy cập (403 hoặc redirect) | | TH3 |
| TC-06-04 | Xóa sự kiện không tồn tại | Admin → POST với id=99999 | Redirect về Index (không crash) | | TH4 |
| TC-06-05 | File ảnh bị xóa khỏi disk | Admin → Xóa sự kiện có ảnh → Kiểm tra thư mục `/uploads/` | File ảnh không còn tồn tại trong thư mục | | File cleanup |
| TC-06-06 | Kiểm tra danh sách sau xóa | Xóa sự kiện → Vào `/Events/Index` | Sự kiện không còn xuất hiện trong danh sách | | UI verification |

---

## 7. Đặt Vé

**URL:** POST `/Events/Details?handler=Buy`  
**Phương thức:** POST (handler=Buy)  
**Quyền truy cập:** Tất cả (có thể đặt khi chưa đăng nhập)

### 7.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Loại vé hợp lệ (tồn tại, thuộc sự kiện) | T | F | — |
| C2 | Số lượng hợp lệ (1–10) | T | <1 hoặc >10 | 1, 10 |
| C3 | Số lượng ≤ AvailableQuantity | T | F | = AvailableQuantity |
| C4 | Họ tên không trống | T | F | — |
| C5 | Email không trống | T | F | — |
| C6 | Mã giảm giá được nhập | T | F | — |
| C7 | Mã giảm giá hợp lệ (active, chưa hết hạn, chưa dùng hết, đủ giá trị đơn tối thiểu) | T | F | — |
| C8 | Người dùng đã đăng nhập | T | F | — |

### 7.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Tạo đơn hàng Pending, chuyển → `/Orders/Payment?id={orderId}` |
| A2 | TempData error "Loại vé không hợp lệ.", redirect về Details |
| A3 | Số lượng bị clamp về [1, 10] |
| A4 | TempData error "Chỉ còn {n} vé loại '{name}'.", redirect |
| A5 | TempData error "Vui lòng điền đầy đủ họ tên và email.", redirect |
| A6 | Áp dụng giảm giá từ mã coupon |
| A7 | Không áp dụng giảm giá (mã không hợp lệ hoặc không nhập) |
| A8 | Gán ApplicationUserId vào đơn hàng (nếu đăng nhập) |

### 7.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | C6 | C7 | Hành động |
|----|----|----|----|----|----|----|----|----|
| TH1 | T | T | T | T | T | F | — | A1 + A7 |
| TH2 | T | T | T | T | T | T | T | A1 + A6 |
| TH3 | T | T | T | T | T | T | F | A1 + A7 |
| TH4 | F | — | — | — | — | — | — | A2 |
| TH5 | T | F(>10) | — | — | — | — | — | A3 (clamp 10) |
| TH6 | T | T | F | — | — | — | — | A4 |
| TH7 | T | T | T | F | T | — | — | A5 |
| TH8 | T | T | T | T | F | — | — | A5 |
| TH9 | T | B(=SL) | T(=) | T | T | — | — | A1 |

### 7.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-07-01 | Đặt vé thành công, không mã giảm giá | `/Events/Details?id={id}` → Chọn vé, nhập SL=2, HoTen="Nguyễn A", Email="a@test.com" → Đặt vé | Đơn hàng Pending tạo, chuyển tới trang thanh toán | | TH1 |
| TC-07-02 | Đặt vé có mã giảm giá hợp lệ | Nhập mã coupon hợp lệ vào ô mã → Đặt vé | Đơn hàng tạo với giảm giá, TotalAmount < OriginalAmount | | TH2 |
| TC-07-03 | Đặt vé mã giảm giá không hợp lệ | Nhập mã coupon hết hạn → Đặt vé | Đơn hàng tạo KHÔNG có giảm giá, TotalAmount = OriginalAmount | | TH3 |
| TC-07-04 | Loại vé không tồn tại | POST với ticketTypeId=99999 | Hiển thị lỗi "Loại vé không hợp lệ." | | TH4 |
| TC-07-05 | Số lượng = 0 (clamp về 1) | Qty=0 → Đặt vé | Hệ thống xử lý qty=1 (clamp) | | TH5 |
| TC-07-06 | Số lượng = 11 (clamp về 10) | Qty=11 → Đặt vé | Hệ thống xử lý qty=10 (clamp) | | TH5 |
| TC-07-07 | Số lượng = 1 (biên dưới) | Qty=1 → Đặt vé thành công | Đặt thành công 1 vé | | Boundary |
| TC-07-08 | Số lượng = 10 (biên trên) | Qty=10 → Đặt vé thành công | Đặt thành công 10 vé | | Boundary |
| TC-07-09 | Vượt số lượng khả dụng | Vé còn 3, Qty=5 → Đặt vé | Lỗi "Chỉ còn 3 vé loại '...'." | | TH6 |
| TC-07-10 | Đặt vé bằng số lượng khả dụng (biên) | Vé còn 5, Qty=5 → Đặt vé | Đặt thành công 5 vé | | TH9 |
| TC-07-11 | Để trống Họ tên | Để trống customerName → Đặt vé | Lỗi "Vui lòng điền đầy đủ họ tên và email." | | TH7 |
| TC-07-12 | Để trống Email | Để trống customerEmail → Đặt vé | Lỗi "Vui lòng điền đầy đủ họ tên và email." | | TH8 |
| TC-07-13 | Đặt vé khi đã đăng nhập | Đăng nhập User → Đặt vé | Đơn hàng có ApplicationUserId, hiển thị trong "Vé của tôi" | | C8=T |
| TC-07-14 | Đặt vé khi chưa đăng nhập | Không đăng nhập → Đặt vé | Đơn hàng tạo thành công (guest) không có ApplicationUserId | | C8=F |
| TC-07-15 | Thanh toán trong 5 phút | Đặt vé → Vào trang Payment → Xác nhận trong 5 phút | Đơn hàng Confirmed, chuyển về Confirmation | | Payment flow |
| TC-07-16 | Thanh toán sau 5 phút (hết hạn) | Đặt vé → Chờ >5 phút → Vào trang Payment | Đơn hàng tự hủy "Đơn hàng đã hết thời gian thanh toán và bị tự động hủy." | | Timeout |
| TC-07-17 | Hủy đơn hàng trên trang Payment | Đặt vé → Nhấn "Hủy" trên trang Payment | Đơn hàng Cancelled, số vé được hoàn trả | | Cancel |

---

## 8. Giỏ Hàng

**URL:** `/Cart/Index`  
**Phương thức:** GET / POST (handlers: UpdateQty, Remove, Clear, Checkout)  
**Quyền truy cập:** Tất cả (session-based)

### 8.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Giỏ hàng không trống | T | F | — |
| C2 | Số lượng mỗi loại vé ≤ AvailableQuantity | T | F | = AvailableQuantity |
| C3 | Số lượng thêm vào ≥ 1 và ≤ 10 | T | F | 1, 10 |
| C4 | Họ tên nhập đủ khi checkout | T | F | — |
| C5 | Email nhập đủ khi checkout | T | F | — |
| C6 | Có nhập mã giảm giá | T | F | — |
| C7 | Mã giảm giá hợp lệ | T | F | — |
| C8 | Tất cả vé trong giỏ còn đủ số lượng khi checkout | T | F | — |

### 8.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Thêm vào giỏ thành công, TempData "Đã thêm..." |
| A2 | Lỗi "Loại vé không hợp lệ." |
| A3 | Lỗi "Chỉ còn {n} vé." |
| A4 | Checkout thành công, tạo Orders, chuyển → `/Cart/Confirmation` |
| A5 | Lỗi "Giỏ hàng trống." |
| A6 | Lỗi "Vui lòng điền họ tên và email." |
| A7 | Lỗi "Vé '{name}' không còn đủ số lượng." |
| A8 | Giảm giá được phân bổ theo tỷ lệ cho từng order |
| A9 | Cập nhật số lượng trong giỏ |
| A10 | Xóa item khỏi giỏ |
| A11 | Xóa toàn bộ giỏ hàng |

### 8.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | C5 | C6 | C7 | C8 | Hành động |
|----|----|----|----|----|----|----|----|----|-----------|
| TH1 (Thêm) | — | T | T | — | — | — | — | — | A1 |
| TH2 (Thêm vượt SL) | — | F | — | — | — | — | — | — | A3 |
| TH3 (Checkout) | T | — | — | T | T | F | — | T | A4 |
| TH4 (Checkout+GG) | T | — | — | T | T | T | T | T | A4 + A8 |
| TH5 (Giỏ trống) | F | — | — | — | — | — | — | — | A5 |
| TH6 (Thiếu info) | T | — | — | F | — | — | — | — | A6 |
| TH7 (Vé hết) | T | — | — | T | T | — | — | F | A7 |

### 8.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-08-01 | Thêm 1 loại vé vào giỏ | `/Events/Details?id={id}` → Nhấn "Thêm vào giỏ", SL=2 | Thông báo "Đã thêm 2 vé '...' vào giỏ hàng!", badge giỏ hàng +2 | | TH1 |
| TC-08-02 | Thêm nhiều loại vé khác nhau | Thêm 2 loại vé từ 2 sự kiện khác nhau | Giỏ hàng có 2 dòng, subtotal = tổng | | Multiple items |
| TC-08-03 | Thêm vé vượt số lượng khả dụng | SL còn 3, thêm SL=5 | Lỗi "Chỉ còn 3 vé." | | TH2 |
| TC-08-04 | Xem giỏ hàng trống | Truy cập `/Cart/Index` khi giỏ trống | Hiển thị thông báo giỏ trống, không có danh sách | | Empty state |
| TC-08-05 | Cập nhật số lượng trong giỏ | Giỏ có 2 vé → Đổi SL thành 4 → Cập nhật | Giỏ hàng hiển thị SL=4, subtotal cập nhật | | UpdateQty |
| TC-08-06 | Cập nhật số lượng = 0 | Đặt SL=0 → Cập nhật | Item bị xóa khỏi giỏ | | Remove by qty=0 |
| TC-08-07 | Xóa một item khỏi giỏ | Nhấn nút Xóa cho 1 item | Item bị xóa, các item khác còn lại | | Remove single |
| TC-08-08 | Xóa toàn bộ giỏ hàng | Nhấn "Xóa giỏ hàng" | Giỏ hàng trống hoàn toàn | | Clear cart |
| TC-08-09 | Checkout thành công – không mã GG | Giỏ có 2 item → Nhập HoTen+Email → Nhấn Thanh Toán | Orders tạo thành công, chuyển về Confirmation | | TH3 |
| TC-08-10 | Checkout thành công – có mã GG | Giỏ có 2 item + mã giảm giá hợp lệ → Checkout | Orders tạo với giảm giá, TotalAmount < OriginalAmount | | TH4 |
| TC-08-11 | Checkout giỏ trống | Giỏ trống → POST Checkout | Lỗi "Giỏ hàng trống." | | TH5 |
| TC-08-12 | Checkout thiếu họ tên | Giỏ có item → Không nhập HoTen → Checkout | Lỗi "Vui lòng điền họ tên và email." | | TH6 |
| TC-08-13 | Checkout thiếu email | Giỏ có item → Không nhập Email → Checkout | Lỗi "Vui lòng điền họ tên và email." | | TH6 |
| TC-08-14 | Checkout khi vé vừa hết | Thêm vé cuối → Người khác mua hết → Checkout | Lỗi "Vé '...' không còn đủ số lượng." | | Race condition |
| TC-08-15 | Confirmation tự xác nhận đơn | Sau Checkout → Vào trang Confirmation | Tất cả orders chuyển từ Pending → Confirmed | | Auto-confirm |
| TC-08-16 | Badge giỏ hàng cập nhật | Thêm vé → Kiểm tra badge số trên navbar | Badge hiển thị đúng số lượng | | UI |
| TC-08-17 | Badge giỏ hàng sau xóa | Xóa item → Kiểm tra badge | Badge giảm tương ứng | | UI |

---

## 9. Mã Giảm Giá

**URL:** `/Coupons` (Admin), `/api/coupons/validate` (API)  
**Phương thức:** GET / POST  
**Quyền truy cập:** [Authorize(Roles = "Admin")] cho quản lý; Public cho API

### 9.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Mã coupon không trống | T | F | — |
| C2 | Mã coupon chưa tồn tại trong DB | T | F | — |
| C3 | Có giá trị giảm (% hoặc số tiền > 0) | T | F | > 0 |
| C4 | Coupon đang Active | T | F | — |
| C5 | Coupon chưa hết hạn (ExpiryDate) | T | F | = ngày hết hạn |
| C6 | Số lần dùng < MaxUses | T (UsedCount<MaxUses) | F (UsedCount≥MaxUses) | UsedCount=MaxUses-1 |
| C7 | Giá trị đơn hàng ≥ MinOrderValue | T | F | = MinOrderValue |
| C8 | Loại giảm giá (% hay số tiền cố định) | Percent | Fixed Amount | — |

### 9.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Tạo/cập nhật coupon thành công |
| A2 | Lỗi "Vui lòng nhập giá trị giảm (% hoặc số tiền)." |
| A3 | Lỗi "Mã giảm giá này đã tồn tại." |
| A4 | Coupon hợp lệ, trả về discount amount |
| A5 | Lỗi "Mã giảm giá không còn hiệu lực." |
| A6 | Lỗi "Mã giảm giá đã hết hạn." |
| A7 | Lỗi "Mã giảm giá đã được sử dụng hết." |
| A8 | Lỗi "Đơn hàng tối thiểu {n}đ để áp dụng mã này." |
| A9 | Lỗi "Mã giảm giá không tồn tại." |
| A10 | Xóa coupon thành công |
| A11 | Toggle trạng thái active/inactive |

### 9.3 Bảng Quyết Định – Tạo Coupon

| TH | C1 | C2 | C3 | Hành động |
|----|----|----|----|----|
| TH1 | T | T | T | A1 |
| TH2 | F | — | — | Lỗi required |
| TH3 | T | F | — | A3 |
| TH4 | T | T | F | A2 |

### 9.4 Bảng Quyết Định – Validate Coupon

| TH | C4 | C5 | C6 | C7 | Hành động |
|----|----|----|----|----|-------|
| TH1 | T | T | T | T | A4 |
| TH2 | F | — | — | — | A5 |
| TH3 | T | F | — | — | A6 |
| TH4 | T | T | F | — | A7 |
| TH5 | T | T | T | F | A8 |

### 9.5 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-09-01 | Tạo coupon giảm % thành công | Admin → `/Coupons/Create` → Code="SAVE20", Loại=%, Giảm=20%, MaxUses=100 → Lưu | Coupon tạo thành công, mã chuyển UPPERCASE | | TH1 |
| TC-09-02 | Tạo coupon giảm số tiền cố định | Admin → Code="FLAT50K", Loại=Số tiền, Giảm=50000đ → Lưu | Coupon tạo thành công | | Fixed amount |
| TC-09-03 | Mã trùng nhau | Tạo coupon code đã tồn tại → Lưu | Lỗi "Mã giảm giá này đã tồn tại." | | TH3 |
| TC-09-04 | Không nhập giá trị giảm | Admin → Code="TEST", không nhập % và số tiền → Lưu | Lỗi "Vui lòng nhập giá trị giảm (% hoặc số tiền)." | | TH4 |
| TC-09-05 | Chỉnh sửa coupon thành công | Admin → `/Coupons/Edit?id={id}` → Thay đổi % → Lưu | Coupon cập nhật thành công | | Edit |
| TC-09-06 | Xóa coupon | Admin → Nhấn Xóa → Xác nhận | Coupon bị xóa, thông báo "Đã xóa mã giảm giá." | | A10 |
| TC-09-07 | Toggle active/inactive | Admin → Nhấn bật/tắt trạng thái coupon | Trạng thái đổi ngược | | A11 |
| TC-09-08 | Validate – mã hợp lệ (API) | GET `/api/coupons/validate?code=SAVE20&total=200000` | JSON `{valid:true, discount:..., finalTotal:...}` | | A4 |
| TC-09-09 | Validate – mã không tồn tại (API) | GET `/api/coupons/validate?code=NOTEXIST&total=100000` | JSON `{valid:false, error:"Mã giảm giá không tồn tại."}` | | A9 |
| TC-09-10 | Validate – mã inactive | Coupon IsActive=false → Validate | JSON `{valid:false, error:"Mã giảm giá không còn hiệu lực."}` | | A5 |
| TC-09-11 | Validate – mã hết hạn | Coupon ExpiryDate đã qua → Validate | JSON `{valid:false, error:"Mã giảm giá đã hết hạn."}` | | A6 |
| TC-09-12 | Validate – hết lượt dùng | UsedCount=MaxUses → Validate | JSON `{valid:false, error:"Mã giảm giá đã được sử dụng hết."}` | | A7 |
| TC-09-13 | Validate – đơn hàng dưới mức tối thiểu | MinOrderValue=500000, total=100000 → Validate | JSON `{valid:false, error:"Đơn hàng tối thiểu 500,000đ..."}` | | A8 |
| TC-09-14 | Validate – đơn hàng đúng mức tối thiểu (biên) | MinOrderValue=500000, total=500000 → Validate | JSON `{valid:true, ...}` | | Boundary C7 |
| TC-09-15 | Validate – mã trống (API) | GET `/api/coupons/validate?code=&total=100000` | 400 BadRequest `{valid:false, error:"Vui lòng nhập mã giảm giá."}` | | Empty code |
| TC-09-16 | Tính giảm giá % | Mã 20%, đơn 200000 → Validate | discount=40000, finalTotal=160000 | | Calculation |
| TC-09-17 | Tính giảm giá cố định > tổng đơn | Mã giảm 200000đ, đơn 100000 → Apply | Discount capped = 100000, finalTotal=0 | | Cap at total |
| TC-09-18 | Mã chuyển thành uppercase tự động | Nhập code="save20" → Lưu | Trong DB lưu "SAVE20" | | Case normalization |

---

## 10. Quản Lý Khách Hàng

**URL:** `/Customers/Index`, `/Customers/Details?id={userId}`  
**Phương thức:** GET  
**Quyền truy cập:** [Authorize(Roles = "Admin")]

### 10.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng là Admin | T | F | — |
| C2 | Có từ khóa tìm kiếm | T | F | — |
| C3 | Từ khóa khớp HoTen | T | F | — |
| C4 | Từ khóa khớp Email | T | F | — |
| C5 | Từ khóa khớp SĐT | T | F | — |
| C6 | Khách hàng có đơn hàng Confirmed | T | F | — |
| C7 | ID khách hàng tồn tại | T | F | — |

### 10.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Hiển thị danh sách tất cả khách hàng |
| A2 | Hiển thị kết quả tìm kiếm |
| A3 | Hiển thị danh sách rỗng (không tìm thấy) |
| A4 | Từ chối truy cập |
| A5 | Hiển thị chi tiết khách hàng và lịch sử đơn hàng |
| A6 | 404 Not Found |

### 10.3 Bảng Quyết Định

| TH | C1 | C2 | C3/C4/C5 | Hành động |
|----|----|----|----------|-----------|
| TH1 | T | F | — | A1 |
| TH2 | T | T | T (bất kỳ) | A2 |
| TH3 | T | T | F (tất cả) | A3 |
| TH4 | F | — | — | A4 |

### 10.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-10-01 | Xem danh sách không lọc | Admin → `/Customers/Index` | Hiển thị tất cả user, có SĐon và TongTien | | TH1 |
| TC-10-02 | Tìm kiếm theo Họ tên | Admin → Nhập search="Nguyễn" → Tìm | Hiển thị khách hàng có Nguyễn trong tên | | TH2 – C3 |
| TC-10-03 | Tìm kiếm theo Email | Admin → Nhập search="@gmail.com" → Tìm | Hiển thị khách hàng có @gmail.com trong email | | TH2 – C4 |
| TC-10-04 | Tìm kiếm theo SĐT | Admin → Nhập search="0901" → Tìm | Hiển thị khách hàng có 0901 trong SĐT | | TH2 – C5 |
| TC-10-05 | Tìm kiếm không có kết quả | Admin → Nhập search="zzzzzzz" → Tìm | Danh sách rỗng, không có kết quả | | TH3 |
| TC-10-06 | User thường truy cập | User thường → `/Customers/Index` | Từ chối truy cập (403 hoặc redirect) | | TH4 |
| TC-10-07 | Xem chi tiết khách hàng | Admin → Click vào khách hàng hoặc `/Customers/Details?id={id}` | Hiển thị thông tin chi tiết + lịch sử đơn hàng | | A5 |
| TC-10-08 | Chi tiết khách hàng ID không tồn tại | Admin → `/Customers/Details?id=INVALID_ID` | 404 Not Found | | A6 |
| TC-10-09 | Số đơn hàng chính xác | Khách hàng có 3 Confirmed orders → Xem danh sách | SĐon=3, TongTien đúng | | C6=T |
| TC-10-10 | Khách hàng không có đơn hàng | Khách hàng mới đăng ký chưa đặt vé → Xem | SĐon=0, TongTien=0 | | C6=F |
| TC-10-11 | Sắp xếp theo NgayTao | Danh sách khách hàng → Kiểm tra thứ tự | Khách hàng mới nhất hiển thị trước | | Sort order |

---

## 11. Báo Cáo Doanh Thu

**URL:** `/Reports/Index`  
**Phương thức:** GET  
**Quyền truy cập:** [Authorize(Roles = "Admin")]

### 11.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng là Admin | T | F | — |
| C2 | Có đơn hàng Confirmed trong hệ thống | T | F | — |
| C3 | Có đơn hàng trong 30 ngày gần nhất | T | F | — |
| C4 | Có đơn hàng trong năm hiện tại | T | F | — |
| C5 | Có tối thiểu 1 sự kiện với đơn hàng Confirmed | T | F | — |

### 11.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Hiển thị tổng doanh thu, tổng đơn, tổng vé + biểu đồ |
| A2 | Từ chối truy cập |
| A3 | Hiển thị doanh thu = 0, biểu đồ rỗng |
| A4 | Biểu đồ theo ngày (30 ngày) |
| A5 | Biểu đồ theo tháng (năm hiện tại) |
| A6 | Biểu đồ top 8 sự kiện |

### 11.3 Bảng Quyết Định

| TH | C1 | C2 | Hành động |
|----|----|----|-----------|
| TH1 | T | T | A1 |
| TH2 | T | F | A3 |
| TH3 | F | — | A2 |

### 11.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-11-01 | Xem báo cáo khi có dữ liệu | Admin → `/Reports/Index` | Hiển thị TongDoanhThu, TongDonHang, TongVeBan đúng với dữ liệu Confirmed | | TH1 |
| TC-11-02 | User thường truy cập | User → `/Reports/Index` | Từ chối truy cập | | TH3 |
| TC-11-03 | Báo cáo không tính đơn Pending | Hệ thống có đơn Pending → Xem báo cáo | Đơn Pending KHÔNG được tính vào doanh thu | | Confirmed only |
| TC-11-04 | Báo cáo không tính đơn Cancelled | Hệ thống có đơn Cancelled → Xem báo cáo | Đơn Cancelled KHÔNG được tính | | Confirmed only |
| TC-11-05 | Biểu đồ theo ngày | Xem Reports → Kiểm tra biểu đồ "Theo Ngày" | Hiển thị dữ liệu 30 ngày gần nhất theo định dạng "dd/MM" | | A4 |
| TC-11-06 | Biểu đồ theo tháng | Xem Reports → Kiểm tra biểu đồ "Theo Tháng" | Hiển thị 12 tháng trong năm, chỉ tháng có doanh thu > 0 | | A5 |
| TC-11-07 | Biểu đồ top sự kiện | Xem Reports → Kiểm tra biểu đồ "Sự Kiện" | Hiển thị tối đa 8 sự kiện có doanh thu cao nhất | | A6 – Top 8 |
| TC-11-08 | Báo cáo khi hệ thống trống | Hệ thống không có đơn hàng nào | TongDoanhThu=0, TongDonHang=0, TongVeBan=0, biểu đồ trống | | TH2 |
| TC-11-09 | Tính đúng TongDoanhThu | Xác nhận 3 đơn: 100000, 200000, 300000 → Xem | TongDoanhThu = 600000 | | Calculation |

---

## 12. Gửi Liên Hệ

**URL:** `/Contact`  
**Phương thức:** GET / POST  
**Quyền truy cập:** [Authorize] – phải đăng nhập

### 12.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng đã đăng nhập | T | F | — |
| C2 | Người dùng là Admin | T | F | — |
| C3 | Chủ đề không trống | T | F | 200 ký tự |
| C4 | Lời nhắn không trống | T | F | 2000 ký tự |
| C5 | Số điện thoại được nhập | T | F | — |

### 12.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Lưu tin nhắn vào DB, hiển thị trạng thái thành công |
| A2 | Chuyển hướng về trang đăng nhập |
| A3 | Hiển thị thông báo "Dùng hòm thư Admin", không cho gửi |
| A4 | Lỗi "Vui lòng nhập chủ đề." |
| A5 | Lỗi "Vui lòng nhập lời nhắn." |
| A6 | Form hiển thị HoTen + Email readonly từ tài khoản |

### 12.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | C4 | Hành động |
|----|----|----|----|----|-----------|
| TH1 | T | F | T | T | A1 + A6 |
| TH2 | F | — | — | — | A2 |
| TH3 | T | T | — | — | A3 |
| TH4 | T | F | F | T | A4 |
| TH5 | T | F | T | F | A5 |

### 12.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-12-01 | Gửi tin nhắn thành công | User đăng nhập → `/Contact` → Nhập ChủĐề="Hỏi về vé", LờiNhắn="Xin chào..." → Gửi | Hiển thị "Tin nhắn đã được gửi thành công!", form ẩn | | TH1 |
| TC-12-02 | Chưa đăng nhập | Truy cập `/Contact` khi chưa login | Chuyển hướng về `/Account/Login?returnUrl=/Contact` | | TH2 |
| TC-12-03 | Admin truy cập | Admin đăng nhập → `/Contact` | Hiển thị thông báo "Bạn đang đăng nhập với tài khoản Admin...", nút "Vào Hòm Thư Admin" | | TH3 |
| TC-12-04 | Để trống Chủ đề | User đăng nhập → Để trống ChủĐề → Gửi | Lỗi "Vui lòng nhập chủ đề." | | TH4 |
| TC-12-05 | Để trống Lời nhắn | User đăng nhập → Để trống LờiNhắn → Gửi | Lỗi "Vui lòng nhập lời nhắn." | | TH5 |
| TC-12-06 | HoTen và Email điền sẵn readonly | User đăng nhập → Xem form `/Contact` | HoTen và Email hiển thị từ tài khoản, không chỉnh sửa được | | A6 |
| TC-12-07 | Điền số điện thoại (tùy chọn) | User → Nhập SĐT="0901234567" → Gửi | Tin nhắn gửi thành công, SĐT được lưu | | C5=T |
| TC-12-08 | Không điền SĐT | User → Không nhập SĐT → Gửi | Tin nhắn gửi thành công (SĐT tùy chọn) | | C5=F |
| TC-12-09 | Gửi thành công và xem trong Admin | User gửi tin nhắn → Admin vào `/Admin/Messages` | Tin nhắn hiển thị với trạng thái "Mới" | | End-to-end |
| TC-12-10 | Gửi tin nhắn khác sau thành công | Gửi thành công → Nhấn "Gửi tin nhắn khác" | Form hiển thị lại để gửi tiếp | | Re-send |
| TC-12-11 | Chủ đề đúng 200 ký tự (biên) | Nhập chủ đề = 200 ký tự → Gửi | Gửi thành công | | Boundary C3 |
| TC-12-12 | Lời nhắn đúng 2000 ký tự (biên) | Nhập lời nhắn = 2000 ký tự → Gửi | Gửi thành công | | Boundary C4 |

---

## 13. Hòm Thư Admin

**URL:** `/Admin/Messages`  
**Phương thức:** GET / POST (handlers: MarkRead, Delete)  
**Quyền truy cập:** [Authorize(Roles = "Admin")]

### 13.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng là Admin | T | F | — |
| C2 | Có tin nhắn trong hệ thống | T | F | — |
| C3 | Tin nhắn chưa được đọc | T | F | — |
| C4 | Tin nhắn tồn tại theo ID (khi đánh dấu/xóa) | T | F | — |

### 13.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Hiển thị danh sách tin nhắn với stats |
| A2 | Từ chối truy cập |
| A3 | Hiển thị hộp thư trống |
| A4 | Đánh dấu đã đọc (AJAX, không reload trang) |
| A5 | Xóa tin nhắn, redirect về cùng trang |
| A6 | Mở rộng nội dung tin nhắn (click row) |
| A7 | Badge số chưa đọc cập nhật |

### 13.3 Bảng Quyết Định

| TH | C1 | C2 | C3 | Hành động |
|----|----|----|----|----|
| TH1 | T | T | T | A1 + A7 |
| TH2 | T | T | F | A1 (badge=0) |
| TH3 | T | F | — | A3 |
| TH4 | F | — | — | A2 |

### 13.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-13-01 | Xem hòm thư có tin nhắn | Admin → `/Admin/Messages` | Danh sách tin nhắn, stats (tổng/chưa đọc/đã đọc) | | TH1 |
| TC-13-02 | User thường truy cập | User thường → `/Admin/Messages` | Từ chối truy cập | | TH4 |
| TC-13-03 | Hòm thư trống | Chưa có tin nhắn nào → Vào hòm thư | Hiển thị "Hòm thư trống. Chưa có tin nhắn liên hệ nào." | | TH3 |
| TC-13-04 | Click mở tin nhắn chưa đọc | Nhấn vào dòng tin nhắn trạng thái "Mới" | Nội dung mở rộng, AJAX đổi trạng thái → "Đã đọc" không reload trang | | A4 + A6 |
| TC-13-05 | Click mở tin nhắn đã đọc | Nhấn vào dòng tin nhắn "Đã đọc" | Nội dung mở rộng, không gọi AJAX mark-read | | A6 |
| TC-13-06 | Đóng tin nhắn đang mở | Click vào tin nhắn đang mở | Nội dung thu gọn lại | | Toggle |
| TC-13-07 | Chỉ một tin mở cùng lúc | Mở tin A → Click tin B | Tin A đóng, tin B mở | | Single expand |
| TC-13-08 | Badge chưa đọc trên navbar | Có 3 tin chưa đọc → Vào trang bất kỳ | Navbar Admin hiển thị "📬 Hòm Thư" với badge "3" màu đỏ | | A7 |
| TC-13-09 | Badge cập nhật sau đọc | Mở tin nhắn → Badge giảm từ 3 → 2 | Badge cập nhật ngay (AJAX) không cần reload | | Real-time badge |
| TC-13-10 | Xóa tin nhắn | Admin → Mở tin nhắn → Nhấn "🗑 Xóa" → Xác nhận | Tin nhắn bị xóa, trang reload, danh sách cập nhật | | A5 |
| TC-13-11 | Xóa tin nhắn ID không tồn tại | POST delete với id=99999 | Redirect về trang (silent fail) | | Edge case |
| TC-13-12 | Stats đúng | Có 5 tin: 2 chưa đọc, 3 đã đọc | Stats: Tổng=5, Chưa đọc=2, Đã đọc=3 | | A1 stats |
| TC-13-13 | Nút "Trả lời qua email" | Mở tin nhắn → Click "↩ Trả lời qua email" | Mở client email với to=email người gửi, subject="Re: {chủ đề}" | | Reply link |
| TC-13-14 | Tin nhắn mới sau gửi liên hệ | User gửi tin qua `/Contact` → Admin vào hòm thư | Tin mới xuất hiện đầu danh sách, trạng thái "🔵 Mới" | | End-to-end |

---

## 14. AI Dự Đoán Nhu Cầu

**URL:** `/AI/Index`  
**Phương thức:** GET  
**Quyền truy cập:** [Authorize(Roles = "Admin")]

### 14.1 Phân Tích Điều Kiện

| Mã | Điều kiện | T | F | B |
|----|-----------|---|---|---|
| C1 | Người dùng là Admin | T | F | — |
| C2 | Model ML.NET đã được train | T | F | — |
| C3 | Có dữ liệu lịch sử đủ để train | T | F | — |
| C4 | Có sự kiện active trong hệ thống | T | F | — |
| C5 | Metrics R² > 0 (model có độ chính xác) | T | F | R²=0 |

### 14.2 Hành Động

| Mã | Hành động |
|----|-----------|
| A1 | Hiển thị dự đoán nhu cầu cho từng sự kiện/loại vé |
| A2 | Từ chối truy cập |
| A3 | Hiển thị "Model chưa được train" hoặc dự đoán mặc định |
| A4 | Hiển thị metrics: R², MAE, RMSE |
| A5 | Không có sự kiện để dự đoán |

### 14.3 Bảng Quyết Định

| TH | C1 | C2 | C4 | Hành động |
|----|----|----|----|----|
| TH1 | T | T | T | A1 + A4 |
| TH2 | T | F | T | A3 |
| TH3 | T | T | F | A5 |
| TH4 | F | — | — | A2 |

### 14.4 Kịch Bản Test

| ID | Tiêu đề | Mô tả | Kết quả mong đợi | Kết quả thực tế | Ghi chú |
|----|---------|-------|------------------|-----------------|---------|
| TC-14-01 | Xem trang AI | Admin → `/AI/Index` | Trang load thành công, hiển thị trạng thái model | | Basic access |
| TC-14-02 | User thường truy cập | User → `/AI/Index` | Từ chối truy cập | | TH4 |
| TC-14-03 | Model được train thành công | Admin → `/AI/Index` khi có đủ dữ liệu | `IsTrained = true`, hiển thị metrics R², MAE, RMSE | | TH1 |
| TC-14-04 | Hiển thị metrics | Xem trang AI khi model đã train | Các chỉ số R², MAE, RMSE hiển thị với giá trị số | | A4 |
| TC-14-05 | Dự đoán cho sự kiện active | Model trained + có sự kiện active | Mỗi loại vé trong sự kiện có số vé dự đoán | | A1 |
| TC-14-06 | Không có sự kiện active | Xóa tất cả sự kiện active → Xem AI | Trang hiển thị không có dự đoán (danh sách trống) | | TH3 |
| TC-14-07 | R² gần 1 (model tốt) | Hệ thống có nhiều dữ liệu lịch sử | R² > 0.8, dự đoán tương đối chính xác | | Quality check |
| TC-14-08 | Kết quả dự đoán là số nguyên | Xem dự đoán | Số vé dự đoán là số không âm | | Non-negative |

---

---

## TỔNG HỢP TEST CASE

### Thống Kê

| Chức năng | Số TC | TC Thành công | TC Thất bại | Tỉ lệ |
|-----------|-------|---------------|-------------|--------|
| Đăng ký tài khoản | 14 | | | |
| Đăng nhập | 12 | | | |
| Đổi mật khẩu | 14 | | | |
| Tạo sự kiện mới | 15 | | | |
| Chỉnh sửa sự kiện | 13 | | | |
| Xóa sự kiện | 6 | | | |
| Đặt vé | 17 | | | |
| Giỏ hàng | 17 | | | |
| Mã giảm giá | 18 | | | |
| Quản lý khách hàng | 11 | | | |
| Báo cáo doanh thu | 9 | | | |
| Gửi liên hệ | 12 | | | |
| Hòm thư Admin | 14 | | | |
| AI dự đoán nhu cầu | 8 | | | |
| **Tổng cộng** | **180** | | | |

---

### Danh Sách Tài Khoản Test

| Vai trò | Email | Mật khẩu | Ghi chú |
|---------|-------|----------|---------|
| Admin | admin@tickethub.vn | Admin@123456 | Tài khoản demo |
| User | nguyen.van.an@email.com | User@123456 | Tài khoản demo |
| User test mới | newuser@test.com | Test@123 | Tạo khi test TC-01-01 |

---

### Dữ Liệu Test Cần Chuẩn Bị

| Hạng mục | Mô tả | Trạng thái |
|----------|-------|-----------|
| Sự kiện test | Ít nhất 1 sự kiện active, có loại vé, còn số lượng | |
| Coupon hợp lệ | Code="SAVE20", 20%, MaxUses=100, IsActive=true | |
| Coupon hết hạn | ExpiryDate < ngày hiện tại | |
| Coupon đã dùng hết | UsedCount = MaxUses | |
| Coupon inactive | IsActive = false | |
| Vé gần hết | Sự kiện với AvailableQuantity=1 | |
| Đơn hàng mẫu | Ít nhất 1 Confirmed order để test Reports | |
| Tin nhắn liên hệ | Ít nhất 1 tin nhắn chưa đọc | |

---

### Môi Trường Test

| Hạng mục | Giá trị |
|----------|---------|
| URL | http://localhost:5134 |
| Database | SQL Server LocalDB |
| Framework | ASP.NET Core 10 |
| Browser | Chrome / Edge |
| OS | Windows 11 |

---

*File này được tạo tự động dựa trên phân tích toàn bộ source code tại `C:\Users\ASUS\EventTicketSystem\src\EventTicketSystem.Web`*
