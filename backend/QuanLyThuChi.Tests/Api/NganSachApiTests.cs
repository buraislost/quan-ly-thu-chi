using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using QuanLyThuChi.Data;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Tests.Api;

public class NganSachApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static readonly DateTime NgayTrongThang = new(2026, 11, 8, 9, 0, 0, DateTimeKind.Unspecified);

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private static async Task<int> MaTaiKhoan(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi")).GetProperty("maTaiKhoan").GetInt32();

    private int MaMacDinh(string ten, LoaiGiaoDich loai) =>
        factory.VoiDb(db => db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == ten && d.Loai == loai).MaDanhMuc);

    /// <summary>Thêm một giao dịch trực tiếp vào CSDL (ví tiền mặt mặc định của người dùng).</summary>
    private void ThemGiaoDich(int maTk, int maDm, decimal soTien, DateTime ngay) => factory.VoiDb(db =>
    {
        var vi = db.Vi.First(v => v.MaTaiKhoan == maTk);
        var dm = db.DanhMuc.Single(d => d.MaDanhMuc == maDm);
        db.GiaoDich.Add(new GiaoDich(maTk, vi, dm, soTien, dm.Loai, ngay, null));
        return db.SaveChanges();
    });

    private static Task<HttpResponseMessage> Lap(HttpClient client, int maDm, decimal hanMuc, int thang = 11, int nam = 2026,
        int? nguong = null) =>
        client.PostAsJsonAsync("/api/ngan-sach", new { maDanhMuc = maDm, hanMuc, thang, nam, nguongCanhBao = nguong });

    private (bool Nguong, bool Vuot) Co(int maNganSach) => factory.VoiDb(db =>
    {
        var n = db.NganSach.Single(x => x.MaNganSach == maNganSach);
        return (n.DaCanhBaoNguong, n.DaCanhBaoVuot);
    });

    [Fact]
    public async Task LapNganSach_ThanhCong_201()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);

        var r = await Lap(client, maDm, 1_000_000);

        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("maDanhMuc").GetInt32().Should().Be(maDm);
        json.GetProperty("tenDanhMuc").GetString().Should().Be("Ăn uống");
        json.GetProperty("hanMuc").GetDecimal().Should().Be(1_000_000);
        json.GetProperty("soDaChi").GetDecimal().Should().Be(0);
        json.GetProperty("tienDo").GetDouble().Should().Be(0);
        json.GetProperty("nguongCanhBao").GetInt32().Should().Be(80);
    }

    [Fact]
    public async Task LapNganSach_DanhMucThu_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await Lap(client, MaMacDinh("Lương", LoaiGiaoDich.Thu), 1_000_000);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Chỉ lập ngân sách cho danh mục chi");
    }

    [Fact]
    public async Task LapNganSach_DanhMucHeThong_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        var r = await Lap(client, MaMacDinh("Tiết kiệm", LoaiGiaoDich.Chi), 1_000_000);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Không thể lập ngân sách cho danh mục hệ thống");
    }

    [Fact]
    public async Task LapNganSach_DanhMucCuaNguoiKhac_404()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var khac = await factory.TaoClientDaDangNhap(EmailMoi());
        var rDm = await khac.PostAsJsonAsync("/api/danh-muc", new { tenDanhMuc = "Riêng tư", loai = "Chi" });
        var maDm = (await rDm.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maDanhMuc").GetInt32();

        (await Lap(client, maDm, 1_000_000)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LapNganSach_Trung_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        (await Lap(client, maDm, 1_000_000)).StatusCode.Should().Be(HttpStatusCode.Created);

        var r = await Lap(client, maDm, 2_000_000);

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Danh mục này đã có ngân sách trong tháng");
        // tháng khác thì được
        (await Lap(client, maDm, 2_000_000, thang: 12)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task LapNganSach_HanMucKhongHopLe_400()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);

        var r = await Lap(client, maDm, 0);
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var r2 = await Lap(client, maDm, 1_000, nguong: 101);
        r2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var r3 = await Lap(client, maDm, 1_000, thang: 13);
        r3.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DanhSach_TinhSoDaChiVaTienDo()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var anUong = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        var diLai = MaMacDinh("Đi lại", LoaiGiaoDich.Chi);
        await Lap(client, anUong, 1_000_000);
        await Lap(client, diLai, 300_000);
        await Lap(client, anUong, 500_000, thang: 12);

        ThemGiaoDich(maTk, anUong, 600_000, new(2026, 11, 1, 0, 0, 0));        // biên đầu tháng: tính
        ThemGiaoDich(maTk, anUong, 250_000, new(2026, 11, 30, 23, 59, 59));    // biên cuối tháng: tính
        ThemGiaoDich(maTk, anUong, 111_111, new(2026, 10, 31, 23, 59, 59));    // tháng trước: không
        ThemGiaoDich(maTk, anUong, 222_222, new(2026, 12, 1, 0, 0, 0));        // tháng sau: không
        ThemGiaoDich(maTk, diLai, 100_000, NgayTrongThang);
        ThemGiaoDich(maTk, MaMacDinh("Lương", LoaiGiaoDich.Thu), 9_000_000, NgayTrongThang); // khoản thu: không

        var ds = await client.GetFromJsonAsync<JsonElement>("/api/ngan-sach?thang=11&nam=2026");

        ds.GetArrayLength().Should().Be(2);
        var an = ds.EnumerateArray().Single(x => x.GetProperty("maDanhMuc").GetInt32() == anUong);
        an.GetProperty("soDaChi").GetDecimal().Should().Be(850_000);
        an.GetProperty("tienDo").GetDouble().Should().Be(85.0);
        var di = ds.EnumerateArray().Single(x => x.GetProperty("maDanhMuc").GetInt32() == diLai);
        di.GetProperty("soDaChi").GetDecimal().Should().Be(100_000);
        di.GetProperty("tienDo").GetDouble().Should().Be(33.3);

        var thang12 = await client.GetFromJsonAsync<JsonElement>("/api/ngan-sach?thang=12&nam=2026");
        thang12.GetArrayLength().Should().Be(1);
        thang12[0].GetProperty("soDaChi").GetDecimal().Should().Be(222_222);
    }

    [Fact]
    public async Task DanhSach_MacDinhThangHienTai_VaChiCuaToi()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var khac = await factory.TaoClientDaDangNhap(EmailMoi());
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        await Lap(client, maDm, 1_000_000);          // 11/2026 = tháng của đồng hồ giả
        await Lap(client, maDm, 1_000_000, thang: 12);
        await Lap(khac, maDm, 5_000_000);

        var ds = await client.GetFromJsonAsync<JsonElement>("/api/ngan-sach");

        ds.GetArrayLength().Should().Be(1);
        ds[0].GetProperty("hanMuc").GetDecimal().Should().Be(1_000_000);
    }

    [Fact]
    public async Task SuaHanMuc_TinhLaiCo()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        ThemGiaoDich(maTk, maDm, 900_000, NgayTrongThang);
        var tao = await Lap(client, maDm, 1_000_000);
        var ma = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maNganSach").GetInt32();
        Co(ma).Should().Be((true, false)); // tạo mới tính cờ theo mức chi hiện có, không phát thông báo

        var rong = await client.PutAsJsonAsync($"/api/ngan-sach/{ma}", new { hanMuc = 2_000_000 });
        rong.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await rong.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("hanMuc").GetDecimal().Should().Be(2_000_000);
        json.GetProperty("soDaChi").GetDecimal().Should().Be(900_000);
        json.GetProperty("tienDo").GetDouble().Should().Be(45.0);
        Co(ma).Should().Be((false, false));

        (await client.PutAsJsonAsync($"/api/ngan-sach/{ma}", new { hanMuc = 800_000 })).StatusCode.Should().Be(HttpStatusCode.OK);
        Co(ma).Should().Be((true, true));

        // đổi ngưỡng: 95% của 1.000.000 -> 90% chưa chạm ngưỡng
        var doi = await client.PutAsJsonAsync($"/api/ngan-sach/{ma}", new { hanMuc = 1_000_000, nguongCanhBao = 95 });
        (await doi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("nguongCanhBao").GetInt32().Should().Be(95);
        Co(ma).Should().Be((false, false));

        factory.VoiDb(db => db.ThongBao.Count(t => t.MaTaiKhoan == maTk)).Should().Be(0);
    }

    [Fact]
    public async Task Sua_NguongKhongHopLe_400_VaKhongCoNganSach_404()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        var ma = (await (await Lap(client, maDm, 1_000_000)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("maNganSach").GetInt32();

        (await client.PutAsJsonAsync($"/api/ngan-sach/{ma}", new { hanMuc = 1_000_000, nguongCanhBao = 0 }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync("/api/ngan-sach/99999999", new { hanMuc = 1_000_000 }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Xoa_204_VaNganSachNguoiKhac_404()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var khac = await factory.TaoClientDaDangNhap(EmailMoi());
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        var ma = (await (await Lap(client, maDm, 1_000_000)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("maNganSach").GetInt32();

        (await khac.DeleteAsync($"/api/ngan-sach/{ma}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await khac.PutAsJsonAsync($"/api/ngan-sach/{ma}", new { hanMuc = 5 })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await client.DeleteAsync($"/api/ngan-sach/{ma}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<JsonElement>("/api/ngan-sach")).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ChuaDangNhap_401()
    {
        (await factory.CreateClient().GetAsync("/api/ngan-sach")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ----- KiemTraSauGiaoDichAsync (Task 11 sẽ gọi) -----

    /// <summary>Gọi KiemTraSauGiaoDichAsync trong một scope riêng rồi SaveChanges, trả về nội dung các thông báo của người dùng.</summary>
    private async Task<List<string>> ChayKiemTra(int maTk, int maDm, int thang = 11, int nam = 2026)
    {
        using var scope = factory.TaoScope();
        await scope.ServiceProvider.GetRequiredService<NganSachService>().KiemTraSauGiaoDichAsync(maTk, maDm, thang, nam);
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().SaveChangesAsync();
        return factory.VoiDb(db => db.ThongBao.Where(t => t.MaTaiKhoan == maTk && t.Loai == LoaiThongBao.CanhBaoNganSach)
            .OrderBy(t => t.MaThongBao).Select(t => t.NoiDung).ToList());
    }

    [Fact]
    public async Task KiemTraSauGiaoDich_NguongRoiVuot_MoiMucMotThongBao()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        await Lap(client, maDm, 1_000_000);

        (await ChayKiemTra(maTk, maDm)).Should().BeEmpty();

        ThemGiaoDich(maTk, maDm, 850_000, NgayTrongThang);
        (await ChayKiemTra(maTk, maDm)).Should().Equal("Ngân sách Ăn uống tháng 11/2026 đã dùng 85%");

        // gọi lại: cờ đã bật nên không thêm thông báo
        (await ChayKiemTra(maTk, maDm)).Should().HaveCount(1);

        ThemGiaoDich(maTk, maDm, 250_000, NgayTrongThang);
        (await ChayKiemTra(maTk, maDm)).Should().Equal(
            "Ngân sách Ăn uống tháng 11/2026 đã dùng 85%",
            "Ngân sách Ăn uống tháng 11/2026 đã vượt hạn mức");
        (await ChayKiemTra(maTk, maDm)).Should().HaveCount(2);
    }

    [Fact]
    public async Task KiemTraSauGiaoDich_TinhTheoThangTruyenVao()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        await Lap(client, maDm, 1_000_000, thang: 10);
        await Lap(client, maDm, 1_000_000, thang: 11);
        ThemGiaoDich(maTk, maDm, 900_000, new(2026, 10, 15, 8, 0, 0));

        // tháng 11 chưa chi gì -> không cảnh báo; tháng 10 -> cảnh báo 90%
        (await ChayKiemTra(maTk, maDm, 11, 2026)).Should().BeEmpty();
        (await ChayKiemTra(maTk, maDm, 10, 2026)).Should().Equal("Ngân sách Ăn uống tháng 10/2026 đã dùng 90%");
    }

    [Fact]
    public async Task KiemTraSauGiaoDich_KhongCoNganSach_KhongLam()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        ThemGiaoDich(maTk, maDm, 5_000_000, NgayTrongThang);

        (await ChayKiemTra(maTk, maDm)).Should().BeEmpty();
    }

    [Fact]
    public async Task KiemTraSauGiaoDich_TatNhacNganSach_VanBatCoNhungKhongThongBao()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());
        var maTk = await MaTaiKhoan(client);
        var maDm = MaMacDinh("Ăn uống", LoaiGiaoDich.Chi);
        var ma = (await (await Lap(client, maDm, 1_000_000)).Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("maNganSach").GetInt32();
        var cd = await client.GetFromJsonAsync<JsonElement>("/api/thong-bao/cai-dat");
        (await client.PutAsJsonAsync("/api/thong-bao/cai-dat", new
        {
            nhacHangNgay = cd.GetProperty("nhacHangNgay").GetBoolean(),
            gioNhac = cd.GetProperty("gioNhac").GetString(),
            nhacNganSach = false,
            nhacMucTieu = cd.GetProperty("nhacMucTieu").GetBoolean(),
        })).StatusCode.Should().Be(HttpStatusCode.OK);
        ThemGiaoDich(maTk, maDm, 850_000, NgayTrongThang);

        (await ChayKiemTra(maTk, maDm)).Should().BeEmpty();
        Co(ma).Should().Be((true, false));
    }
}
