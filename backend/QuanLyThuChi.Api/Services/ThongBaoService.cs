using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Thông báo trong ứng dụng và cài đặt nhắc nhở (UC 2.9, 3.11).</summary>
public class ThongBaoService(AppDbContext db, IDongHo dongHo)
{
    public const string LoiGioNhac = "Giờ nhắc không hợp lệ (định dạng HH:mm)";
    private const string DinhDangGio = "HH:mm";
    private const int SoLuongToiDa = 100;

    /// <summary>Chỉ Add vào DbContext; người gọi tự SaveChanges cùng transaction của mình.</summary>
    public void Tao(int maTk, LoaiThongBao loai, string noiDung) =>
        db.ThongBao.Add(new ThongBao(maTk, loai, noiDung, dongHo.BayGio));

    /// <summary>Đã có thông báo loại này được tạo trong ngày hôm nay (giờ Việt Nam) chưa.</summary>
    public async Task<bool> DaGuiHomNayAsync(int maTk, LoaiThongBao loai, CancellationToken ct = default)
    {
        var dau = dongHo.HomNay.ToDateTime(TimeOnly.MinValue);
        var cuoi = dau.AddDays(1);
        return await db.ThongBao.AsNoTracking()
            .AnyAsync(t => t.MaTaiKhoan == maTk && t.Loai == loai && t.NgayTao >= dau && t.NgayTao < cuoi, ct);
    }

    public async Task<List<ThongBaoDto>> LietKeAsync(int maTk, bool chiChuaDoc, CancellationToken ct = default)
    {
        var q = db.ThongBao.AsNoTracking().Where(t => t.MaTaiKhoan == maTk);
        if (chiChuaDoc) q = q.Where(t => !t.DaDoc);
        var ds = await q.OrderByDescending(t => t.NgayTao).ThenByDescending(t => t.MaThongBao)
            .Take(SoLuongToiDa).ToListAsync(ct);
        return ds.Select(t => new ThongBaoDto(t.MaThongBao, t.Loai, t.NoiDung, t.DaDoc, t.NgayTao)).ToList();
    }

    public Task<int> DemChuaDocAsync(int maTk, CancellationToken ct = default) =>
        db.ThongBao.CountAsync(t => t.MaTaiKhoan == maTk && !t.DaDoc, ct);

    public async Task DanhDauDaDocAsync(int maTk, int maThongBao, CancellationToken ct = default)
    {
        (await LayCuaToiAsync(maTk, maThongBao, ct)).DanhDauDaDoc();
        await db.SaveChangesAsync(ct);
    }

    public async Task DanhDauTatCaDaDocAsync(int maTk, CancellationToken ct = default)
    {
        var chuaDoc = await db.ThongBao.Where(t => t.MaTaiKhoan == maTk && !t.DaDoc).ToListAsync(ct);
        foreach (var t in chuaDoc) t.DanhDauDaDoc();
        await db.SaveChangesAsync(ct);
    }

    public async Task XoaAsync(int maTk, int maThongBao, CancellationToken ct = default)
    {
        db.ThongBao.Remove(await LayCuaToiAsync(maTk, maThongBao, ct));
        await db.SaveChangesAsync(ct);
    }

    public async Task<CaiDatDto> LayCaiDatAsync(int maTk, CancellationToken ct = default) =>
        ToDto(await LayCaiDatCuaToiAsync(maTk, ct));

    public async Task<CaiDatDto> LuuCaiDatAsync(int maTk, CaiDatDto req, CancellationToken ct = default)
    {
        if (!TimeOnly.TryParseExact(req.GioNhac, DinhDangGio, CultureInfo.InvariantCulture, DateTimeStyles.None, out var gio))
            throw new DomainException(LoiGioNhac);
        var caiDat = await LayCaiDatCuaToiAsync(maTk, ct);
        caiDat.NhacHangNgay = req.NhacHangNgay;
        caiDat.GioNhac = gio;
        caiDat.NhacNganSach = req.NhacNganSach;
        caiDat.NhacMucTieu = req.NhacMucTieu;
        await db.SaveChangesAsync(ct);
        return ToDto(caiDat);
    }

    public async Task<CaiDatDto> KhoiPhucMacDinhAsync(int maTk, CancellationToken ct = default)
    {
        var caiDat = await LayCaiDatCuaToiAsync(maTk, ct);
        caiDat.KhoiPhucMacDinh();
        await db.SaveChangesAsync(ct);
        return ToDto(caiDat);
    }

    /// <summary>Thông báo của chính người dùng; của người khác hoặc không tồn tại đều là 404.</summary>
    private async Task<ThongBao> LayCuaToiAsync(int maTk, int maThongBao, CancellationToken ct) =>
        await db.ThongBao.FirstOrDefaultAsync(t => t.MaThongBao == maThongBao && t.MaTaiKhoan == maTk, ct)
        ?? throw new KhongTimThayException();

    /// <summary>Quản trị viên không có cài đặt nhắc nhở nên nhận 404.</summary>
    private async Task<CaiDatNhacNho> LayCaiDatCuaToiAsync(int maTk, CancellationToken ct) =>
        await db.CaiDatNhacNho.FirstOrDefaultAsync(c => EF.Property<int>(c, "MaTaiKhoan") == maTk, ct)
        ?? throw new KhongTimThayException();

    private static CaiDatDto ToDto(CaiDatNhacNho c) =>
        new(c.NhacHangNgay, c.GioNhac.ToString(DinhDangGio, CultureInfo.InvariantCulture), c.NhacNganSach, c.NhacMucTieu);
}
