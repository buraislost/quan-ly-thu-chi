using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Tests.Api;

public class BaoCaoApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static DateTime Ngay(int thang, int ngay, int nam = 2026) => new(nam, thang, ngay, 9, 0, 0, DateTimeKind.Unspecified);

    private static async Task<int> MaTaiKhoan(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi")).GetProperty("maTaiKhoan").GetInt32();

    private int MaMacDinh(string ten, LoaiGiaoDich loai) =>
        factory.VoiDb(db => db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == ten && d.Loai == loai).MaDanhMuc);

    private void ThemGiaoDich(int maTk, string tenDanhMuc, LoaiGiaoDich loai, decimal soTien, DateTime ngay) => factory.VoiDb(db =>
    {
        var vi = db.Vi.First(v => v.MaTaiKhoan == maTk);
        var dm = db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == tenDanhMuc && d.Loai == loai);
        db.GiaoDich.Add(new GiaoDich(maTk, vi, dm, soTien, loai, ngay, null));
        return db.SaveChanges();
    });

    private void LapNganSach(int maTk, string tenDanhMuc, decimal hanMuc, int thang, int nam) => factory.VoiDb(db =>
    {
        var dm = db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == tenDanhMuc && d.Loai == LoaiGiaoDich.Chi);
        db.NganSach.Add(new NganSach(maTk, dm.MaDanhMuc, hanMuc, thang, nam));
        return db.SaveChanges();
    });

    private async Task<T> ChayDichVu<T>(Func<BaoCaoService, Task<T>> viec)
    {
        using var scope = factory.TaoScope();
        return await viec(scope.ServiceProvider.GetRequiredService<BaoCaoService>());
    }

    [Fact]
    public async Task TongQuan_TinhDungThuChiChenhLech()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        ThemGiaoDich(maTk, "Lương", LoaiGiaoDich.Thu, 10_000_000, Ngay(11, 1));
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 800_000, Ngay(11, 2));
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 200_000, Ngay(11, 3));
        ThemGiaoDich(maTk, "Đi lại", LoaiGiaoDich.Chi, 500_000, Ngay(11, 4));
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 999_000, Ngay(10, 30)); // tháng khác
        LapNganSach(maTk, "Ăn uống", 1_100_000, 11, 2026); // 1.000.000 / 1.100.000 = 90,9%
        LapNganSach(maTk, "Đi lại", 5_000_000, 11, 2026); // 10%

        var r = await client.GetAsync("/api/bao-cao/tong-quan?thang=11&nam=2026");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("tongThu").GetDecimal().Should().Be(10_000_000);
        json.GetProperty("tongChi").GetDecimal().Should().Be(1_500_000);
        json.GetProperty("chenhLech").GetDecimal().Should().Be(8_500_000);
        var tongSoDu = factory.VoiDb(db => db.Vi.Where(v => v.MaTaiKhoan == maTk).AsEnumerable().Sum(v => v.SoDu));
        json.GetProperty("tongSoDu").GetDecimal().Should().Be(tongSoDu);

        var dm = json.GetProperty("chiTheoDanhMuc").EnumerateArray().ToList();
        dm.Should().HaveCount(2);
        dm[0].GetProperty("tenDanhMuc").GetString().Should().Be("Ăn uống");
        dm[0].GetProperty("soTien").GetDecimal().Should().Be(1_000_000);
        dm[0].GetProperty("tyLe").GetDouble().Should().Be(66.7);
        dm[1].GetProperty("tyLe").GetDouble().Should().Be(33.3);

        var sapVuot = json.GetProperty("sapVuot").EnumerateArray().ToList();
        sapVuot.Should().ContainSingle();
        sapVuot[0].GetProperty("tenDanhMuc").GetString().Should().Be("Ăn uống");

        var ganDay = json.GetProperty("ganDay").EnumerateArray().ToList();
        ganDay.Should().HaveCount(4);
        ganDay[0].GetProperty("soTien").GetDecimal().Should().Be(500_000);
    }

    [Fact]
    public async Task TheoDanhMuc_KhoangKhongHopLe_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.GetAsync("/api/bao-cao/theo-danh-muc?tu=2026-11-10&den=2026-11-01");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()
            .Should().Be("Khoảng thời gian không hợp lệ");
    }

    [Fact]
    public async Task TheoThoiGian_Du12Thang()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        ThemGiaoDich(maTk, "Lương", LoaiGiaoDich.Thu, 5_000_000, Ngay(3, 15));
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 300_000, Ngay(3, 20));

        var ds = (await client.GetFromJsonAsync<JsonElement>("/api/bao-cao/theo-thoi-gian?nam=2026")).EnumerateArray().ToList();

        ds.Should().HaveCount(12);
        ds.Select(x => x.GetProperty("thang").GetInt32()).Should().Equal(Enumerable.Range(1, 12));
        ds[2].GetProperty("thu").GetDecimal().Should().Be(5_000_000);
        ds[2].GetProperty("chi").GetDecimal().Should().Be(300_000);
        ds.Where((_, i) => i != 2).Should().OnlyContain(x => x.GetProperty("thu").GetDecimal() == 0 && x.GetProperty("chi").GetDecimal() == 0);
    }

    [Fact]
    public async Task TongKet_ChayHaiLan_MotBaoCaoMotThongBao()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        ThemGiaoDich(maTk, "Lương", LoaiGiaoDich.Thu, 2_000_000, Ngay(11, 1));
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 750_000, Ngay(11, 2));

        await ChayDichVu(s => s.TongKetThangAsync(maTk, 11, 2026));
        var thongBaoLan1 = factory.VoiDb(db => db.ThongBao.Where(t => t.MaTaiKhoan == maTk && t.Loai == LoaiThongBao.TongKetThang)
            .Select(t => t.NoiDung).ToList());
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 250_000, Ngay(11, 3));
        var bc = await ChayDichVu(s => s.TongKetThangAsync(maTk, 11, 2026));

        factory.VoiDb(db => db.BaoCaoThang.Count(b => b.MaTaiKhoan == maTk)).Should().Be(1);
        bc.TongThu.Should().Be(2_000_000);
        bc.TongChi.Should().Be(1_000_000); // lần hai cập nhật số liệu
        var thongBaoLan2 = factory.VoiDb(db => db.ThongBao.Where(t => t.MaTaiKhoan == maTk && t.Loai == LoaiThongBao.TongKetThang)
            .Select(t => t.NoiDung).ToList());
        thongBaoLan2.Should().HaveCount(thongBaoLan1.Count);
        thongBaoLan1.Should().Contain("Tổng kết tháng 11/2026: thu 2.000.000đ, chi 750.000đ, 0 danh mục vượt ngân sách");
        thongBaoLan1.Should().Contain("Hãy lập ngân sách cho tháng 12/2026");
    }

    [Fact]
    public async Task TongKet_ThangKhongGiaoDich_BaoCaoBang0()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);

        var bc = await ChayDichVu(s => s.TongKetThangAsync(maTk, 6, 2026));

        bc.TongThu.Should().Be(0);
        bc.TongChi.Should().Be(0);
        bc.SoDanhMucVuot.Should().Be(0);
        bc.Thang.Should().Be(6);
    }

    [Fact]
    public async Task TongKet_DemDanhMucVuot()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 600_000, Ngay(11, 2));
        ThemGiaoDich(maTk, "Đi lại", LoaiGiaoDich.Chi, 300_000, Ngay(11, 2));
        ThemGiaoDich(maTk, "Giải trí", LoaiGiaoDich.Chi, 100_000, Ngay(11, 2));
        LapNganSach(maTk, "Ăn uống", 500_000, 11, 2026);
        LapNganSach(maTk, "Đi lại", 200_000, 11, 2026);
        LapNganSach(maTk, "Giải trí", 1_000_000, 11, 2026);

        var bc = await ChayDichVu(s => s.TongKetThangAsync(maTk, 11, 2026));

        bc.SoDanhMucVuot.Should().Be(2);
        var cb = factory.VoiDb(db => db.ThongBao.Count(t => t.MaTaiKhoan == maTk && t.Loai == LoaiThongBao.CanhBaoNganSach));
        cb.Should().Be(2);
        // tháng sau chưa có ngân sách nên có nhắc lập mới
        factory.VoiDb(db => db.ThongBao.Any(t => t.MaTaiKhoan == maTk && t.NoiDung.StartsWith("Hãy lập"))).Should().BeTrue();
    }

    [Fact]
    public async Task TongKetTatCa_XuLyNguoiDungHoatDong()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);

        var n = await ChayDichVu(s => s.TongKetTatCaAsync(9, 2026));

        n.Should().BeGreaterThanOrEqualTo(1);
        factory.VoiDb(db => db.BaoCaoThang.Any(b => b.MaTaiKhoan == maTk && b.Thang == 9)).Should().BeTrue();
    }

    [Fact]
    public async Task DanhSachBaoCaoThang_MoiNhatTruoc()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        await ChayDichVu(s => s.TongKetThangAsync(maTk, 5, 2026));
        await ChayDichVu(s => s.TongKetThangAsync(maTk, 12, 2025));
        await ChayDichVu(s => s.TongKetThangAsync(maTk, 8, 2026));

        var ds = (await client.GetFromJsonAsync<JsonElement>("/api/bao-cao/thang")).EnumerateArray().ToList();

        ds.Select(x => (x.GetProperty("nam").GetInt32(), x.GetProperty("thang").GetInt32()))
            .Should().Equal((2026, 8), (2026, 5), (2025, 12));
    }

    [Fact]
    public async Task XuatExcel_TraFileXlsxCoHaiSheet()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        ThemGiaoDich(maTk, "Lương", LoaiGiaoDich.Thu, 3_000_000, Ngay(11, 1));
        ThemGiaoDich(maTk, "Ăn uống", LoaiGiaoDich.Chi, 400_000, Ngay(11, 2));

        var r = await client.GetAsync("/api/bao-cao/excel?tu=2026-11-01&den=2026-11-30");

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        r.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        r.Content.Headers.ContentDisposition!.FileName.Should().Be("bao-cao-20261101-20261130.xlsx");
        using var wb = new XLWorkbook(await r.Content.ReadAsStreamAsync());
        wb.Worksheets.Select(w => w.Name).Should().Equal("Giao dịch", "Tổng hợp");
        var gd = wb.Worksheet("Giao dịch");
        gd.Cell(1, 1).GetString().Should().Be("Ngày");
        gd.Cell(1, 5).GetString().Should().Be("Số tiền");
        gd.Cell(2, 3).GetString().Should().Be("Ăn uống"); // mới nhất trước
        gd.Cell(2, 5).GetDouble().Should().Be(400_000);
        gd.Cell(3, 3).GetString().Should().Be("Lương");
        var th = wb.Worksheet("Tổng hợp");
        th.CellsUsed().Select(c => c.GetString()).Should().Contain(["Tổng thu", "Tổng chi", "Chênh lệch"]);
    }

    [Fact]
    public async Task XuatExcel_KhoangKhongHopLe_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.GetAsync("/api/bao-cao/excel?tu=2026-11-30&den=2026-11-01");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/bao-cao/theo-danh-muc?tu=2026-11-01&den=9999-12-31")]
    [InlineData("/api/bao-cao/excel?tu=2026-11-01&den=9999-12-31")]
    public async Task DenLaNgayToiDa_400(string url)
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.GetAsync(url);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()
            .Should().Be("Khoảng thời gian không hợp lệ");
    }

    [Theory]
    [InlineData("/api/bao-cao/theo-danh-muc")]
    [InlineData("/api/bao-cao/theo-danh-muc?tu=2026-11-01")]
    [InlineData("/api/bao-cao/excel")]
    [InlineData("/api/bao-cao/excel?den=2026-11-30")]
    public async Task ThieuTuHoacDen_400(string url)
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.GetAsync(url);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()
            .Should().Be("Vui lòng chọn khoảng thời gian");
    }
}
