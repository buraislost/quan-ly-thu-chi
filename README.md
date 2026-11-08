# Quản lý thu chi cá nhân

Đồ án môn Phân tích thiết kế hướng đối tượng: hệ thống quản lý thu chi cá nhân và lập kế hoạch tài chính. Phần backend là ASP.NET Core Web API nằm trong thư mục `backend/`.

## Yêu cầu cài đặt

- .NET 10 SDK (kiểm tra bằng `dotnet --list-sdks`).
- SQL Server Express hoặc LocalDB (đi kèm Visual Studio). Chuỗi kết nối mặc định trong `appsettings.json` trỏ tới `(localdb)\MSSQLLocalDB`, cơ sở dữ liệu `QuanLyThuChi`; cần đổi ở `ConnectionStrings:MacDinh` nếu dùng SQL Server Express riêng.
- Công cụ `dotnet-ef`: `dotnet tool install --global dotnet-ef`.
- Chạy test không cần SQL Server vì test dùng SQLite in-memory.

## Cấu hình

Tạo file `backend/QuanLyThuChi.Api/appsettings.Development.json` từ file mẫu `appsettings.Development.example.json` (file này đã được gitignore, không commit):

```json
{
  "Jwt": { "Khoa": "chuỗi-ngẫu-nhiên-tối-thiểu-32-ký-tự" },
  "Gemini": { "ApiKey": "khóa-lấy-từ-Google-AI-Studio", "DungBanGia": false }
}
```

- `Jwt:Khoa`: khóa ký JWT, bắt buộc và tối thiểu 32 ký tự; thiếu hoặc ngắn hơn thì ứng dụng không khởi động. Khi triển khai thật, đặt qua biến môi trường `Jwt__Khoa`.
- `Gemini:ApiKey`: khóa Gemini để quét hóa đơn, là bí mật nên chỉ đặt trong file trên hoặc biến môi trường `Gemini__ApiKey`. Thiếu khóa thì ứng dụng vẫn chạy, nhưng quét hóa đơn trả lỗi 400 "Không nhận dạng được hóa đơn".
- `Gemini:DungBanGia`: đặt `true` để demo offline; hệ thống trả hóa đơn mẫu cố định (Bách Hóa Xanh, 3 dòng) thay vì gọi Gemini.
- `Gemini:Model`: mặc định trong `appsettings.json`, đổi theo tên model hiện hành nếu cần.

## Tạo cơ sở dữ liệu và chạy

Từ thư mục gốc của repo:

```bash
dotnet ef database update --project backend/QuanLyThuChi.Api
dotnet run --project backend/QuanLyThuChi.Api
```

- Swagger UI: mở địa chỉ `https://localhost:<cổng>/swagger` (cổng in ra trong cửa sổ lệnh, xem thêm `Properties/launchSettings.json`).
- Kiểm tra nhanh: `GET /api/health`.
- Để gọi API có khóa trên Swagger: gọi `POST /api/auth/dang-nhap`, sao chép `token` rồi bấm Authorize và dán token.

## Tài khoản demo

Khi chạy ở môi trường Development, ứng dụng tự nạp dữ liệu khởi tạo và dữ liệu demo (chỉ nạp một lần, chạy lại không bị trùng). Mật khẩu của cả hai tài khoản là `Demo@123`.

| Vai trò | Email | Ghi chú |
|---|---|---|
| Người dùng | `demo@demo.vn` | Họ tên Nguyễn Văn Demo; 3 ví (Tiền mặt, Vietcombank, MoMo), khoảng 90 giao dịch trong 3 tháng gần nhất, ngân sách Ăn uống gần ngưỡng (khoảng 85%) và Giải trí vượt hạn mức, mục tiêu "Mua laptop" đúng tiến độ và "Du lịch Đà Lạt" bị chậm |
| Quản trị viên | `admin@demo.vn` | Xem thống kê, quản lý người dùng |

Dữ liệu demo bám theo ngày hiện tại (3 tháng gần nhất tính đến hôm nay) và không nạp ở môi trường khác Development.

## Tổng kết tháng

Bộ hẹn giờ tự chạy tổng kết tháng. Khi demo có thể kích hoạt thủ công bằng tài khoản quản trị viên (đăng nhập `admin@demo.vn`, gửi kèm Bearer token):

```
POST /api/admin/chay-tong-ket?thang=10&nam=2026
```

Bỏ trống `thang` và `nam` thì hệ thống tổng kết tháng hiện tại. Kết quả trả về là số người dùng được tổng kết thành công; người dùng xem báo cáo tháng và thông báo tổng kết trong ứng dụng.

## Ảnh hóa đơn

Ảnh tải lên được lưu ở `backend/QuanLyThuChi.Api/uploads/hoa-don/` (đã gitignore) và phục vụ tại `/uploads/hoa-don/...`; đổi thư mục gốc bằng `LuuTep:ThuMuc`. Lưu ý các ảnh này công khai theo URL: ai biết đường dẫn đều xem được, không cần đăng nhập, nên đừng tải lên hóa đơn có thông tin nhạy cảm. Ảnh mẫu để demo do nhóm bổ sung trong `docs/demo/`.

## Chạy test

```bash
dotnet test backend
```

## Cấu trúc dự án

```
backend/
  QuanLyThuChi.Api/
    Controllers/     Các endpoint REST
    Services/        Nghiệp vụ theo từng chức năng
    Domain/          Lớp thực thể và luật nghiệp vụ (theo sơ đồ lớp)
    Dtos/            Dữ liệu vào/ra của API
    Data/            AppDbContext, cấu hình bảng, Migrations, dữ liệu khởi tạo và demo
    Infrastructure/  JWT, băm mật khẩu, OCR (Gemini), lưu tệp, xuất Excel
    Jobs/            Bộ hẹn giờ nhắc nhở và tổng kết tháng
  QuanLyThuChi.Tests/  Test domain và test API (SQLite in-memory)
docs/
  thiet-ke/          Tài liệu thiết kế và kế hoạch
  staruml/           Mô hình StarUML (.mdj) và ảnh sơ đồ lớp
  demo/              Ảnh hóa đơn mẫu để demo quét hóa đơn
```

## Quy ước

- Mọi logic "hôm nay/bây giờ" dùng giờ Việt Nam (UTC+7) qua `IDongHo`, không dùng `DateTime.Now` trong Domain/Services.
