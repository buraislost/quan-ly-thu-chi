# Ảnh hóa đơn mẫu cho demo quét hóa đơn

Thư mục này dành cho 2–3 ảnh hóa đơn thật do nhóm tự chụp, dùng để demo chức năng quét hóa đơn (`POST /api/hoa-don/quet`).

Nhóm vui lòng bổ sung ảnh theo các lưu ý sau:

- Định dạng JPG hoặc PNG, dung lượng mỗi ảnh tối đa 5 MB.
- Chụp rõ nét, đủ sáng, thẳng góc, thấy đủ tên cửa hàng, ngày, các dòng hàng và tổng tiền.
- Nên có ít nhất một hóa đơn siêu thị/cửa hàng tiện lợi và một hóa đơn quán ăn hoặc cà phê.
- Không chụp thông tin cá nhân nhạy cảm (số điện thoại, số thẻ, mã thành viên).
- Đặt tên ngắn gọn, không dấu, ví dụ `hoa-don-1.jpg`.

Khi không có mạng hoặc chưa có khóa Gemini, đặt `Gemini:DungBanGia = true` trong `appsettings.Development.json` để demo bằng hóa đơn mẫu cố định (xem README gốc).
