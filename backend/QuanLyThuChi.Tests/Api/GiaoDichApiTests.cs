using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Api;

public class GiaoDichApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private int MaDm(string ten, LoaiGiaoDich loai) =>
        factory.VoiDb(db => db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == ten && d.Loai == loai).MaDanhMuc);

    private static async Task<int> MaViTienMat(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/vi")).EnumerateArray().First().GetProperty("maVi").GetInt32();

    private static async Task<int> MaTaiKhoan(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi")).GetProperty("maTaiKhoan").GetInt32();

    private decimal SoDu(int maVi) => factory.VoiDb(db => db.Vi.AsNoTracking().Single(v => v.MaVi == maVi).SoDu);

    private int DemGiaoDich(int maTk) => factory.VoiDb(db => db.GiaoDich.Count(g => g.MaTaiKhoan == maTk));

    private static Task<HttpResponseMessage> Gui(HttpClient c, int maVi, int maDm, decimal soTien, LoaiGiaoDich loai,
        DateTime? ngay = null, string? ghiChu = null, object? hoaDon = null) =>
        c.PostAsJsonAsync("/api/giao-dich", new
        {
            maVi, maDanhMuc = maDm, soTien, loai = loai.ToString(), ngayGiaoDich = ngay, ghiChu, hoaDon,
        });

    private static async Task<int> TaoThanhCong(HttpClient c, int maVi, int maDm, decimal soTien, LoaiGiaoDich loai,
        DateTime? ngay = null, string? ghiChu = null)
    {
        var r = await Gui(c, maVi, maDm, soTien, loai, ngay, ghiChu);
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maGiaoDich").GetInt32();
    }

    private static readonly DateTime Ngay = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    /// <summary>Người dùng mới, ví tiền mặt có sẵn 1.000.000.</summary>
    private async Task<(HttpClient C, int Tk, int Vi)> NguoiDung(decimal soDu = 1_000_000)
    {
        var c = await factory.TaoClientDaDangNhap(EmailMoi());
        var vi = await MaViTienMat(c);
        if (soDu > 0) await TaoThanhCong(c, vi, MaDm("Lương", LoaiGiaoDich.Thu), soDu, LoaiGiaoDich.Thu, Ngay);
        return (c, await MaTaiKhoan(c), vi);
    }

    private static Task<HttpResponseMessage> LapNganSach(HttpClient c, int maDm, decimal hanMuc, int thang, int nam) =>
        c.PostAsJsonAsync("/api/ngan-sach", new { maDanhMuc = maDm, hanMuc, thang, nam });

    private static async Task<List<string>> ThongBao(HttpClient c) =>
        (await c.GetFromJsonAsync<JsonElement>("/api/thong-bao")).EnumerateArray()
        .Select(x => x.GetProperty("noiDung").GetString()!).ToList();

    [Fact]
    public async Task ThemChi_ViGiamDungSoTien()
    {
        var (c, _, vi) = await NguoiDung();
        var r = await Gui(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 250_000, LoaiGiaoDich.Chi, Ngay, "Bún bò");

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("soTien").GetDecimal().Should().Be(250_000);
        json.GetProperty("loai").GetString().Should().Be("Chi");
        json.GetProperty("tenVi").GetString().Should().Be("Tiền mặt");
        json.GetProperty("tenDanhMuc").GetString().Should().Be("Ăn uống");
        json.GetProperty("coHoaDon").GetBoolean().Should().BeFalse();
        SoDu(vi).Should().Be(750_000);
    }

    [Fact]
    public async Task ThemChi_VuotSoDu_400VaKhongLuu()
    {
        var (c, tk, vi) = await NguoiDung(100_000);
        var truoc = DemGiaoDich(tk);

        var r = await Gui(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 100_001, LoaiGiaoDich.Chi, Ngay);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Số dư ví không đủ");
        SoDu(vi).Should().Be(100_000);
        DemGiaoDich(tk).Should().Be(truoc);
    }

    [Fact]
    public async Task SuaDoiVi_ViCuHoanViMoiTru()
    {
        var (c, _, viA) = await NguoiDung();
        var viB = (await (await c.PostAsJsonAsync("/api/vi", new { tenVi = "Ngân hàng", loaiVi = "NganHang", soDuBanDau = 500_000 }))
            .Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maVi").GetInt32();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        var ma = await TaoThanhCong(c, viA, dm, 200_000, LoaiGiaoDich.Chi, Ngay);
        SoDu(viA).Should().Be(800_000);

        var r = await c.PutAsJsonAsync($"/api/giao-dich/{ma}", new
        {
            maVi = viB, maDanhMuc = dm, soTien = 300_000, loai = "Chi", ngayGiaoDich = Ngay, ghiChu = "Đổi ví",
        });

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maVi").GetInt32().Should().Be(viB);
        SoDu(viA).Should().Be(1_000_000);
        SoDu(viB).Should().Be(200_000);
    }

    [Fact]
    public async Task XoaGiaoDichThu_ViDaTieuHet_400VaCsdlKhongDoi()
    {
        var (c, tk, vi) = await NguoiDung(0);
        var thu = await TaoThanhCong(c, vi, MaDm("Lương", LoaiGiaoDich.Thu), 500_000, LoaiGiaoDich.Thu, Ngay);
        await TaoThanhCong(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 500_000, LoaiGiaoDich.Chi, Ngay);
        var truoc = DemGiaoDich(tk);

        var r = await c.DeleteAsync($"/api/giao-dich/{thu}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Số dư ví không đủ");
        SoDu(vi).Should().Be(0);
        DemGiaoDich(tk).Should().Be(truoc);
    }

    [Fact]
    public async Task SuaThuThanhChi_LamViAm_400VaCsdlKhongDoi()
    {
        var (c, _, vi) = await NguoiDung(0);
        var thu = await TaoThanhCong(c, vi, MaDm("Lương", LoaiGiaoDich.Thu), 100_000, LoaiGiaoDich.Thu, Ngay, "Gốc");

        var r = await c.PutAsJsonAsync($"/api/giao-dich/{thu}", new
        {
            maVi = vi, maDanhMuc = MaDm("Ăn uống", LoaiGiaoDich.Chi), soTien = 100_000, loai = "Chi", ngayGiaoDich = Ngay, ghiChu = "Sửa",
        });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Số dư ví không đủ");
        SoDu(vi).Should().Be(100_000);
        var gd = factory.VoiDb(db => db.GiaoDich.AsNoTracking().Single(g => g.MaGiaoDich == thu));
        gd.Loai.Should().Be(LoaiGiaoDich.Thu);
        gd.GhiChu.Should().Be("Gốc");
    }

    [Fact]
    public async Task SuaGhiChuKhoanThuDaTieu_200()
    {
        var (c, _, vi) = await NguoiDung(0);
        var luong = MaDm("Lương", LoaiGiaoDich.Thu);
        var thu = await TaoThanhCong(c, vi, luong, 1_000_000, LoaiGiaoDich.Thu, Ngay);
        await TaoThanhCong(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 900_000, LoaiGiaoDich.Chi, Ngay);

        var r = await c.PutAsJsonAsync($"/api/giao-dich/{thu}", new
        {
            maVi = vi, maDanhMuc = luong, soTien = 1_000_000, loai = "Thu", ghiChu = "Lương tháng 11",
        });

        r.StatusCode.Should().Be(HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        SoDu(vi).Should().Be(100_000);
        factory.VoiDb(db => db.GiaoDich.AsNoTracking().Single(g => g.MaGiaoDich == thu)).GhiChu.Should().Be("Lương tháng 11");
    }

    [Fact]
    public async Task TangSoTienKhoanThuDaTieu_200VaSoDuDung()
    {
        var (c, _, vi) = await NguoiDung(0);
        var luong = MaDm("Lương", LoaiGiaoDich.Thu);
        var thu = await TaoThanhCong(c, vi, luong, 1_000_000, LoaiGiaoDich.Thu, Ngay);
        await TaoThanhCong(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 900_000, LoaiGiaoDich.Chi, Ngay);

        var r = await c.PutAsJsonAsync($"/api/giao-dich/{thu}", new
        {
            maVi = vi, maDanhMuc = luong, soTien = 1_200_000, loai = "Thu",
        });

        r.StatusCode.Should().Be(HttpStatusCode.OK, await r.Content.ReadAsStringAsync());
        SoDu(vi).Should().Be(300_000);
    }

    [Fact]
    public async Task ThemChiThangTruoc_ChiAnhHuongNganSachThangTruoc()
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        await LapNganSach(c, dm, 1_000_000, 10, 2026);
        await LapNganSach(c, dm, 1_000_000, 11, 2026);

        await TaoThanhCong(c, vi, dm, 900_000, LoaiGiaoDich.Chi, new DateTime(2026, 10, 15, 12, 0, 0));

        var thang10 = (await c.GetFromJsonAsync<JsonElement>("/api/ngan-sach?thang=10&nam=2026")).EnumerateArray().Single();
        var thang11 = (await c.GetFromJsonAsync<JsonElement>("/api/ngan-sach?thang=11&nam=2026")).EnumerateArray().Single();
        thang10.GetProperty("soDaChi").GetDecimal().Should().Be(900_000);
        thang11.GetProperty("soDaChi").GetDecimal().Should().Be(0);
        (await ThongBao(c)).Should().ContainSingle(n => n.Contains("10/2026"));
        (await ThongBao(c)).Should().NotContain(n => n.Contains("11/2026"));
    }

    [Fact]
    public async Task ThemChi_UTC17h30Ngay31_10_TinhVaoThang11()
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        await LapNganSach(c, dm, 1_000_000, 11, 2026);
        var cu = factory.DongHo.BayGio;
        try
        {
            // 2026-10-31 17:30 UTC = 2026-11-01 00:30 giờ Việt Nam
            factory.DongHo.BayGio = new DateTime(2026, 11, 1, 0, 30, 0, DateTimeKind.Unspecified);
            var r = await Gui(c, vi, dm, 100_000, LoaiGiaoDich.Chi); // không gửi ngayGiaoDich
            r.StatusCode.Should().Be(HttpStatusCode.Created);
            (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ngayGiaoDich").GetDateTime()
                .Should().Be(new DateTime(2026, 11, 1, 0, 30, 0));
        }
        finally { factory.DongHo.BayGio = cu; }

        var ns = (await c.GetFromJsonAsync<JsonElement>("/api/ngan-sach?thang=11&nam=2026")).EnumerateArray().Single();
        ns.GetProperty("soDaChi").GetDecimal().Should().Be(100_000);
    }

    [Theory]
    [InlineData("2026-10-31T17:30:00Z")]
    [InlineData("2026-11-01T00:30:00+07:00")]
    public async Task ThemChi_NgayCoMuiGio_ChuanHoaGioVietNam(string ngay)
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        await LapNganSach(c, dm, 1_000_000, 11, 2026);

        var r = await c.PostAsJsonAsync("/api/giao-dich", new
        {
            maVi = vi, maDanhMuc = dm, soTien = 100_000, loai = "Chi", ngayGiaoDich = ngay,
        });

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ngayGiaoDich").GetDateTime()
            .Should().Be(new DateTime(2026, 11, 1, 0, 30, 0));
        var ns = (await c.GetFromJsonAsync<JsonElement>("/api/ngan-sach?thang=11&nam=2026")).EnumerateArray().Single();
        ns.GetProperty("soDaChi").GetDecimal().Should().Be(100_000);
    }

    [Fact]
    public async Task SuaChi_NgayCoMuiGio_ChuanHoaGioVietNam()
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        var ma = await TaoThanhCong(c, vi, dm, 1000, LoaiGiaoDich.Chi, Ngay);

        var r = await c.PutAsJsonAsync($"/api/giao-dich/{ma}", new
        {
            maVi = vi, maDanhMuc = dm, soTien = 1000, loai = "Chi", ngayGiaoDich = "2026-10-31T17:30:00Z",
        });

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ngayGiaoDich").GetDateTime()
            .Should().Be(new DateTime(2026, 11, 1, 0, 30, 0));
        factory.VoiDb(db => db.GiaoDich.AsNoTracking().Single(g => g.MaGiaoDich == ma).Ngay)
            .Should().Be(new DateTime(2026, 11, 1, 0, 30, 0));
    }

    [Fact]
    public async Task ThemChi_Dat85PhanTram_TaoThongBaoCanhBao()
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        await LapNganSach(c, dm, 100_000, 11, 2026);

        await TaoThanhCong(c, vi, dm, 85_000, LoaiGiaoDich.Chi, Ngay);

        (await ThongBao(c)).Should().ContainSingle(n => n.Contains("Ăn uống") && n.Contains("85%"));
    }

    [Fact]
    public async Task ThemKemHoaDon_LuuHoaDonVaChiTiet()
    {
        var (c, _, vi) = await NguoiDung();
        var hoaDon = new
        {
            tenCuaHang = "Circle K", ngayHoaDon = Ngay, anhHoaDon = "/uploads/hd1.jpg",
            chiTiet = new[]
            {
                new { tenMatHang = "Nước suối", soLuong = 2m, donGia = 10_000m },
                new { tenMatHang = "Bánh mì", soLuong = 1m, donGia = 25_000m },
            },
        };

        var r = await Gui(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 45_000, LoaiGiaoDich.Chi, Ngay, null, hoaDon);

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("coHoaDon").GetBoolean().Should().BeTrue();
        var ma = json.GetProperty("maGiaoDich").GetInt32();
        factory.VoiDb(db =>
        {
            var hd = db.HoaDon.Include(h => h.ChiTiet).Single(h => h.MaGiaoDich == ma);
            hd.TenCuaHang.Should().Be("Circle K");
            hd.TongTien.Should().Be(45_000);
            hd.ChiTiet.Should().HaveCount(2);
            return 0;
        });
        var ct = await c.GetFromJsonAsync<JsonElement>($"/api/giao-dich/{ma}");
        ct.GetProperty("hoaDon").GetProperty("chiTiet").GetArrayLength().Should().Be(2);
        ct.GetProperty("hoaDon").GetProperty("tongTien").GetDecimal().Should().Be(45_000);
    }

    [Theory]
    [InlineData(201, 10)]
    [InlineData(10, 201)]
    public async Task ThemKemHoaDon_TenQuaDai_400VaKhongLuu(int doDaiCuaHang, int doDaiMatHang)
    {
        var (c, tk, vi) = await NguoiDung();
        var truoc = DemGiaoDich(tk);
        var hoaDon = new
        {
            tenCuaHang = new string('a', doDaiCuaHang), anhHoaDon = "/uploads/hd.jpg",
            chiTiet = new[] { new { tenMatHang = new string('b', doDaiMatHang), soLuong = 1m, donGia = 10_000m } },
        };

        var r = await Gui(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 10_000, LoaiGiaoDich.Chi, Ngay, null, hoaDon);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var loi = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        loi.EnumerateObject().Select(e => e.Name).Should().ContainSingle()
            .Which.Should().Be(doDaiCuaHang > 200 ? "hoaDon.tenCuaHang" : "hoaDon.chiTiet[0].tenMatHang");
        DemGiaoDich(tk).Should().Be(truoc);
    }

    [Fact]
    public async Task ThemKemHoaDon_NgayHoaDonCoMuiGio_ChuanHoaGioVietNam()
    {
        var (c, _, vi) = await NguoiDung();
        var hoaDon = new
        {
            ngayHoaDon = "2026-10-31T17:30:00Z", anhHoaDon = "/uploads/hd.jpg",
            chiTiet = new[] { new { tenMatHang = "Cà phê", soLuong = 1m, donGia = 30_000m } },
        };

        var r = await Gui(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 30_000, LoaiGiaoDich.Chi, Ngay, null, hoaDon);

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var ma = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maGiaoDich").GetInt32();
        factory.VoiDb(db => db.HoaDon.AsNoTracking().Single(h => h.MaGiaoDich == ma).NgayHoaDon)
            .Should().Be(new DateTime(2026, 11, 1, 0, 30, 0));
    }

    [Fact]
    public async Task ThemKemHoaDon_GiaoDichThu_400()
    {
        var (c, _, vi) = await NguoiDung(0);
        var hoaDon = new { anhHoaDon = "/a.jpg", chiTiet = new[] { new { tenMatHang = "x", soLuong = 1m, donGia = 1m } } };

        var r = await Gui(c, vi, MaDm("Lương", LoaiGiaoDich.Thu), 1000, LoaiGiaoDich.Thu, Ngay, null, hoaDon);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Chỉ giao dịch chi mới đính kèm hóa đơn");
    }

    [Fact]
    public async Task ChonDanhMucHeThong_400()
    {
        var (c, _, vi) = await NguoiDung();

        var r = await Gui(c, vi, MaDm("Tiết kiệm", LoaiGiaoDich.Chi), 1000, LoaiGiaoDich.Chi, Ngay);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Không thể chọn danh mục hệ thống");
    }

    [Fact]
    public async Task GiaoDichNapMucTieu_SuaXoa_400()
    {
        var (c, tk, vi) = await NguoiDung();
        var maMt = factory.VoiDb(db =>
        {
            var mt = new MucTieuTietKiem(tk, "Mua xe", 10_000_000, new DateOnly(2027, 12, 31), new DateOnly(2026, 11, 8));
            db.MucTieuTietKiem.Add(mt);
            db.SaveChanges();
            return mt.MaMucTieu;
        });
        var ma = factory.VoiDb(db =>
        {
            var g = new GiaoDich(tk, db.Vi.Single(v => v.MaVi == vi), db.DanhMuc.Single(d => d.MaDanhMuc == MaDm("Tiết kiệm", LoaiGiaoDich.Chi)),
                1000, LoaiGiaoDich.Chi, Ngay, null, maMt);
            db.GiaoDich.Add(g);
            db.SaveChanges();
            return g.MaGiaoDich;
        });

        var xoa = await c.DeleteAsync($"/api/giao-dich/{ma}");
        var sua = await c.PutAsJsonAsync($"/api/giao-dich/{ma}", new
        {
            maVi = vi, maDanhMuc = MaDm("Ăn uống", LoaiGiaoDich.Chi), soTien = 500, loai = "Chi", ngayGiaoDich = Ngay,
        });

        xoa.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(xoa)).Should().Be("Giao dịch nạp mục tiêu không thể sửa hoặc xóa");
        sua.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task XoaGiaoDichChi_HoanTienVaoVi_204()
    {
        var (c, _, vi) = await NguoiDung();
        var ma = await TaoThanhCong(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 300_000, LoaiGiaoDich.Chi, Ngay);

        var r = await c.DeleteAsync($"/api/giao-dich/{ma}");

        r.StatusCode.Should().Be(HttpStatusCode.NoContent);
        SoDu(vi).Should().Be(1_000_000);
    }

    [Fact]
    public async Task TimKiem_TuLonHonDen_400()
    {
        var (c, _, _) = await NguoiDung(0);

        var r = await c.GetAsync("/api/giao-dich?tu=2026-11-10&den=2026-11-01");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Khoảng thời gian không hợp lệ");
    }

    [Theory]
    [InlineData("/api/giao-dich?den=9999-12-31")]
    [InlineData("/api/giao-dich?tu=2026-11-01&den=9999-12-31")]
    public async Task TimKiem_DenLaNgayToiDa_400(string url)
    {
        var (c, _, _) = await NguoiDung(0);

        var r = await c.GetAsync(url);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Khoảng thời gian không hợp lệ");
    }

    [Fact]
    public async Task TimKiem_TrangVuotQua_TraRong()
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        for (var i = 0; i < 3; i++) await TaoThanhCong(c, vi, dm, 1000, LoaiGiaoDich.Chi, Ngay);

        var json = await c.GetFromJsonAsync<JsonElement>("/api/giao-dich?trang=5");

        json.GetProperty("muc").GetArrayLength().Should().Be(0);
        json.GetProperty("tongSo").GetInt32().Should().Be(4); // 1 thu + 3 chi
        json.GetProperty("trang").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task TimKiem_PhanTrang20VaMoiNhatTruoc()
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        for (var i = 0; i < 21; i++) await TaoThanhCong(c, vi, dm, 1000, LoaiGiaoDich.Chi, Ngay.AddDays(i + 1), $"gd{i}");

        var t1 = await c.GetFromJsonAsync<JsonElement>("/api/giao-dich?trang=0");
        var t2 = await c.GetFromJsonAsync<JsonElement>("/api/giao-dich?trang=2");

        t1.GetProperty("trang").GetInt32().Should().Be(1);
        t1.GetProperty("muc").GetArrayLength().Should().Be(20);
        t1.GetProperty("muc")[0].GetProperty("ghiChu").GetString().Should().Be("gd20");
        t1.GetProperty("tongSo").GetInt32().Should().Be(22);
        t2.GetProperty("muc").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task TimKiem_Q_LocTheoGhiChu()
    {
        var (c, _, vi) = await NguoiDung();
        var dm = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        await TaoThanhCong(c, vi, dm, 1000, LoaiGiaoDich.Chi, Ngay, "Cà phê sáng");
        await TaoThanhCong(c, vi, dm, 1000, LoaiGiaoDich.Chi, Ngay, "Cơm trưa");

        var json = await c.GetFromJsonAsync<JsonElement>("/api/giao-dich?q=CƠM");

        json.GetProperty("tongSo").GetInt32().Should().Be(1);
        json.GetProperty("muc")[0].GetProperty("ghiChu").GetString().Should().Be("Cơm trưa");
    }

    [Fact]
    public async Task TimKiem_LocTheoKhoangNgayLoaiVaDenBaoGomCaNgay()
    {
        var (c, _, vi) = await NguoiDung(0);
        var dmChi = MaDm("Ăn uống", LoaiGiaoDich.Chi);
        await TaoThanhCong(c, vi, MaDm("Lương", LoaiGiaoDich.Thu), 1_000_000, LoaiGiaoDich.Thu, new DateTime(2026, 10, 1, 8, 0, 0));
        await TaoThanhCong(c, vi, dmChi, 1000, LoaiGiaoDich.Chi, new DateTime(2026, 10, 5, 23, 59, 0), "trong");
        await TaoThanhCong(c, vi, dmChi, 1000, LoaiGiaoDich.Chi, new DateTime(2026, 10, 6, 0, 0, 0), "ngoai");

        var json = await c.GetFromJsonAsync<JsonElement>($"/api/giao-dich?tu=2026-10-02&den=2026-10-05&loai=Chi&danhMucId={dmChi}&viId={vi}");

        json.GetProperty("tongSo").GetInt32().Should().Be(1);
        json.GetProperty("muc")[0].GetProperty("ghiChu").GetString().Should().Be("trong");
    }

    [Fact]
    public async Task GiaoDichNguoiKhac_404()
    {
        var (c, _, vi) = await NguoiDung();
        var ma = await TaoThanhCong(c, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 1000, LoaiGiaoDich.Chi, Ngay);
        var khac = await factory.TaoClientDaDangNhap(EmailMoi());
        var viKhac = await MaViTienMat(khac);

        (await khac.GetAsync($"/api/giao-dich/{ma}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await khac.DeleteAsync($"/api/giao-dich/{ma}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await khac.PutAsJsonAsync($"/api/giao-dich/{ma}", new
        {
            maVi = viKhac, maDanhMuc = MaDm("Ăn uống", LoaiGiaoDich.Chi), soTien = 1, loai = "Chi",
        })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Gui(khac, vi, MaDm("Ăn uống", LoaiGiaoDich.Chi), 1, LoaiGiaoDich.Chi, Ngay)).StatusCode
            .Should().Be(HttpStatusCode.NotFound); // ví của người khác
        SoDu(vi).Should().Be(999_000);
    }
}
