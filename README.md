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
