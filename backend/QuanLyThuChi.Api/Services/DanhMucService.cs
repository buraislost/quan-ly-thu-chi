using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Quản lý danh mục thu/chi của người dùng cùng danh mục mặc định dùng chung (UC 3.3, 3.4).</summary>
public class DanhMucService(AppDbContext db)
{
    public const string LoiTrungTen = "Tên danh mục đã tồn tại";
    public const string LoiSuaMacDinh = "Không thể sửa danh mục mặc định";
    public const string LoiXoaMacDinh = "Không thể xóa danh mục mặc định";
    public const string LoiCanChuyenSang = "Danh mục đang được sử dụng, vui lòng chọn danh mục để chuyển sang";
    public const string LoiKhacLoai = "Danh mục chuyển sang phải cùng loại";
    public const string LoiChuyenSangHeThong = "Không thể chuyển sang danh mục hệ thống";

    /// <summary>Danh mục mặc định rồi đến danh mục của người dùng, mỗi nhóm theo tên.</summary>
    public async Task<List<DanhMucDto>> LietKeAsync(int maTk, LoaiGiaoDich? loai, CancellationToken ct = default)
    {
        var truyVan = db.DanhMuc.AsNoTracking().Where(d => d.MaTaiKhoan == null || d.MaTaiKhoan == maTk);
        if (loai is { } l) truyVan = truyVan.Where(d => d.Loai == l);
        var ds = await truyVan.ToListAsync(ct);
        return ds.OrderBy(d => d.MaTaiKhoan != null).ThenBy(d => d.TenDanhMuc, StringComparer.CurrentCulture)
            .Select(ToDto).ToList();
    }

    /// <summary>Danh mục mặc định hoặc của chính người dùng; của người khác hoặc không tồn tại đều là 404.</summary>
    public async Task<DanhMuc> LayDungDuocAsync(int maTk, int maDm, CancellationToken ct = default) =>
        await db.DanhMuc.FirstOrDefaultAsync(d => d.MaDanhMuc == maDm && (d.MaTaiKhoan == null || d.MaTaiKhoan == maTk), ct)
        ?? throw new KhongTimThayException();

    public async Task<DanhMucDto> TaoAsync(int maTk, TaoDanhMucRequest req, CancellationToken ct = default)
    {
        var dm = new DanhMuc(maTk, req.TenDanhMuc, req.Loai, req.BieuTuong, req.MauSac, laMacDinh: false, laHeThong: false);
        await KiemTraTrungTenAsync(maTk, dm.TenDanhMuc, dm.Loai, null, ct);
        db.DanhMuc.Add(dm);
        await db.SaveChangesAsync(ct);
        return ToDto(dm);
    }

    public async Task<DanhMucDto> SuaAsync(int maTk, int maDm, SuaDanhMucRequest req, CancellationToken ct = default)
    {
        var dm = await LayDungDuocAsync(maTk, maDm, ct);
        if (dm.LaMacDinh) throw new DomainException(LoiSuaMacDinh);
        dm.DoiThongTin(req.TenDanhMuc, req.BieuTuong, req.MauSac);
        await KiemTraTrungTenAsync(maTk, dm.TenDanhMuc, dm.Loai, maDm, ct);
        await db.SaveChangesAsync(ct);
        return ToDto(dm);
    }

    /// <summary>
    /// Xóa danh mục của người dùng. Nếu đang được dùng (giao dịch hoặc ngân sách) thì phải có <paramref name="chuyenSang"/>:
    /// giao dịch được chuyển sang đó, ngân sách cùng tháng được gộp hạn mức; tất cả trong một transaction.
    /// </summary>
    public async Task XoaAsync(int maTk, int maDm, int? chuyenSang, CancellationToken ct = default)
    {
        var dm = await LayDungDuocAsync(maTk, maDm, ct);
        if (dm.LaMacDinh) throw new DomainException(LoiXoaMacDinh);

        DanhMuc? dich = null;
        if (chuyenSang is { } maDich)
        {
            if (maDich == maDm) throw new DomainException(LoiKhacLoai);
            dich = await LayDungDuocAsync(maTk, maDich, ct);
            if (dich.LaHeThong) throw new DomainException(LoiChuyenSangHeThong);
            if (dich.Loai != dm.Loai) throw new DomainException(LoiKhacLoai);
        }

        var giaoDich = await db.GiaoDich.Where(g => g.MaDanhMuc == maDm && g.MaTaiKhoan == maTk).ToListAsync(ct);
        var nganSach = await db.NganSach.Where(n => n.MaDanhMuc == maDm && n.MaTaiKhoan == maTk).ToListAsync(ct);
        if (dm.DangDuocSuDung(giaoDich.Count > 0, nganSach.Count > 0))
        {
            if (dich is null) throw new DomainException(LoiCanChuyenSang);

            await using var giaoDichCsdl = await db.Database.BeginTransactionAsync(ct);
            foreach (var g in giaoDich) g.ChuyenDanhMuc(dich);

            var nganSachDich = await db.NganSach.Where(n => n.MaDanhMuc == dich.MaDanhMuc && n.MaTaiKhoan == maTk).ToListAsync(ct);
            foreach (var n in nganSach)
            {
                var trung = nganSachDich.FirstOrDefault(x => x.Thang == n.Thang && x.Nam == n.Nam);
                if (trung is null) n.ChuyenDanhMuc(dich.MaDanhMuc);
                else
                {
                    trung.GopHanMuc(n.HanMuc);
                    db.NganSach.Remove(n);
                }
            }
            db.DanhMuc.Remove(dm);
            await db.SaveChangesAsync(ct);
            await giaoDichCsdl.CommitAsync(ct);
            return;
        }

        db.DanhMuc.Remove(dm);
        await db.SaveChangesAsync(ct);
    }

    // So sánh trong bộ nhớ để không phụ thuộc collation của CSDL.
    // maTk null: kiểm tra trong phạm vi danh mục mặc định (dùng cho quản trị viên).
    internal async Task KiemTraTrungTenAsync(int? maTk, string ten, LoaiGiaoDich loai, int? boQuaMa, CancellationToken ct)
    {
        var cacTen = await db.DanhMuc.AsNoTracking()
            .Where(d => (d.MaTaiKhoan == null || d.MaTaiKhoan == maTk) && d.Loai == loai && d.MaDanhMuc != boQuaMa)
            .Select(d => d.TenDanhMuc).ToListAsync(ct);
        if (cacTen.Any(t => string.Equals(t.Trim(), ten.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException(LoiTrungTen);
    }

    private static DanhMucDto ToDto(DanhMuc d) =>
        new(d.MaDanhMuc, d.TenDanhMuc, d.Loai, d.BieuTuong, d.MauSac, d.LaMacDinh, d.LaHeThong);
}
