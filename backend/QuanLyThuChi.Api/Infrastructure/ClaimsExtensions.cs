using System.Security.Claims;

namespace QuanLyThuChi.Infrastructure;

public static class ClaimsExtensions
{
    /// <summary>Mã tài khoản trong claim <c>sub</c>; ném <see cref="InvalidOperationException"/> nếu thiếu hoặc sai định dạng.</summary>
    public static int MaTaiKhoan(this ClaimsPrincipal user)
    {
        var giaTri = user.FindFirst(JwtService.ClaimSub)?.Value;
        if (!int.TryParse(giaTri, out var ma))
            throw new InvalidOperationException("Token thiếu claim sub hợp lệ");
        return ma;
    }
}
