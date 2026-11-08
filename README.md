# Quản lý thu chi cá nhân

Đồ án môn Phân tích thiết kế hướng đối tượng. Backend: ASP.NET Core Web API (`backend/`).

## Yêu cầu cài đặt

- .NET 10 SDK (máy dựng ban đầu dùng SDK `10.0.403`, kiểm tra bằng `dotnet --list-sdks`). Máy của nhóm cần cài .NET 10 SDK.
- (Các task sau) SQL Server để chạy thật; test dùng SQLite in-memory nên không cần.

## Cấu hình khóa JWT (bắt buộc trước khi chạy)

Khóa ký JWT không nằm trong git. Mỗi thành viên tạo file `backend/QuanLyThuChi.Api/appsettings.Development.json` (đã được gitignore) từ file mẫu `appsettings.Development.example.json`:

```json
{ "Jwt": { "Khoa": "chuỗi-ngẫu-nhiên-tối-thiểu-32-ký-tự" } }
```

Thiếu hoặc ngắn hơn 32 ký tự thì ứng dụng không khởi động. Khi triển khai thật, đặt qua biến môi trường `Jwt__Khoa`. Test tự cấp khóa riêng nên không cần file này.

## Quét hóa đơn (Gemini)

Chức năng `POST /api/hoa-don/quet` gọi Gemini để đọc ảnh hóa đơn. Khóa API là bí mật, chỉ đặt trong `appsettings.Development.json` (đã gitignore) hoặc biến môi trường `Gemini__ApiKey`:

```json
{ "Gemini": { "ApiKey": "khóa-lấy-từ-Google-AI-Studio" } }
```

- `Gemini:Model` (mặc định trong `appsettings.json`: `gemini-3.8-flash`) có thể đổi theo tên model hiện hành trong tài liệu Gemini.
- Thiếu `ApiKey` thì ứng dụng vẫn khởi động, nhưng quét hóa đơn trả lỗi 400 "Không nhận dạng được hóa đơn".
- Demo không có mạng/khóa: đặt `"Gemini": { "DungBanGia": true }` để dùng hóa đơn mẫu cố định (Bách Hóa Xanh, 3 dòng) thay cho Gemini.
- Ảnh tải lên được lưu ở `backend/QuanLyThuChi.Api/uploads/hoa-don/` (gitignore) và phục vụ tại `/uploads/hoa-don/...`; đổi thư mục gốc bằng `LuuTep:ThuMuc`.

## Chạy backend

```bash
cd backend
dotnet run --project QuanLyThuChi.Api
```

- Kiểm tra: `GET /api/health`
- Swagger UI: `/swagger`

## Chạy test

```bash
dotnet test backend
```

## Quy ước

- Mọi logic "hôm nay/bây giờ" dùng giờ Việt Nam (UTC+7) qua `IDongHo`, không dùng `DateTime.Now` trong Domain/Services.
