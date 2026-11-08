using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Services;

/// <summary>Đăng ký, đăng nhập và quản lý hồ sơ tài khoản (UC 2.1, 3.1, 3.2).</summary>
public class TaiKhoanService(AppDbContext db, IBamMatKhau bam, IDongHo dongHo, JwtService jwt)
{
    public const string LoiEmailDaDung = "Email đã được sử dụng";
    public const string LoiDangNhapSai = "Email hoặc mật khẩu không đúng";
    public const string LoiMatKhauCuSai = "Mật khẩu hiện tại không đúng";

    private static string ChuanHoaEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<DangNhapResponse> DangKyAsync(DangKyRequest req, CancellationToken ct = default)
    {
        var email = ChuanHoaEmail(req.Email);
        if (await db.TaiKhoan.AnyAsync(t => t.Email == email, ct))
            throw new DomainException(LoiEmailDaDung);

        var nguoiDung = new NguoiDung(req.HoTen, email, bam.Bam(req.MatKhau), dongHo.BayGio);
        db.TaiKhoan.Add(nguoiDung); // kéo theo CaiDatNhacNho mặc định qua quan hệ 1-1

        // Vi chỉ giữ khóa ngoại MaTaiKhoan (không có navigation), nên cần mã do CSDL cấp:
        // hai lần lưu trong cùng một transaction để việc tạo tài khoản là nguyên tử.
        await using var giaoDich = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);
            db.Vi.Add(new Vi(nguoiDung.MaTaiKhoan, "Tiền mặt", LoaiVi.TienMat, 0, dongHo.BayGio));
            await db.SaveChangesAsync(ct);
            await giaoDich.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Hai yêu cầu đăng ký cùng email đồng thời: chỉ mục duy nhất từ chối yêu cầu đến sau.
            await giaoDich.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            if (await db.TaiKhoan.AsNoTracking().AnyAsync(t => t.Email == email, ct))
                throw new DomainException(LoiEmailDaDung);
            throw;
        }

        return new DangNhapResponse(jwt.TaoToken(nguoiDung), VaiTro(nguoiDung), nguoiDung.HoTen);
    }

    public async Task<DangNhapResponse> DangNhapAsync(DangNhapRequest req, CancellationToken ct = default)
    {
        var email = ChuanHoaEmail(req.Email);
        var taiKhoan = await db.TaiKhoan.FirstOrDefaultAsync(t => t.Email == email, ct);
        if (taiKhoan is null || !taiKhoan.DangNhap(req.MatKhau, bam))
            throw new DomainException(LoiDangNhapSai);

        return new DangNhapResponse(jwt.TaoToken(taiKhoan), VaiTro(taiKhoan), taiKhoan.HoTen);
    }

    public async Task<HoSoResponse> LayHoSoAsync(int maTaiKhoan, CancellationToken ct = default) =>
        ToHoSo(await TimAsync(maTaiKhoan, ct));

    public async Task<HoSoResponse> CapNhatHoSoAsync(int maTaiKhoan, CapNhatHoSoRequest req, CancellationToken ct = default)
    {
        var taiKhoan = await TimAsync(maTaiKhoan, ct);
        taiKhoan.CapNhatThongTin(req.HoTen);
        await db.SaveChangesAsync(ct);
        return ToHoSo(taiKhoan);
    }

    public async Task DoiMatKhauAsync(int maTaiKhoan, DoiMatKhauRequest req, CancellationToken ct = default)
    {
        var taiKhoan = await TimAsync(maTaiKhoan, ct);
        if (!taiKhoan.DoiMatKhau(req.MatKhauCu, req.MatKhauMoi, bam))
            throw new DomainException(LoiMatKhauCuSai);
        await db.SaveChangesAsync(ct);
    }

    private async Task<TaiKhoan> TimAsync(int ma, CancellationToken ct) =>
        await db.TaiKhoan.FirstOrDefaultAsync(t => t.MaTaiKhoan == ma, ct) ?? throw new KhongTimThayException();

    private static string VaiTro(TaiKhoan tk) => tk is QuanTriVien ? "QuanTriVien" : "NguoiDung";

    private static HoSoResponse ToHoSo(TaiKhoan tk) =>
        new(tk.MaTaiKhoan, tk.HoTen, tk.Email, VaiTro(tk), (tk as NguoiDung)?.DonViTienTe);
}
