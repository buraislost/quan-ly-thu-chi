using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Tests.Api;

public class QuanTriApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static string EmailMoi() => $"u{Guid.NewGuid():N}@demo.vn";

    private static async Task<string> Message(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString()!;

    private int MaTheoEmail(string email) =>
        factory.VoiDb(db => db.TaiKhoan.Single(t => t.Email == email.ToLower()).MaTaiKhoan);

    private async Task<(string Email, int Ma)> TaoNguoiDung()
    {
        var email = EmailMoi();
        await factory.TaoClientDaDangNhap(email);
        return (email, MaTheoEmail(email));
    }

    // ----- Phân quyền -----

    [Fact]
    public async Task NguoiDungThuong_GoiAdmin_403()
    {
        var client = await factory.TaoClientDaDangNhap(EmailMoi());

        (await client.GetAsync("/api/admin/nguoi-dung")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/admin/thong-ke")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PostAsync("/api/admin/chay-tong-ket", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/admin/danh-muc-mac-dinh")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_KhongToken_401()
    {
        (await factory.CreateClient().GetAsync("/api/admin/thong-ke")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ----- Người dùng -----

    [Fact]
    public async Task LietKeNguoiDung_TimTheoTenHoacEmail_KhongGomQuanTri()
    {
        var admin = await factory.TaoClientAdmin();
        var (email, ma) = await TaoNguoiDung();

        var tatCa = await (await admin.GetAsync("/api/admin/nguoi-dung")).Content.ReadFromJsonAsync<JsonElement>();
        tatCa.EnumerateArray().Select(x => x.GetProperty("email").GetString()).Should().NotContain("admin@demo.vn");

        var tim = await (await admin.GetAsync($"/api/admin/nguoi-dung?q={email.ToUpper()}")).Content.ReadFromJsonAsync<JsonElement>();
        tim.GetArrayLength().Should().Be(1);
        tim[0].GetProperty("maTaiKhoan").GetInt32().Should().Be(ma);
        tim[0].GetProperty("trangThai").GetString().Should().Be("HoatDong");
        tim[0].GetProperty("soGiaoDich").GetInt32().Should().Be(0);

        var theoTen = await (await admin.GetAsync("/api/admin/nguoi-dung?q=NGƯỜI DÙNG THỬ")).Content.ReadFromJsonAsync<JsonElement>();
        theoTen.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Khoa_NguoiDungKhongDangNhapDuoc_VaNhanThongBaoSauKhiMoKhoa()
    {
        var admin = await factory.TaoClientAdmin();
        var (email, ma) = await TaoNguoiDung();
        var cu = await factory.TaoClientDaDangNhap(email); // token còn hạn trước khi khóa

        var khoa = await admin.PutAsJsonAsync($"/api/admin/nguoi-dung/{ma}/khoa", new { lyDo = "Vi phạm điều khoản" });
        khoa.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await cu.GetAsync("/api/vi")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var dangNhap = await factory.CreateClient().PostAsJsonAsync("/api/auth/dang-nhap", new { email, matKhau = "matkhau123" });
        dangNhap.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(dangNhap)).Should().Be("Tài khoản đã bị khóa");

        // Khóa lần nữa: idempotent, không tạo thêm thông báo.
        (await admin.PutAsJsonAsync($"/api/admin/nguoi-dung/{ma}/khoa", new { lyDo = "Lần hai" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await admin.PutAsync($"/api/admin/nguoi-dung/{ma}/mo-khoa", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.PutAsync($"/api/admin/nguoi-dung/{ma}/mo-khoa", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var lai = await factory.TaoClientDaDangNhap(email);
        var ds = await (await lai.GetAsync("/api/thong-bao")).Content.ReadFromJsonAsync<JsonElement>();
        var noiDung = ds.EnumerateArray().Select(x => x.GetProperty("noiDung").GetString()).ToList();
        noiDung.Should().BeEquivalentTo(
            "Tài khoản của bạn đã bị khóa. Lý do: Vi phạm điều khoản",
            "Tài khoản của bạn đã được mở khóa");
        ds.EnumerateArray().Should().OnlyContain(x => x.GetProperty("loai").GetString() == "HeThong");
    }

    [Fact]
    public async Task Khoa_ThieuLyDo_400()
    {
        var admin = await factory.TaoClientAdmin();
        var (_, ma) = await TaoNguoiDung();

        (await admin.PutAsJsonAsync($"/api/admin/nguoi-dung/{ma}/khoa", new { lyDo = "  " }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.VoiDb(db => db.TaiKhoan.Single(t => t.MaTaiKhoan == ma).TrangThai).Should().Be(TrangThaiTaiKhoan.HoatDong);
    }

    [Fact]
    public async Task Khoa_ChinhMinh_400()
    {
        var admin = await factory.TaoClientAdmin();
        var maAdmin = MaTheoEmail("admin@demo.vn");

        var r = await admin.PutAsJsonAsync($"/api/admin/nguoi-dung/{maAdmin}/khoa", new { lyDo = "thử" });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Không thể tự khóa tài khoản của mình");
    }

    [Fact]
    public async Task Khoa_QuanTriVienKhac_400()
    {
        var admin = await factory.TaoClientAdmin();
        var maKhac = factory.VoiDb(db =>
        {
            var qt = new QuanTriVien("Admin phụ", EmailMoi(), "hash", new DateTime(2026, 11, 8));
            db.TaiKhoan.Add(qt);
            db.SaveChanges();
            return qt.MaTaiKhoan;
        });

        var r = await admin.PutAsJsonAsync($"/api/admin/nguoi-dung/{maKhac}/khoa", new { lyDo = "thử" });

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Không thể khóa tài khoản quản trị viên");
    }

    [Fact]
    public async Task KhoaVaMoKhoa_KhongTonTai_404()
    {
        var admin = await factory.TaoClientAdmin();

        (await admin.PutAsJsonAsync("/api/admin/nguoi-dung/999999/khoa", new { lyDo = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PutAsync("/api/admin/nguoi-dung/999999/mo-khoa", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ----- Danh mục mặc định -----

    [Fact]
    public async Task DanhMucMacDinh_Them_NguoiDungThayDuoc()
    {
        var admin = await factory.TaoClientAdmin();
        var ten = "MacDinhMoi" + Guid.NewGuid().ToString("N")[..6];

        var r = await admin.PostAsJsonAsync("/api/admin/danh-muc-mac-dinh",
            new { tenDanhMuc = ten, loai = "Chi", bieuTuong = "gift", mauSac = "#123456" });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("laMacDinh").GetBoolean().Should().BeTrue();
        var ma = json.GetProperty("maDanhMuc").GetInt32();

        var user = await factory.TaoClientDaDangNhap(EmailMoi());
        var ds = await (await user.GetAsync("/api/danh-muc?loai=Chi")).Content.ReadFromJsonAsync<JsonElement>();
        ds.EnumerateArray().Select(x => x.GetProperty("tenDanhMuc").GetString()).Should().Contain(ten);

        // Trùng tên (không phân biệt hoa thường) cùng loại: 400.
        var trung = await admin.PostAsJsonAsync("/api/admin/danh-muc-mac-dinh", new { tenDanhMuc = ten.ToUpper(), loai = "Chi" });
        trung.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(trung)).Should().Be("Tên danh mục đã tồn tại");

        var sua = await admin.PutAsJsonAsync($"/api/admin/danh-muc-mac-dinh/{ma}", new { tenDanhMuc = ten + "X" });
        sua.StatusCode.Should().Be(HttpStatusCode.OK);

        (await admin.DeleteAsync($"/api/admin/danh-muc-mac-dinh/{ma}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.VoiDb(db => db.DanhMuc.Any(d => d.MaDanhMuc == ma)).Should().BeFalse();
    }

    [Fact]
    public async Task DanhMucMacDinh_HeThong_KhongSuaKhongXoa()
    {
        var admin = await factory.TaoClientAdmin();
        var ma = factory.VoiDb(db => db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.LaHeThong).MaDanhMuc);

        var sua = await admin.PutAsJsonAsync($"/api/admin/danh-muc-mac-dinh/{ma}", new { tenDanhMuc = "Đổi" });
        sua.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(sua)).Should().Be("Không thể sửa danh mục hệ thống");

        var xoa = await admin.DeleteAsync($"/api/admin/danh-muc-mac-dinh/{ma}");
        xoa.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(xoa)).Should().Be("Không thể xóa danh mục hệ thống");
    }

    [Fact]
    public async Task DanhMucMacDinh_CuaNguoiDung_404()
    {
        var admin = await factory.TaoClientAdmin();
        var user = await factory.TaoClientDaDangNhap(EmailMoi());
        var r = await user.PostAsJsonAsync("/api/danh-muc", new { tenDanhMuc = "Riêng", loai = "Chi" });
        var ma = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maDanhMuc").GetInt32();

        (await admin.DeleteAsync($"/api/admin/danh-muc-mac-dinh/{ma}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task XoaDanhMucMacDinhDangDung_400()
    {
        var admin = await factory.TaoClientAdmin();
        var (_, maTk) = await TaoNguoiDung();
        var ma = factory.VoiDb(db => db.DanhMuc.Single(d => d.MaTaiKhoan == null && d.TenDanhMuc == "Ăn uống" && d.Loai == LoaiGiaoDich.Chi).MaDanhMuc);
        factory.VoiDb(db => { db.NganSach.Add(new NganSach(maTk, ma, 100_000, 11, 2026)); return db.SaveChanges(); });

        var r = await admin.DeleteAsync($"/api/admin/danh-muc-mac-dinh/{ma}");

        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Message(r)).Should().Be("Danh mục mặc định đang được sử dụng, không thể xóa");
        factory.VoiDb(db => db.DanhMuc.Any(d => d.MaDanhMuc == ma)).Should().BeTrue();
    }

    // ----- Thống kê và tổng kết -----

    [Fact]
    public async Task ThongKe_DemDungNguoiDungVaGiaoDich()
    {
        var admin = await factory.TaoClientAdmin();
        var (emailTk, ma) = await TaoNguoiDung();
        var (hoatDong, biKhoa, tong, thangNay) = factory.VoiDb(db =>
        {
            var vi = db.Vi.First(v => v.MaTaiKhoan == ma);
            var dm = db.DanhMuc.First(d => d.MaTaiKhoan == null && d.Loai == LoaiGiaoDich.Chi);
            db.GiaoDich.Add(new GiaoDich(ma, vi, dm, 1000, dm.Loai, new DateTime(2026, 11, 3, 8, 0, 0), null));
            db.GiaoDich.Add(new GiaoDich(ma, vi, dm, 1000, dm.Loai, new DateTime(2026, 10, 30, 8, 0, 0), null));
            db.SaveChanges();
            var nd = db.Set<NguoiDung>();
            return (nd.Count(u => u.TrangThai == TrangThaiTaiKhoan.HoatDong), nd.Count(u => u.TrangThai == TrangThaiTaiKhoan.BiKhoa),
                db.GiaoDich.Count(), db.GiaoDich.Count(g => g.Ngay >= new DateTime(2026, 11, 1) && g.Ngay < new DateTime(2026, 12, 1)));
        });

        var json = await (await admin.GetAsync("/api/admin/thong-ke")).Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("soDangHoatDong").GetInt32().Should().Be(hoatDong);
        json.GetProperty("soBiKhoa").GetInt32().Should().Be(biKhoa);
        json.GetProperty("soNguoiDung").GetInt32().Should().Be(hoatDong + biKhoa);
        json.GetProperty("soGiaoDich").GetInt32().Should().Be(tong);
        json.GetProperty("soGiaoDichThangNay").GetInt32().Should().Be(thangNay);

        var ds = await (await admin.GetAsync("/api/admin/nguoi-dung?q=" + emailTk)).Content.ReadFromJsonAsync<JsonElement>();
        ds[0].GetProperty("soGiaoDich").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ChayTongKet_TraSoNguoiDaXuLy()
    {
        var admin = await factory.TaoClientAdmin();
        await TaoNguoiDung();
        var soHoatDong = factory.VoiDb(db => db.Set<NguoiDung>().Count(u => u.TrangThai == TrangThaiTaiKhoan.HoatDong));

        var r = await admin.PostAsync("/api/admin/chay-tong-ket?thang=10&nam=2026", null);

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("soNguoiDaXuLy").GetInt32().Should().Be(soHoatDong);

        // Mặc định: tháng hiện tại theo giờ Việt Nam.
        (await admin.PostAsync("/api/admin/chay-tong-ket", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.PostAsync("/api/admin/chay-tong-ket?thang=13&nam=2026", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
