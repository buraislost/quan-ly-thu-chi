using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Services;

/// <summary>Báo cáo thu chi, tổng kết tháng và dữ liệu xuất Excel (UC 2.7, 3.9, 3.12; R9).</summary>
public class BaoCaoService(AppDbContext db, IDongHo dongHo, NganSachService nganSach, ThongBaoService thongBao,
    ILogger<BaoCaoService> logger)
{
    private const int SoGiaoDichGanDay = 5;
    private const double NguongSapVuot = 80;
    private static readonly CultureInfo ViVN = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>Tổng quan một tháng (mặc định tháng hiện tại theo giờ Việt Nam).</summary>
    public async Task<TongQuanDto> TongQuanAsync(int maTk, int? thang, int? nam, CancellationToken ct = default)
    {
        var homNay = dongHo.HomNay;
        var t = thang ?? homNay.Month;
        var n = nam ?? homNay.Year;
        if (t is < 1 or > 12) throw new DomainException(NganSachService.LoiThang);
        if (n is < 2000 or > 2100) throw new DomainException(NganSachService.LoiNam);

        var dau = new DateOnly(n, t, 1);
        var tq = await TongQuanKhoangAsync(maTk, dau, dau.AddMonths(1).AddDays(-1), ct);
        var sapVuot = (await nganSach.LietKeAsync(maTk, t, n, ct)).Where(x => x.TienDo >= NguongSapVuot)
            .OrderByDescending(x => x.TienDo).ToList();
        return tq with { SapVuot = sapVuot };
    }

    /// <summary>Số liệu tổng hợp của khoảng ngày [tu, den] (gồm cả hai đầu); không có ngân sách sắp vượt.</summary>
    public async Task<TongQuanDto> TongQuanKhoangAsync(int maTk, DateOnly tu, DateOnly den, CancellationToken ct = default)
    {
        KiemTraKhoang(tu, den);
        var ds = await LayGiaoDichAsync(maTk, tu, den, ct);
        var thu = ds.Where(g => g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien);
        var chi = ds.Where(g => g.Loai == LoaiGiaoDich.Chi).Sum(g => g.SoTien);
        var soDu = (await db.Vi.AsNoTracking().Where(v => v.MaTaiKhoan == maTk).Select(v => v.SoDu).ToListAsync(ct)).Sum();
        var ganDay = ds.Take(SoGiaoDichGanDay).Select(GiaoDichService.ToDto).ToList();
        return new TongQuanDto(thu, chi, thu - chi, soDu, NhomTheoDanhMuc(ds, LoaiGiaoDich.Chi), [], ganDay);
    }

    public async Task<List<TheoDanhMucDto>> TheoDanhMucAsync(int maTk, DateOnly tu, DateOnly den, LoaiGiaoDich loai,
        CancellationToken ct = default)
    {
        KiemTraKhoang(tu, den);
        return NhomTheoDanhMuc(await LayGiaoDichAsync(maTk, tu, den, ct), loai);
    }

    /// <summary>Thu chi theo từng tháng trong năm; luôn đủ 12 tháng, tháng trống bằng 0.</summary>
    public async Task<List<TheoThangDto>> TheoThoiGianAsync(int maTk, int? nam, CancellationToken ct = default)
    {
        var n = nam ?? dongHo.HomNay.Year;
        if (n is < 2000 or > 2100) throw new DomainException(NganSachService.LoiNam);
        var dau = new DateTime(n, 1, 1);
        var cuoi = dau.AddYears(1);
        var ds = await db.GiaoDich.AsNoTracking()
            .Where(g => g.MaTaiKhoan == maTk && g.Ngay >= dau && g.Ngay < cuoi)
            .Select(g => new { g.Ngay, g.Loai, g.SoTien }).ToListAsync(ct);
        return Enumerable.Range(1, 12).Select(t => new TheoThangDto(t,
            ds.Where(g => g.Ngay.Month == t && g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien),
            ds.Where(g => g.Ngay.Month == t && g.Loai == LoaiGiaoDich.Chi).Sum(g => g.SoTien))).ToList();
    }

    public async Task<List<BaoCaoThangDto>> LietKeBaoCaoAsync(int maTk, CancellationToken ct = default) =>
        (await db.BaoCaoThang.AsNoTracking().Where(b => b.MaTaiKhoan == maTk).ToListAsync(ct))
        .OrderByDescending(b => b.Nam).ThenByDescending(b => b.Thang)
        .Select(b => new BaoCaoThangDto(b.Thang, b.Nam, b.TongThu, b.TongChi, b.SoDanhMucVuot, b.NgayLap)).ToList();

    /// <summary>Mọi giao dịch trong khoảng ngày, mới nhất trước (dùng cho xuất Excel).</summary>
    public async Task<List<GiaoDichDto>> LayGiaoDichDtoAsync(int maTk, DateOnly tu, DateOnly den, CancellationToken ct = default)
    {
        KiemTraKhoang(tu, den);
        return (await LayGiaoDichAsync(maTk, tu, den, ct)).Select(GiaoDichService.ToDto).ToList();
    }

    /// <summary>
    /// Lập hoặc cập nhật báo cáo tháng (R9: mỗi tháng một bản ghi). Thông báo tổng kết chỉ gửi ở lần tạo đầu tiên.
    /// </summary>
    public async Task<BaoCaoThang> TongKetThangAsync(int maTk, int thang, int nam, CancellationToken ct = default)
    {
        if (thang is < 1 or > 12) throw new DomainException(NganSachService.LoiThang);
        if (nam is < 2000 or > 2100) throw new DomainException(NganSachService.LoiNam);

        var dau = new DateTime(nam, thang, 1);
        var cuoi = dau.AddMonths(1);
        var gd = await db.GiaoDich.AsNoTracking()
            .Where(g => g.MaTaiKhoan == maTk && g.Ngay >= dau && g.Ngay < cuoi)
            .Select(g => new { g.Loai, g.SoTien }).ToListAsync(ct);
        var thu = gd.Where(g => g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien);
        var chi = gd.Where(g => g.Loai == LoaiGiaoDich.Chi).Sum(g => g.SoTien);
        var vuot = (await nganSach.LietKeAsync(maTk, thang, nam, ct)).Where(x => x.SoDaChi > x.HanMuc).ToList();

        var tx = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        await using var _ = tx;
        foreach (var x in vuot)
            await nganSach.KiemTraSauGiaoDichAsync(maTk, x.MaDanhMuc, thang, nam, ct);

        var bc = await db.BaoCaoThang.FirstOrDefaultAsync(b => b.MaTaiKhoan == maTk && b.Thang == thang && b.Nam == nam, ct);
        var moi = bc is null;
        if (bc is null)
        {
            bc = new BaoCaoThang(maTk, thang, nam);
            db.BaoCaoThang.Add(bc);
        }
        bc.LapBaoCao(thu, chi, vuot.Count, dongHo.BayGio);

        if (moi)
        {
            var ky = string.Create(CultureInfo.InvariantCulture, $"{thang:00}/{nam}");
            thongBao.Tao(maTk, LoaiThongBao.TongKetThang,
                string.Create(ViVN, $"Tổng kết tháng {ky}: thu {thu:N0}đ, chi {chi:N0}đ, {vuot.Count} danh mục vượt ngân sách"));

            var thangSau = dau.AddMonths(1);
            var coNganSachSau = await db.NganSach.AnyAsync(
                x => x.MaTaiKhoan == maTk && x.Thang == thangSau.Month && x.Nam == thangSau.Year, ct);
            if (!coNganSachSau)
                thongBao.Tao(maTk, LoaiThongBao.TongKetThang,
                    string.Create(CultureInfo.InvariantCulture, $"Hãy lập ngân sách cho tháng {thangSau.Month:00}/{thangSau.Year}"));
        }

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return bc;
    }

    /// <summary>Tổng kết tháng cho mọi người dùng đang hoạt động; lỗi của một người không làm dừng những người khác.</summary>
    public async Task<int> TongKetTatCaAsync(int thang, int nam, CancellationToken ct = default)
    {
        var ids = await db.Set<NguoiDung>().AsNoTracking().Where(u => u.TrangThai == TrangThaiTaiKhoan.HoatDong)
            .Select(u => u.MaTaiKhoan).ToListAsync(ct);
        var thanhCong = 0;
        foreach (var id in ids)
        {
            try
            {
                await TongKetThangAsync(id, thang, nam, ct);
                thanhCong++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Tổng kết tháng {Thang}/{Nam} lỗi cho tài khoản {MaTk}", thang, nam, id);
                db.ChangeTracker.Clear(); // bỏ thay đổi dở dang của người này trước khi sang người kế tiếp
            }
        }
        return thanhCong;
    }

    private static void KiemTraKhoang(DateOnly tu, DateOnly den)
    {
        if (tu > den) throw new DomainException(GiaoDichService.LoiKhoangThoiGian);
    }

    /// <summary>Giao dịch trong [tu, den] gồm cả hai đầu, mới nhất trước, kèm ví và danh mục.</summary>
    private async Task<List<GiaoDich>> LayGiaoDichAsync(int maTk, DateOnly tu, DateOnly den, CancellationToken ct)
    {
        var dau = tu.ToDateTime(TimeOnly.MinValue);
        var cuoi = den.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var ds = await db.GiaoDich.AsNoTracking().Include(g => g.Vi).Include(g => g.DanhMuc).Include(g => g.HoaDon)
            .Where(g => g.MaTaiKhoan == maTk && g.Ngay >= dau && g.Ngay < cuoi).ToListAsync(ct);
        return ds.OrderByDescending(g => g.Ngay).ThenByDescending(g => g.MaGiaoDich).ToList();
    }

    private static List<TheoDanhMucDto> NhomTheoDanhMuc(IEnumerable<GiaoDich> ds, LoaiGiaoDich loai)
    {
        var nhom = ds.Where(g => g.Loai == loai).GroupBy(g => (g.MaDanhMuc, g.DanhMuc.TenDanhMuc))
            .Select(x => (x.Key.MaDanhMuc, x.Key.TenDanhMuc, SoTien: x.Sum(g => g.SoTien))).ToList();
        var tong = nhom.Sum(x => x.SoTien);
        return nhom.OrderByDescending(x => x.SoTien).ThenBy(x => x.TenDanhMuc, StringComparer.CurrentCulture)
            .Select(x => new TheoDanhMucDto(x.MaDanhMuc, x.TenDanhMuc, x.SoTien,
                tong == 0 ? 0 : Math.Round((double)(x.SoTien / tong * 100m), 1, MidpointRounding.AwayFromZero))).ToList();
    }
}
