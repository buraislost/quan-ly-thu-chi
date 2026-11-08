namespace QuanLyThuChi.Domain;

/// <summary>Vi phạm quy tắc nghiệp vụ; thông điệp tiếng Việt hiển thị cho người dùng (HTTP 400).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Không tìm thấy dữ liệu (HTTP 404).</summary>
public class KhongTimThayException : Exception
{
    public KhongTimThayException() : base("Không tìm thấy dữ liệu") { }

    public KhongTimThayException(string message) : base(message) { }
}
