using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Mục tiêu tiết kiệm và nạp tiền từ ví vào mục tiêu (UC 2.6, 3.8).</summary>
public class MucTieuService(AppDbContext db, IDongHo dongHo, GiaoDichService giaoDich, ThongBaoService thongBao)
{
    public const string LoiHanChot = "Hạn chót phải sau hôm nay";
    public const string LoiDaCoTienNap = "Mục tiêu đã có tiền nạp, không thể xóa";
    public const string TenDanhMucTietKiem = "Tiết kiệm";

    /// <summary>Mục tiêu chưa hoàn thành trước, trong cùng nhóm xếp theo hạn chót tăng dần.</summary>
    public async Task<List<MucTieuDto>> LietKeAsync(int maTk, CancellationToken ct = default)
    {
        var ds = await db.MucTieuTietKiem.AsNoTracking().Where(m => m.MaTaiKhoan == maTk).ToListAsync(ct);
        return ds.OrderBy(m => m.DaHoanThanh()).ThenBy(m => m.HanChot).ThenBy(m => m.MaMucTieu).Select(ToDto).ToList();
    }

    public async Task<MucTieuDto> TaoAsync(int maTk, TaoMucTieuRequest req, CancellationToken ct = default)
    {
        KiemTraHanChot(req.HanChot);
        var m = new MucTieuTietKiem(maTk, req.TenMucTieu, req.SoTienMucTieu, req.HanChot, dongHo.HomNay);
        db.MucTieuTietKiem.Add(m);
        await db.SaveChangesAsync(ct);
        return ToDto(m);
    }

    public async Task<MucTieuDto> SuaAsync(int maTk, int maMucTieu, SuaMucTieuRequest req, CancellationToken ct = default)
    {
        var m = await LayCuaToiAsync(maTk, maMucTieu, ct);
        KiemTraHanChot(req.HanChot);
        m.DoiThongTin(req.TenMucTieu, req.SoTienMucTieu, req.HanChot);
        await db.SaveChangesAsync(ct);
        return ToDto(m);
    }

    public async Task XoaAsync(int maTk, int maMucTieu, CancellationToken ct = default)
    {
        var m = await LayCuaToiAsync(maTk, maMucTieu, ct);
        if (await db.GiaoDich.AnyAsync(g => g.MaMucTieu == maMucTieu, ct)) throw new DomainException(LoiDaCoTienNap);
        db.MucTieuTietKiem.Remove(m);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Nạp tiền: trừ ví, ghi giao dịch chi "Tiết kiệm", tăng số đã nạp và báo khi hoàn thành, tất cả trong một transaction.</summary>
    public async Task<MucTieuDto> NapTienAsync(int maTk, int maMucTieu, NapTienRequest req, CancellationToken ct = default)
    {
        var m = await LayCuaToiAsync(maTk, maMucTieu, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var maDanhMuc = await db.DanhMuc.AsNoTracking()
            .Where(d => d.LaHeThong && d.MaTaiKhoan == null && d.TenDanhMuc == TenDanhMucTietKiem)
            .Select(d => d.MaDanhMuc).SingleAsync(ct);
        await giaoDich.TaoAsync(maTk,
            new TaoGiaoDichRequest(req.MaVi, maDanhMuc, req.SoTien, LoaiGiaoDich.Chi, null, $"Nạp mục tiêu: {m.TenMucTieu}"),
            maMucTieu, ct);
        if (m.NapTien(req.SoTien))
            thongBao.Tao(maTk, LoaiThongBao.NhacMucTieu, $"Chúc mừng! Bạn đã hoàn thành mục tiêu {m.TenMucTieu}");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ToDto(m);
    }

    private void KiemTraHanChot(DateOnly hanChot)
    {
        if (hanChot <= dongHo.HomNay) throw new DomainException(LoiHanChot);
    }

    /// <summary>Mục tiêu của chính người dùng; của người khác hoặc không tồn tại đều là 404.</summary>
    private async Task<MucTieuTietKiem> LayCuaToiAsync(int maTk, int maMucTieu, CancellationToken ct) =>
        await db.MucTieuTietKiem.FirstOrDefaultAsync(m => m.MaMucTieu == maMucTieu && m.MaTaiKhoan == maTk, ct)
        ?? throw new KhongTimThayException();

    private MucTieuDto ToDto(MucTieuTietKiem m) =>
        new(m.MaMucTieu, m.TenMucTieu, m.SoTienMucTieu, m.SoTienHienTai, m.HanChot,
            Math.Round(m.TinhTienDo(), 1, MidpointRounding.AwayFromZero), m.SoTienCanMoiThang(dongHo.HomNay), m.DaHoanThanh());
}
