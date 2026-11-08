using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Quản lý ví của người dùng (UC 2.2).</summary>
public class ViService(AppDbContext db, IDongHo dongHo)
{
    public const string LoiTrungTen = "Tên ví đã tồn tại";
    public const string LoiConGiaoDich = "Ví còn giao dịch, không thể xóa";

    public async Task<List<ViDto>> LietKeAsync(int maTk, CancellationToken ct = default) =>
        (await db.Vi.AsNoTracking().Where(v => v.MaTaiKhoan == maTk).OrderBy(v => v.MaVi).ToListAsync(ct))
        .Select(ToDto).ToList();

    /// <summary>Ví của chính người dùng; ví của người khác hoặc không tồn tại đều là 404.</summary>
    public async Task<Vi> LayCuaToiAsync(int maTk, int maVi, CancellationToken ct = default) =>
        await db.Vi.FirstOrDefaultAsync(v => v.MaVi == maVi && v.MaTaiKhoan == maTk, ct)
        ?? throw new KhongTimThayException();

    public async Task<ViDto> TaoAsync(int maTk, TaoViRequest req, CancellationToken ct = default)
    {
        var vi = new Vi(maTk, req.TenVi, req.LoaiVi, req.SoDuBanDau, dongHo.BayGio);
        await KiemTraTrungTenAsync(maTk, vi.TenVi, null, ct);
        db.Vi.Add(vi);
        await db.SaveChangesAsync(ct);
        return ToDto(vi);
    }

    public async Task<ViDto> SuaAsync(int maTk, int maVi, SuaViRequest req, CancellationToken ct = default)
    {
        var vi = await LayCuaToiAsync(maTk, maVi, ct);
        vi.DoiThongTin(req.TenVi, req.LoaiVi);
        await KiemTraTrungTenAsync(maTk, vi.TenVi, maVi, ct);
        await db.SaveChangesAsync(ct);
        return ToDto(vi);
    }

    public async Task XoaAsync(int maTk, int maVi, CancellationToken ct = default)
    {
        var vi = await LayCuaToiAsync(maTk, maVi, ct);
        if (await db.GiaoDich.AnyAsync(g => g.MaVi == maVi, ct))
            throw new DomainException(LoiConGiaoDich);
        db.Vi.Remove(vi);
        await db.SaveChangesAsync(ct);
    }

    // So sánh trong bộ nhớ để không phụ thuộc collation của CSDL (SQL Server không phân biệt hoa thường, SQLite có).
    private async Task KiemTraTrungTenAsync(int maTk, string ten, int? boQuaMa, CancellationToken ct)
    {
        var cacTen = await db.Vi.AsNoTracking()
            .Where(v => v.MaTaiKhoan == maTk && v.MaVi != boQuaMa)
            .Select(v => v.TenVi).ToListAsync(ct);
        if (cacTen.Any(t => string.Equals(t.Trim(), ten.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException(LoiTrungTen);
    }

    private static ViDto ToDto(Vi v) => new(v.MaVi, v.TenVi, v.LoaiVi, v.SoDu);
}
