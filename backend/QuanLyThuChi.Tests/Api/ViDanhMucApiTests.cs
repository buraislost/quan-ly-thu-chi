using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Api;

public class ViDanhMucApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static readonly DateTime Ngay = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task<int> LayMa(HttpResponseMessage r, string truong) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty(truong).GetInt32();

    private static async Task<int> MaTaiKhoan(HttpClient client) =>
        await LayMa(await client.GetAsync("/api/tai-khoan/toi"), "maTaiKhoan");

    private async Task<int> TaoVi(HttpClient client, string ten, decimal soDu = 0)
    {
        var r = await client.PostAsJsonAsync("/api/vi", new { tenVi = ten, loaiVi = "NganHang", soDuBanDau = soDu });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        return await LayMa(r, "maVi");
    }

    private async Task<int> TaoDanhMuc(HttpClient client, string ten, string loai = "Chi")
    {
        var r = await client.PostAsJsonAsync("/api/danh-muc",
            new { tenDanhMuc = ten, loai, bieuTuong = "star", mauSac = "#112233" });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        return await LayMa(r, "maDanhMuc");
    }

    private int MaMacDinh(string ten, LoaiGiaoDich loai) =>
        factory.VoiDb(db => db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == ten && d.Loai == loai).MaDanhMuc);

    private void ThemGiaoDich(int maTk, int maVi, int maDm, decimal soTien = 10_000) => factory.VoiDb(db =>
    {
        var vi = db.Vi.Single(v => v.MaVi == maVi);
        var dm = db.DanhMuc.Single(d => d.MaDanhMuc == maDm);
        db.GiaoDich.Add(new GiaoDich(maTk, vi, dm, soTien, dm.Loai, Ngay, null));
        return db.SaveChanges();
    });

    // ----- Ví -----

    [Fact]
    public async Task ThemVi_ThanhCong_201VaLietKe()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.PostAsJsonAsync("/api/vi", new { tenVi = "  Techcombank ", loaiVi = "NganHang", soDuBanDau = 250_000 });

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("tenVi").GetString().Should().Be("Techcombank");
        json.GetProperty("loaiVi").GetString().Should().Be("NganHang");
        json.GetProperty("soDu").GetDecimal().Should().Be(250_000);

        var ds = await (await client.GetAsync("/api/vi")).Content.ReadFromJsonAsync<JsonElement>();
        ds.EnumerateArray().Select(x => x.GetProperty("tenVi").GetString()).Should().BeEquivalentTo("Tiền mặt", "Techcombank");
    }

    [Fact]
    public async Task ThemVi_SoDuAm_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.PostAsJsonAsync("/api/vi", new { tenVi = "A", loaiVi = "TienMat", soDuBanDau = -1 });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ThemVi_TrungTen_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        await TaoVi(client, "Vietcombank");

        var r = await client.PostAsJsonAsync("/api/vi", new { tenVi = " VIETCOMBANK ", loaiVi = "NganHang", soDuBanDau = 0 });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Tên ví đã tồn tại");
    }

    [Fact]
    public async Task ThemVi_TrungTenCuaNguoiKhac_DuocPhep()
    {
        var a = await factory.TaoClientDaDangNhap(EmailMoi());
        var b = await factory.TaoClientDaDangNhap(EmailMoi());
        await TaoVi(a, "Ví chung tên");

        await TaoVi(b, "Ví chung tên");
    }

    [Fact]
    public async Task SuaVi_DoiTenVaLoai_GiuNguyenSoDu()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoVi(client, "Cũ", 100_000);
        await TaoVi(client, "Khác");

        var r = await client.PutAsJsonAsync($"/api/vi/{ma}", new { tenVi = "Mới", loaiVi = "ViDienTu" });

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("tenVi").GetString().Should().Be("Mới");
        json.GetProperty("loaiVi").GetString().Should().Be("ViDienTu");
        json.GetProperty("soDu").GetDecimal().Should().Be(100_000);

        var trung = await client.PutAsJsonAsync($"/api/vi/{ma}", new { tenVi = "khác", loaiVi = "ViDienTu" });
        trung.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(trung)).Should().Be("Tên ví đã tồn tại");

        var giuTen = await client.PutAsJsonAsync($"/api/vi/{ma}", new { tenVi = "MỚI", loaiVi = "TienMat" });
        giuTen.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task XoaVi_KhongGiaoDich_204()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoVi(client, "Tạm");

        var r = await client.DeleteAsync($"/api/vi/{ma}");

        r.StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.VoiDb(db => db.Vi.Any(v => v.MaVi == ma)).Should().BeFalse();
    }

    [Fact]
    public async Task XoaVi_ConGiaoDich_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var ma = await TaoVi(client, "Có giao dịch", 100_000);
        ThemGiaoDich(maTk, ma, MaMacDinh("Ăn uống", LoaiGiaoDich.Chi));

        var r = await client.DeleteAsync($"/api/vi/{ma}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Ví còn giao dịch, không thể xóa");
        factory.VoiDb(db => db.Vi.Any(v => v.MaVi == ma)).Should().BeTrue();
    }

    [Fact]
    public async Task ViCuaNguoiKhac_404()
    {
        var a = await factory.TaoClientDaDangNhap(EmailMoi());
        var b = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoVi(a, "Của A");

        (await b.PutAsJsonAsync($"/api/vi/{ma}", new { tenVi = "Chiếm", loaiVi = "TienMat" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.DeleteAsync($"/api/vi/{ma}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.DeleteAsync("/api/vi/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var dsB = await (await b.GetAsync("/api/vi")).Content.ReadFromJsonAsync<JsonElement>();
        dsB.EnumerateArray().Select(x => x.GetProperty("maVi").GetInt32()).Should().NotContain(ma);
    }

    [Fact]
    public async Task Vi_KhongToken_401()
    {
        (await factory.CreateClient().GetAsync("/api/vi")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await factory.CreateClient().GetAsync("/api/danh-muc")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ----- Danh mục -----

    [Fact]
    public async Task LietKeDanhMuc_MacDinhTruocRoiCuaMinh_LocTheoLoai()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var khac = await factory.TaoClientDaDangNhap(EmailMoi());
        await TaoDanhMuc(client, "Cà phê");
        await TaoDanhMuc(client, "Âm nhạc");
        await TaoDanhMuc(client, "Freelance", "Thu");
        await TaoDanhMuc(khac, "Của người khác");

        var chi = await (await client.GetAsync("/api/danh-muc?loai=Chi")).Content.ReadFromJsonAsync<JsonElement>();
        var tenChi = chi.EnumerateArray().Select(x => x.GetProperty("tenDanhMuc").GetString()!).ToList();
        tenChi.Should().NotContain("Của người khác").And.NotContain("Freelance");
        tenChi.Should().Contain("Ăn uống").And.Contain("Tiết kiệm");
        tenChi.TakeLast(2).Should().Equal("Âm nhạc", "Cà phê");
        chi.EnumerateArray().Take(tenChi.Count - 2).Should().OnlyContain(x => x.GetProperty("laMacDinh").GetBoolean());
        chi.EnumerateArray().Single(x => x.GetProperty("tenDanhMuc").GetString() == "Tiết kiệm")
            .GetProperty("laHeThong").GetBoolean().Should().BeTrue();

        var thu = await (await client.GetAsync("/api/danh-muc?loai=Thu")).Content.ReadFromJsonAsync<JsonElement>();
        thu.EnumerateArray().Select(x => x.GetProperty("tenDanhMuc").GetString()).Should().Contain("Lương").And.Contain("Freelance")
            .And.NotContain("Cà phê");

        var tatCa = await (await client.GetAsync("/api/danh-muc")).Content.ReadFromJsonAsync<JsonElement>();
        tatCa.GetArrayLength().Should().Be(chi.GetArrayLength() + thu.GetArrayLength());
    }

    [Fact]
    public async Task ThemDanhMuc_201VaTrungTen_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await client.PostAsJsonAsync("/api/danh-muc",
            new { tenDanhMuc = " Cà phê ", loai = "Chi", bieuTuong = "coffee", mauSac = "#aabbcc" });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("tenDanhMuc").GetString().Should().Be("Cà phê");
        json.GetProperty("loai").GetString().Should().Be("Chi");
        json.GetProperty("laMacDinh").GetBoolean().Should().BeFalse();
        json.GetProperty("laHeThong").GetBoolean().Should().BeFalse();

        var trungRieng = await client.PostAsJsonAsync("/api/danh-muc", new { tenDanhMuc = "CÀ PHÊ", loai = "Chi" });
        trungRieng.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(trungRieng)).Should().Be("Tên danh mục đã tồn tại");

        var trungMacDinh = await client.PostAsJsonAsync("/api/danh-muc", new { tenDanhMuc = "ăn uống", loai = "Chi" });
        trungMacDinh.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(trungMacDinh)).Should().Be("Tên danh mục đã tồn tại");

        // Cùng tên nhưng khác loại thì được (mặc định cũng có "Khác" ở cả Thu lẫn Chi).
        (await client.PostAsJsonAsync("/api/danh-muc", new { tenDanhMuc = "Cà phê", loai = "Thu" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SuaDanhMuc_CuaMinh_DoiTenBieuTuongMau_GiuLoai()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoDanhMuc(client, "Cũ");

        var r = await client.PutAsJsonAsync($"/api/danh-muc/{ma}", new { tenDanhMuc = "Mới", bieuTuong = "x", mauSac = "#000000" });

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("tenDanhMuc").GetString().Should().Be("Mới");
        json.GetProperty("bieuTuong").GetString().Should().Be("x");
        json.GetProperty("mauSac").GetString().Should().Be("#000000");
        json.GetProperty("loai").GetString().Should().Be("Chi");

        var trung = await client.PutAsJsonAsync($"/api/danh-muc/{ma}", new { tenDanhMuc = "Ăn uống" });
        trung.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(trung)).Should().Be("Tên danh mục đã tồn tại");
    }

    [Fact]
    public async Task SuaDanhMucMacDinh_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);

        var r = await client.PutAsJsonAsync($"/api/danh-muc/{ma}", new { tenDanhMuc = "Đổi tên" });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Không thể sửa danh mục mặc định");
    }

    [Fact]
    public async Task XoaDanhMucMacDinh_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = MaMacDinh("Tiết kiệm", LoaiGiaoDich.Chi);

        var r = await client.DeleteAsync($"/api/danh-muc/{ma}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Không thể xóa danh mục mặc định");
    }

    [Fact]
    public async Task DanhMucCuaNguoiKhac_404()
    {
        var a = await factory.TaoClientDaDangNhap(EmailMoi());
        var b = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoDanhMuc(a, "Riêng của A");

        (await b.PutAsJsonAsync($"/api/danh-muc/{ma}", new { tenDanhMuc = "Chiếm" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await b.DeleteAsync($"/api/danh-muc/{ma}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task XoaDanhMuc_ChuaDung_204()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoDanhMuc(client, "Tạm");

        (await client.DeleteAsync($"/api/danh-muc/{ma}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.VoiDb(db => db.DanhMuc.Any(d => d.MaDanhMuc == ma)).Should().BeFalse();
    }

    [Fact]
    public async Task XoaDanhMucDangDung_KhongChonChuyenSang_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maVi = await TaoVi(client, "V");
        var ma = await TaoDanhMuc(client, "Đang dùng");
        ThemGiaoDich(maTk, maVi, ma);

        var r = await client.DeleteAsync($"/api/danh-muc/{ma}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Danh mục đang được sử dụng, vui lòng chọn danh mục để chuyển sang");
        factory.VoiDb(db => db.DanhMuc.Any(d => d.MaDanhMuc == ma)).Should().BeTrue();
    }

    [Fact]
    public async Task XoaDanhMucChiCoNganSach_KhongChonChuyenSang_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var ma = await TaoDanhMuc(client, "Có ngân sách");
        factory.VoiDb(db => { db.NganSach.Add(new NganSach(maTk, ma, 100_000, 11, 2026)); return db.SaveChanges(); });

        var r = await client.DeleteAsync($"/api/danh-muc/{ma}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Danh mục đang được sử dụng, vui lòng chọn danh mục để chuyển sang");
    }

    [Fact]
    public async Task XoaDanhMucDangDung_ChuyenSangKhacLoai_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maVi = await TaoVi(client, "V");
        var ma = await TaoDanhMuc(client, "Đang dùng");
        ThemGiaoDich(maTk, maVi, ma);

        var r = await client.DeleteAsync($"/api/danh-muc/{ma}?chuyenSang={MaMacDinh("Lương", LoaiGiaoDich.Thu)}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Danh mục chuyển sang phải cùng loại");
        factory.VoiDb(db => db.GiaoDich.Count(g => g.MaDanhMuc == ma)).Should().Be(1);
    }

    [Fact]
    public async Task XoaDanhMuc_ChuyenSangChinhNo_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoDanhMuc(client, "Chính nó");

        var r = await client.DeleteAsync($"/api/danh-muc/{ma}?chuyenSang={ma}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Danh mục chuyển sang phải cùng loại");
    }

    [Fact]
    public async Task XoaDanhMuc_ChuyenSangDanhMucHeThong_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var ma = await TaoDanhMuc(client, "Nguồn");

        var r = await client.DeleteAsync($"/api/danh-muc/{ma}?chuyenSang={MaMacDinh("Tiết kiệm", LoaiGiaoDich.Chi)}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Không thể chuyển sang danh mục hệ thống");
    }

    [Fact]
    public async Task XoaDanhMuc_ChuyenSangDanhMucNguoiKhac_404()
    {
        var a = await factory.TaoClientDaDangNhap(EmailMoi());
        var b = await factory.TaoClientDaDangNhap(EmailMoi());
        var maCuaB = await TaoDanhMuc(b, "Của B");
        var ma = await TaoDanhMuc(a, "Của A");

        (await a.DeleteAsync($"/api/danh-muc/{ma}?chuyenSang={maCuaB}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task XoaDanhMuc_ChuyenSang_GiaoDichDoiDanhMuc()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maVi = await TaoVi(client, "V", 1_000_000);
        var nguon = await TaoDanhMuc(client, "Nguồn");
        var dich = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        ThemGiaoDich(maTk, maVi, nguon, 30_000);
        ThemGiaoDich(maTk, maVi, nguon, 40_000);

        var r = await client.DeleteAsync($"/api/danh-muc/{nguon}?chuyenSang={dich}");

        r.StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.VoiDb(db =>
        {
            db.DanhMuc.Any(d => d.MaDanhMuc == nguon).Should().BeFalse();
            db.GiaoDich.Where(g => g.MaTaiKhoan == maTk).Select(g => g.MaDanhMuc).ToList().Should().Equal(dich, dich);
            return 0;
        });
    }

    [Fact]
    public async Task XoaDanhMuc_DichChuaCoNganSach_NganSachDuocChuyenSang()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var nguon = await TaoDanhMuc(client, "Nguồn");
        var dich = await TaoDanhMuc(client, "Đích");
        factory.VoiDb(db =>
        {
            db.NganSach.Add(new NganSach(maTk, nguon, 300_000, 11, 2026));
            return db.SaveChanges();
        });

        (await client.DeleteAsync($"/api/danh-muc/{nguon}?chuyenSang={dich}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        factory.VoiDb(db =>
        {
            var ns = db.NganSach.Where(n => n.MaTaiKhoan == maTk).ToList();
            ns.Should().ContainSingle();
            ns[0].MaDanhMuc.Should().Be(dich);
            ns[0].HanMuc.Should().Be(300_000);
            return 0;
        });
    }

    [Fact]
    public async Task XoaDanhMuc_DichDaCoNganSachCungThang_GopHanMuc()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var nguon = await TaoDanhMuc(client, "Nguồn");
        var dich = await TaoDanhMuc(client, "Đích");
        factory.VoiDb(db =>
        {
            db.NganSach.Add(new NganSach(maTk, nguon, 300_000, 11, 2026));
            db.NganSach.Add(new NganSach(maTk, dich, 200_000, 11, 2026));
            db.NganSach.Add(new NganSach(maTk, nguon, 100_000, 12, 2026)); // tháng khác: chỉ chuyển danh mục
            return db.SaveChanges();
        });

        var r = await client.DeleteAsync($"/api/danh-muc/{nguon}?chuyenSang={dich}");

        r.StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.VoiDb(db =>
        {
            var ns = db.NganSach.Where(n => n.MaTaiKhoan == maTk).OrderBy(n => n.Thang).ToList();
            ns.Should().HaveCount(2);
            ns.Should().OnlyContain(n => n.MaDanhMuc == dich);
            ns[0].Thang.Should().Be(11);
            ns[0].HanMuc.Should().Be(500_000);
            ns[1].Thang.Should().Be(12);
            ns[1].HanMuc.Should().Be(100_000);
            db.DanhMuc.Any(d => d.MaDanhMuc == nguon).Should().BeFalse();
            return 0;
        });
    }
}
