using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Infrastructure;

public class BCryptBamMatKhau : IBamMatKhau
{
    public string Bam(string matKhau) => BCrypt.Net.BCrypt.HashPassword(matKhau);

    public bool KiemTra(string matKhau, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(matKhau, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false; // hash không đúng định dạng
        }
    }
}
