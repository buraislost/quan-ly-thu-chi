using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Thêm, sửa, xóa và tìm giao dịch; mọi thay đổi cập nhật số dư ví và cảnh báo ngân sách trong một transaction (UC 2.4, 3.5).</summary>
public class GiaoDichService(AppDbContext db, IDongHo dongHo, ViService viService, DanhMucService danhMucService,
    NganSachService nganSach)
{
    public const int KichThuocTrang = 20;
    public const string LoiHoaDonChiChoChi = "Chỉ giao dịch chi mới đính kèm hóa đơn";
    public const string LoiKhoangThoiGian = "Khoảng thời gian không hợp lệ";
    public const string LoiDanhMucHeThong = "Không thể chọn danh mục hệ thống";
    public const string LoiGiaoDichMucTieu = "Giao dịch nạp mục tiêu không thể sửa hoặc xóa";

    /// <summary>Tạo giao dịch. <paramref name="maMucTieu"/> chỉ do nghiệp vụ nạp mục tiêu tiết kiệm truyền vào.</summary>
    public async Task<GiaoDich> TaoAsync(int maTk, TaoGiaoDichRequest r, int? maMucTieu = null, CancellationToken ct = default)
    {
        var vi = await viService.LayCuaToiAsync(maTk, r.MaVi, ct);
        var dm = await danhMucService.LayDungDuocAsync(maTk, r.MaDanhMuc, ct);
        if (dm.LaHeThong && maMucTieu is null) throw new DomainException(LoiDanhMucHeThong);
        if (r.HoaDon is not null && r.Loai != LoaiGiaoDich.Chi) throw new DomainException(LoiHoaDonChiChoChi);

        var gd = new GiaoDich(maTk, vi, dm, r.SoTien, r.Loai, r.NgayGiaoDich is { } ngayTao ? dongHo.SangGioVietNam(ngayTao) : dongHo.BayGio, r.GhiChu, maMucTieu);
        if (r.HoaDon is { } hd) gd.GanHoaDon(TaoHoaDon(hd, gd.Ngay));

        await using var tx = await BatDauAsync(ct);
        gd.GhiNhan();
        db.GiaoDich.Add(gd);
        await db.SaveChangesAsync(ct);
        await KiemTraNganSachAsync(maTk, [(gd.MaDanhMuc, gd.Ngay, gd.Loai)], ct);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return gd;
    }

    public async Task<GiaoDich> SuaAsync(int maTk, int maGd, SuaGiaoDichRequest r, CancellationToken ct = default)
    {
        var gd = await LayCuaToiAsync(maTk, maGd, ct);
        if (gd.MaMucTieu is not null) throw new DomainException(LoiGiaoDichMucTieu);
        var vi = await viService.LayCuaToiAsync(maTk, r.MaVi, ct);
        var dm = await danhMucService.LayDungDuocAsync(maTk, r.MaDanhMuc, ct);
        if (dm.LaHeThong) throw new DomainException(LoiDanhMucHeThong);
        if (gd.HoaDon is not null && r.Loai != LoaiGiaoDich.Chi) throw new DomainException(LoiHoaDonChiChoChi);

        var cu = (gd.MaDanhMuc, gd.Ngay, gd.Loai);
        await using var tx = await BatDauAsync(ct);
        gd.CapNhat(vi, dm, r.SoTien, r.Loai, r.NgayGiaoDich is { } ngayMoi ? dongHo.SangGioVietNam(ngayMoi) : gd.Ngay, r.GhiChu);
        await db.SaveChangesAsync(ct);
        await KiemTraNganSachAsync(maTk, [cu, (gd.MaDanhMuc, gd.Ngay, gd.Loai)], ct);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return gd;
    }

    public async Task XoaAsync(int maTk, int maGd, CancellationToken ct = default)
    {
        var gd = await LayCuaToiAsync(maTk, maGd, ct);
        if (gd.MaMucTieu is not null) throw new DomainException(LoiGiaoDichMucTieu);

        await using var tx = await BatDauAsync(ct);
        gd.Huy();
        db.GiaoDich.Remove(gd);
        await db.SaveChangesAsync(ct);
        await KiemTraNganSachAsync(maTk, [(gd.MaDanhMuc, gd.Ngay, gd.Loai)], ct);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
    }

    public async Task<GiaoDichChiTietDto> LayChiTietAsync(int maTk, int maGd, CancellationToken ct = default)
    {
        var gd = await LayCuaToiAsync(maTk, maGd, ct);
        return ToChiTietDto(gd);
    }

    public async Task<TrangKetQua<GiaoDichDto>> TimKiemAsync(int maTk, DateOnly? tu, DateOnly? den, LoaiGiaoDich? loai,
        int? maVi, int? maDanhMuc, string? q, int trang, CancellationToken ct = default)
    {
        if ((tu is { } t && den is { } d && t > d) || den == DateOnly.MaxValue) throw new DomainException(LoiKhoangThoiGian);
        if (trang <= 0) trang = 1;

        var truyVan = db.GiaoDich.AsNoTracking().Where(g => g.MaTaiKhoan == maTk);
        if (tu is { } dau) { var tuNgay = dau.ToDateTime(TimeOnly.MinValue); truyVan = truyVan.Where(g => g.Ngay >= tuNgay); }
        if (den is { } cuoi) { var denNgay = cuoi.AddDays(1).ToDateTime(TimeOnly.MinValue); truyVan = truyVan.Where(g => g.Ngay < denNgay); }
        if (loai is { } l) truyVan = truyVan.Where(g => g.Loai == l);
        if (maVi is { } v) truyVan = truyVan.Where(g => g.MaVi == v);
        if (maDanhMuc is { } m) truyVan = truyVan.Where(g => g.MaDanhMuc == m);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var tuKhoa = q.Trim().ToLower();
            truyVan = truyVan.Where(g => g.GhiChu != null && g.GhiChu.ToLower().Contains(tuKhoa));
        }

        var tong = await truyVan.CountAsync(ct);
        var muc = await truyVan.OrderByDescending(g => g.Ngay).ThenByDescending(g => g.MaGiaoDich)
            .Skip((trang - 1) * KichThuocTrang).Take(KichThuocTrang)
            .Select(g => new GiaoDichDto(g.MaGiaoDich, g.SoTien, g.Loai, g.Ngay, g.GhiChu, g.MaVi, g.Vi.TenVi,
                g.MaDanhMuc, g.DanhMuc.TenDanhMuc, g.HoaDon != null))
            .ToListAsync(ct);
        return new TrangKetQua<GiaoDichDto>(muc, trang, tong);
    }

    public static GiaoDichDto ToDto(GiaoDich g) =>
        new(g.MaGiaoDich, g.SoTien, g.Loai, g.Ngay, g.GhiChu, g.MaVi, g.Vi.TenVi, g.MaDanhMuc, g.DanhMuc.TenDanhMuc,
            g.HoaDon is not null);

    private static GiaoDichChiTietDto ToChiTietDto(GiaoDich g)
    {
        var hd = g.HoaDon is null ? null : new HoaDonDto(g.HoaDon.MaHoaDon, g.HoaDon.TenCuaHang, g.HoaDon.NgayHoaDon,
            g.HoaDon.TongTien, g.HoaDon.AnhHoaDon,
            g.HoaDon.ChiTiet.OrderBy(c => c.MaChiTiet)
                .Select(c => new ChiTietHoaDonDto(c.TenMatHang, c.SoLuong, c.DonGia, c.ThanhTien())).ToList());
        return new GiaoDichChiTietDto(g.MaGiaoDich, g.SoTien, g.Loai, g.Ngay, g.GhiChu, g.MaVi, g.Vi.TenVi,
            g.MaDanhMuc, g.DanhMuc.TenDanhMuc, hd is not null, hd);
    }

    private HoaDon TaoHoaDon(HoaDonRequest r, DateTime ngayGiaoDich)
    {
        var hd = new HoaDon(r.TenCuaHang, r.NgayHoaDon is { } ngay ? dongHo.SangGioVietNam(ngay) : ngayGiaoDich, r.AnhHoaDon);
        foreach (var c in r.ChiTiet) hd.ThemChiTiet(new ChiTietHoaDon(c.TenMatHang, c.SoLuong, c.DonGia));
        return hd;
    }

    /// <summary>Giao dịch của chính người dùng; của người khác hoặc không tồn tại đều là 404.</summary>
    private async Task<GiaoDich> LayCuaToiAsync(int maTk, int maGd, CancellationToken ct) =>
        await db.GiaoDich.Include(g => g.Vi).Include(g => g.DanhMuc).Include(g => g.HoaDon!).ThenInclude(h => h.ChiTiet)
            .FirstOrDefaultAsync(g => g.MaGiaoDich == maGd && g.MaTaiKhoan == maTk, ct)
        ?? throw new KhongTimThayException();

    /// <summary>Bắt đầu transaction; nếu người gọi đã mở sẵn thì dùng transaction đó (trả null, không tự commit).</summary>
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BatDauAsync(CancellationToken ct) =>
        db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;

    private async Task KiemTraNganSachAsync(int maTk, IEnumerable<(int MaDanhMuc, DateTime Ngay, LoaiGiaoDich Loai)> cacKhoa,
        CancellationToken ct)
    {
        var khoa = cacKhoa.Where(k => k.Loai == LoaiGiaoDich.Chi)
            .Select(k => (k.MaDanhMuc, k.Ngay.Month, k.Ngay.Year)).Distinct();
        foreach (var (maDm, thang, nam) in khoa)
            await nganSach.KiemTraSauGiaoDichAsync(maTk, maDm, thang, nam, ct);
    }
}
