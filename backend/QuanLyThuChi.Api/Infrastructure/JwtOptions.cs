namespace QuanLyThuChi.Infrastructure;

/// <summary>Cấu hình mục <c>Jwt</c>. <see cref="Khoa"/> phải có ít nhất 32 ký tự và không được commit.</summary>
public class JwtOptions
{
    public const string TenMuc = "Jwt";
    public const int DoDaiKhoaToiThieu = 32;

    public string Khoa { get; set; } = "";
    public string PhatHanh { get; set; } = "QuanLyThuChi";
    public string DoiTuong { get; set; } = "QuanLyThuChi.Web";
    public int HanGio { get; set; } = 8;
}
