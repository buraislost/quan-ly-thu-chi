namespace QuanLyThuChi.Domain;

/// <summary>Băm và kiểm tra mật khẩu (cài đặt BCrypt nằm ở Infrastructure).</summary>
public interface IBamMatKhau
{
    string Bam(string matKhau);
    bool KiemTra(string matKhau, string hash);
}
