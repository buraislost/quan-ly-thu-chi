using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Quản trị: người dùng, danh mục mặc định, thống kê và tổng kết tháng (UC 2.8, 3.10).</summary>
public class QuanTriService(
    AppDbContext db, ThongBaoService thongBao, DanhMucService danhMuc, BaoCaoService baoCao, IDongHo dongHo)
{
    public const string LoiKhoaQuanTri = "Không thể khóa tài khoản quản trị viên";
    public const string LoiSuaHeThong = "Không thể sửa danh mục hệ thống";
    public const string LoiXoaHeThong = "Không thể xóa danh mục hệ thống";
    public const string LoiMacDinhDangDung = "Danh mục mặc định đang được sử dụng, không thể xóa";
    public const string LoiThang = "Tháng không hợp lệ";

    // ----- Người dùng -----

    /// <summary>Người dùng thường (không gồm quản trị viên), mới nhất trước; q khớp họ tên hoặc email.</summary>
    public async Task<List<NguoiDungAdminDto>> LietKeNguoiDungAsync(string? q, CancellationToken ct = default)
    {
        var ds = await db.Set<NguoiDung>().AsNoTracking().ToListAsync(ct);
        // Lọc trong bộ nhớ để không phụ thuộc collation của CSDL.
        var tuKhoa = q?.Trim();
        if (!string.IsNullOrEmpty(tuKhoa))
            ds = ds.Where(u => u.HoTen.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)
                               || u.Email.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)).ToList();

        var soGiaoDich = await db.GiaoDich.AsNoTracking().GroupBy(g => g.MaTaiKhoan)
            .Select(x => new { Ma = x.Key, So = x.Count() }).ToDictionaryAsync(x => x.Ma, x => x.So, ct);

        return ds.OrderByDescending(u => u.NgayTao).ThenByDescending(u => u.MaTaiKhoan)
            .Select(u => new NguoiDungAdminDto(u.MaTaiKhoan, u.HoTen, u.Email, u.TrangThai, u.NgayTao,
                soGiaoDich.GetValueOrDefault(u.MaTaiKhoan)))
            .ToList();
    }

    public async Task KhoaAsync(int maQuanTri, int maDich, string lyDo, CancellationToken ct = default)
    {
        var quanTri = await LayQuanTriAsync(maQuanTri, ct);
        var dich = await LayTaiKhoanAsync(maDich, ct);
        if (dich is QuanTriVien && dich.MaTaiKhoan != maQuanTri) throw new DomainException(LoiKhoaQuanTri);

        var daKhoa = dich.TrangThai == TrangThaiTaiKhoan.BiKhoa;
        quanTri.KhoaTaiKhoan(dich, lyDo);
        if (!daKhoa)
            thongBao.Tao(dich.MaTaiKhoan, LoaiThongBao.HeThong, $"Tài khoản của bạn đã bị khóa. Lý do: {lyDo.Trim()}");
        await db.SaveChangesAsync(ct);
    }

    public async Task MoKhoaAsync(int maQuanTri, int maDich, CancellationToken ct = default)
    {
        var quanTri = await LayQuanTriAsync(maQuanTri, ct);
        var dich = await LayTaiKhoanAsync(maDich, ct);

        var daKhoa = dich.TrangThai == TrangThaiTaiKhoan.BiKhoa;
        quanTri.MoKhoaTaiKhoan(dich);
        if (daKhoa) thongBao.Tao(dich.MaTaiKhoan, LoaiThongBao.HeThong, "Tài khoản của bạn đã được mở khóa");
        await db.SaveChangesAsync(ct);
    }

    // ----- Danh mục mặc định -----

    public async Task<List<DanhMucDto>> LietKeMacDinhAsync(CancellationToken ct = default)
    {
        var ds = await db.DanhMuc.AsNoTracking().Where(d => d.MaTaiKhoan == null).ToListAsync(ct);
        return ds.OrderBy(d => d.Loai).ThenBy(d => d.TenDanhMuc, StringComparer.CurrentCulture).Select(ToDto).ToList();
    }

    public async Task<DanhMucDto> TaoMacDinhAsync(TaoDanhMucRequest req, CancellationToken ct = default)
    {
        var dm = new DanhMuc(null, req.TenDanhMuc, req.Loai, req.BieuTuong, req.MauSac, laMacDinh: true, laHeThong: false);
        await danhMuc.KiemTraTrungTenAsync(null, dm.TenDanhMuc, dm.Loai, null, ct);
        db.DanhMuc.Add(dm);
        await db.SaveChangesAsync(ct);
        return ToDto(dm);
    }

    public async Task<DanhMucDto> SuaMacDinhAsync(int maDm, SuaDanhMucRequest req, CancellationToken ct = default)
    {
        var dm = await LayMacDinhAsync(maDm, ct);
        if (dm.LaHeThong) throw new DomainException(LoiSuaHeThong);
        dm.DoiThongTin(req.TenDanhMuc, req.BieuTuong, req.MauSac);
        await danhMuc.KiemTraTrungTenAsync(null, dm.TenDanhMuc, dm.Loai, maDm, ct);
        await db.SaveChangesAsync(ct);
        return ToDto(dm);
    }

    public async Task XoaMacDinhAsync(int maDm, CancellationToken ct = default)
    {
        var dm = await LayMacDinhAsync(maDm, ct);
        if (dm.LaHeThong) throw new DomainException(LoiXoaHeThong);
        var coGiaoDich = await db.GiaoDich.AnyAsync(g => g.MaDanhMuc == maDm, ct);
        var coNganSach = await db.NganSach.AnyAsync(n => n.MaDanhMuc == maDm, ct);
        if (dm.DangDuocSuDung(coGiaoDich, coNganSach)) throw new DomainException(LoiMacDinhDangDung);
        db.DanhMuc.Remove(dm);
        await db.SaveChangesAsync(ct);
    }

    // ----- Thống kê và tổng kết -----

    public async Task<ThongKeDto> ThongKeAsync(CancellationToken ct = default)
    {
        var nguoiDung = db.Set<NguoiDung>().AsNoTracking();
        var soNguoiDung = await nguoiDung.CountAsync(ct);
        var soBiKhoa = await nguoiDung.CountAsync(u => u.TrangThai == TrangThaiTaiKhoan.BiKhoa, ct);
        var soHoatDong = await nguoiDung.CountAsync(u => u.TrangThai == TrangThaiTaiKhoan.HoatDong, ct);

        var homNay = dongHo.HomNay;
        var dau = new DateTime(homNay.Year, homNay.Month, 1);
        var cuoi = dau.AddMonths(1);
        var soGiaoDich = await db.GiaoDich.CountAsync(ct);
        var soThangNay = await db.GiaoDich.CountAsync(g => g.Ngay >= dau && g.Ngay < cuoi, ct);
        return new ThongKeDto(soNguoiDung, soHoatDong, soBiKhoa, soGiaoDich, soThangNay);
    }

    /// <summary>Chạy tổng kết tháng cho mọi người dùng đang hoạt động; mặc định là tháng hiện tại (giờ Việt Nam).</summary>
    public async Task<KetQuaTongKetDto> ChayTongKetAsync(int? thang, int? nam, CancellationToken ct = default)
    {
        var homNay = dongHo.HomNay;
        var t = thang ?? homNay.Month;
        var n = nam ?? homNay.Year;
        if (t is < 1 or > 12 || n is < 2000 or > 2100) throw new DomainException(LoiThang);
        return new KetQuaTongKetDto(await baoCao.TongKetTatCaAsync(t, n, ct));
    }

    // ----- Hỗ trợ -----

    private async Task<QuanTriVien> LayQuanTriAsync(int ma, CancellationToken ct) =>
        await db.Set<QuanTriVien>().FirstOrDefaultAsync(q => q.MaTaiKhoan == ma, ct) ?? throw new KhongTimThayException();

    private async Task<TaiKhoan> LayTaiKhoanAsync(int ma, CancellationToken ct) =>
        await db.TaiKhoan.FirstOrDefaultAsync(t => t.MaTaiKhoan == ma, ct) ?? throw new KhongTimThayException();

    private async Task<DanhMuc> LayMacDinhAsync(int ma, CancellationToken ct) =>
        await db.DanhMuc.FirstOrDefaultAsync(d => d.MaDanhMuc == ma && d.MaTaiKhoan == null, ct)
        ?? throw new KhongTimThayException();

    private static DanhMucDto ToDto(DanhMuc d) =>
        new(d.MaDanhMuc, d.TenDanhMuc, d.Loai, d.BieuTuong, d.MauSac, d.LaMacDinh, d.LaHeThong);
}
