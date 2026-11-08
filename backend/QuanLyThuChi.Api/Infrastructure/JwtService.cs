using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Infrastructure;

/// <summary>Phát hành JWT cho tài khoản đã đăng nhập.</summary>
public class JwtService(IOptions<JwtOptions> options, TimeProvider thoiGian)
{
    public const string ClaimSub = "sub";
    public const string ClaimRole = "role";

    public static SymmetricSecurityKey TaoKhoaKy(string khoa) => new(Encoding.UTF8.GetBytes(khoa));

    /// <summary>Token có <c>sub</c> = MaTaiKhoan, <c>role</c> = NguoiDung | QuanTriVien, hạn <see cref="JwtOptions.HanGio"/> giờ.</summary>
    public string TaoToken(TaiKhoan tk)
    {
        var cauHinh = options.Value;
        var bayGio = thoiGian.GetUtcNow().UtcDateTime;
        var vaiTro = tk is QuanTriVien ? "QuanTriVien" : "NguoiDung";

        var moTa = new SecurityTokenDescriptor
        {
            Issuer = cauHinh.PhatHanh,
            Audience = cauHinh.DoiTuong,
            NotBefore = bayGio,
            IssuedAt = bayGio,
            Expires = bayGio.AddHours(cauHinh.HanGio),
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimSub, tk.MaTaiKhoan.ToString()),
                new Claim(ClaimRole, vaiTro),
            ]),
            SigningCredentials = new SigningCredentials(TaoKhoaKy(cauHinh.Khoa), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(moTa);
    }
}
