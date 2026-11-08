using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Ngân sách theo danh mục chi và tháng, cùng cảnh báo khi chi gần hoặc vượt hạn mức (UC 2.5, 3.7; R6).</summary>
public class NganSachService(AppDbContext db, IDongHo dongHo, DanhMucService danhMuc, ThongBaoService thongBao)
{
    public const string LoiTrung = "Danh mục này đã có ngân sách trong tháng";
    public const string LoiKhongPhaiChi = "Chỉ lập ngân sách cho danh mục chi";
    public const string LoiHeThong = "Không thể lập ngân sách cho danh mục hệ thống";
    public const string LoiThang = "Tháng không hợp lệ";
    public const string LoiNam = "Năm không hợp lệ";

    /// <summary>Ngân sách của tháng (mặc định tháng hiện tại theo giờ Việt Nam), xếp theo tên danh mục.</summary>
    public async Task<List<NganSachDto>> LietKeAsync(int maTk, int? thang, int? nam, CancellationToken ct = default)
    {
        var homNay = dongHo.HomNay;
        var t = thang ?? homNay.Month;
        var n = nam ?? homNay.Year;
        if (t is < 1 or > 12) throw new DomainException(LoiThang);
        if (n is < 2000 or > 2100) throw new DomainException(LoiNam);

        var ds = await db.NganSach.AsNoTracking()
            .Where(x => x.MaTaiKhoan == maTk && x.Thang == t && x.Nam == n).ToListAsync(ct);
        if (ds.Count == 0) return [];

        var maDms = ds.Select(x => x.MaDanhMuc).ToList();
        var ten = await db.DanhMuc.AsNoTracking().Where(d => maDms.Contains(d.MaDanhMuc))
            .ToDictionaryAsync(d => d.MaDanhMuc, d => d.TenDanhMuc, ct);
        var chi = await LayChiTrongThangAsync(maTk, maDms, t, n, ct);

        return ds.Select(x => ToDto(x, ten[x.MaDanhMuc], x.TinhSoDaChi(chi)))
            .OrderBy(x => x.TenDanhMuc, StringComparer.CurrentCulture).ToList();
    }

    public async Task<NganSachDto> LapAsync(int maTk, TaoNganSachRequest req, CancellationToken ct = default)
    {
        var dm = await danhMuc.LayDungDuocAsync(maTk, req.MaDanhMuc, ct);
        if (dm.Loai != LoaiGiaoDich.Chi) throw new DomainException(LoiKhongPhaiChi);
        if (dm.LaHeThong) throw new DomainException(LoiHeThong);

        var ns = new NganSach(maTk, dm.MaDanhMuc, req.HanMuc, req.Thang, req.Nam, req.NguongCanhBao ?? 80);
        if (await db.NganSach.AnyAsync(x => x.MaTaiKhoan == maTk && x.MaDanhMuc == dm.MaDanhMuc
                                            && x.Thang == req.Thang && x.Nam == req.Nam, ct))
            throw new DomainException(LoiTrung);

        // Tính cờ theo mức chi sẵn có để không phát cảnh báo cho trạng thái đã biết khi lập ngân sách.
        var soDaChi = await TinhSoDaChiAsync(ns, ct);
        ns.CapNhatHanMuc(req.HanMuc, soDaChi);
        db.NganSach.Add(ns);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) // trùng khóa duy nhất do hai yêu cầu đồng thời
        {
            throw new DomainException(LoiTrung);
        }
        return ToDto(ns, dm.TenDanhMuc, soDaChi);
    }

    public async Task<NganSachDto> SuaAsync(int maTk, int maNganSach, SuaNganSachRequest req, CancellationToken ct = default)
    {
        var ns = await LayCuaToiAsync(maTk, maNganSach, ct);
        if (req.NguongCanhBao is { } nguong) ns.DoiNguong(nguong);
        var soDaChi = await TinhSoDaChiAsync(ns, ct);
        ns.CapNhatHanMuc(req.HanMuc, soDaChi);
        await db.SaveChangesAsync(ct);
        var ten = await db.DanhMuc.AsNoTracking().Where(d => d.MaDanhMuc == ns.MaDanhMuc)
            .Select(d => d.TenDanhMuc).SingleAsync(ct);
        return ToDto(ns, ten, soDaChi);
    }

    public async Task XoaAsync(int maTk, int maNganSach, CancellationToken ct = default)
    {
        db.NganSach.Remove(await LayCuaToiAsync(maTk, maNganSach, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Gọi sau khi giao dịch chi đã được ghi vào CSDL (cùng transaction với người gọi): tính số đã chi của tháng truyền vào,
    /// bật cờ cảnh báo và thêm thông báo nếu người dùng bật nhắc ngân sách. Không gọi SaveChanges; người gọi tự lưu.
    /// </summary>
    public async Task KiemTraSauGiaoDichAsync(int maTk, int maDanhMuc, int thang, int nam, CancellationToken ct = default)
    {
        var ns = await db.NganSach.FirstOrDefaultAsync(x => x.MaTaiKhoan == maTk && x.MaDanhMuc == maDanhMuc
                                                            && x.Thang == thang && x.Nam == nam, ct);
        if (ns is null) return;

        var soDaChi = await TinhSoDaChiAsync(ns, ct);
        var ketQua = ns.KiemTraVuot(soDaChi);
        if (ketQua == KetQuaCanhBao.KhongCo) return;

        // Quản trị viên không có cài đặt nhưng cũng không có ngân sách; thiếu cài đặt coi như bật.
        var nhac = await db.CaiDatNhacNho
            .Where(c => EF.Property<int>(c, "MaTaiKhoan") == maTk)
            .Select(c => (bool?)c.NhacNganSach).FirstOrDefaultAsync(ct);
        if (nhac == false) return;

        var ten = await db.DanhMuc.AsNoTracking().Where(d => d.MaDanhMuc == maDanhMuc)
            .Select(d => d.TenDanhMuc).SingleAsync(ct);
        var ky = string.Create(CultureInfo.InvariantCulture, $"{thang:00}/{nam}");
        var noiDung = ketQua == KetQuaCanhBao.Vuot
            ? $"Ngân sách {ten} tháng {ky} đã vượt hạn mức"
            : $"Ngân sách {ten} tháng {ky} đã dùng {(int)Math.Round(soDaChi / ns.HanMuc * 100m, MidpointRounding.AwayFromZero)}%";
        thongBao.Tao(maTk, LoaiThongBao.CanhBaoNganSach, noiDung);
    }

    /// <summary>Giao dịch chi của người dùng trong tháng [mùng 1, mùng 1 tháng sau), lọc theo khoảng ngày để chạy được trên mọi CSDL.</summary>
    private async Task<List<GiaoDich>> LayChiTrongThangAsync(int maTk, List<int> maDanhMucs, int thang, int nam, CancellationToken ct)
    {
        var dau = new DateTime(nam, thang, 1);
        var cuoi = dau.AddMonths(1);
        return await db.GiaoDich.AsNoTracking()
            .Where(g => g.MaTaiKhoan == maTk && g.Loai == LoaiGiaoDich.Chi && maDanhMucs.Contains(g.MaDanhMuc)
                        && g.Ngay >= dau && g.Ngay < cuoi)
            .ToListAsync(ct);
    }

    private async Task<decimal> TinhSoDaChiAsync(NganSach ns, CancellationToken ct) =>
        ns.TinhSoDaChi(await LayChiTrongThangAsync(ns.MaTaiKhoan, [ns.MaDanhMuc], ns.Thang, ns.Nam, ct));

    /// <summary>Ngân sách của chính người dùng; của người khác hoặc không tồn tại đều là 404.</summary>
    private async Task<NganSach> LayCuaToiAsync(int maTk, int maNganSach, CancellationToken ct) =>
        await db.NganSach.FirstOrDefaultAsync(x => x.MaNganSach == maNganSach && x.MaTaiKhoan == maTk, ct)
        ?? throw new KhongTimThayException();

    private static NganSachDto ToDto(NganSach ns, string tenDanhMuc, decimal soDaChi) =>
        new(ns.MaNganSach, ns.MaDanhMuc, tenDanhMuc, ns.HanMuc, soDaChi,
            Math.Round(ns.TinhTienDo(soDaChi), 1, MidpointRounding.AwayFromZero), ns.NguongCanhBao);
}
