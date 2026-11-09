using FluentAssertions;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Tests.Domain;

public class GeminiPhanTichTests
{
    private const string JsonHopLe = """
        {"tenCuaHang":"Bách Hóa Xanh","ngay":"2026-11-07","tongTien":75000,
         "matHang":[{"ten":"Sữa tươi","soLuong":2,"donGia":15000},{"ten":"Gạo ST25","soLuong":1,"donGia":45000}]}
        """;

    [Fact]
    public void PhanTich_JsonHopLe_DocDayDu()
    {
        var kq = GeminiDichVuOCR.PhanTich(JsonHopLe);

        kq.TenCuaHang.Should().Be("Bách Hóa Xanh");
        kq.Ngay.Should().Be(new DateOnly(2026, 11, 7));
        kq.TongTien.Should().Be(75_000m);
        kq.MatHang.Should().HaveCount(2);
        kq.MatHang[0].Should().Be(new MatHangOCR("Sữa tươi", 2, 15_000m));
    }

    [Theory]
    [InlineData("không phải json")]
    [InlineData("")]
    [InlineData("[1,2]")]
    [InlineData("{\"tenCuaHang\":")]
    public void PhanTich_JsonHong_NemDomainException(string json)
    {
        var act = () => GeminiDichVuOCR.PhanTich(json);

        act.Should().Throw<DomainException>().WithMessage("Không nhận dạng được hóa đơn");
    }

    [Fact]
    public void PhanTich_JsonBocTrongMarkdown_VanDocDuoc()
    {
        var kq = GeminiDichVuOCR.PhanTich("```json\n" + JsonHopLe + "\n```");

        kq.TenCuaHang.Should().Be("Bách Hóa Xanh");
        kq.MatHang.Should().HaveCount(2);
    }

    [Fact]
    public void PhanTich_ThieuTruongTuyChon_KhongLoi()
    {
        var kq = GeminiDichVuOCR.PhanTich("""{"matHang":[{"ten":"Bánh mì","soLuong":1}]}""");

        kq.TenCuaHang.Should().BeNull();
        kq.Ngay.Should().BeNull();
        kq.TongTien.Should().BeNull();
        kq.MatHang.Should().ContainSingle().Which.DonGia.Should().Be(0m);
    }

    [Fact]
    public void PhanTich_NgaySai_ThanhNull()
    {
        GeminiDichVuOCR.PhanTich("""{"ngay":"07/11/2026","matHang":[]}""").Ngay.Should().BeNull();
    }

    [Fact]
    public void PhanTich_BoDongTenRongHoacSoLuongKhongDuong()
    {
        var kq = GeminiDichVuOCR.PhanTich("""
            {"matHang":[{"ten":"  ","soLuong":1,"donGia":5},{"ten":"A","soLuong":0,"donGia":5},
                        {"ten":"B","soLuong":-1,"donGia":5},{"ten":"C","soLuong":3,"donGia":5}]}
            """);

        kq.MatHang.Should().ContainSingle().Which.Ten.Should().Be("C");
    }

    [Theory]
    [InlineData("1e30")]
    [InlineData("-75000")]
    [InlineData("1e17")]
    public void PhanTich_TongTienQuaLonHoacAm_ThanhNullKhongLoi(string tongTien)
    {
        var kq = GeminiDichVuOCR.PhanTich($$"""{"tongTien":{{tongTien}},"matHang":[{"ten":"A","soLuong":1,"donGia":5000}]}""");

        kq.TongTien.Should().BeNull();
        kq.MatHang.Should().ContainSingle();
    }

    [Fact]
    public void PhanTich_DongCoSoQuaLonHoacDonGiaAm_BoDongDo()
    {
        var kq = GeminiDichVuOCR.PhanTich("""
            {"matHang":[{"ten":"A","soLuong":1,"donGia":1e30},{"ten":"B","soLuong":1e30,"donGia":5},
                        {"ten":"C","soLuong":1,"donGia":-5},{"ten":"D","soLuong":1e9,"donGia":1e9},
                        {"ten":"E","soLuong":2,"donGia":5}]}
            """);

        kq.MatHang.Should().ContainSingle().Which.Ten.Should().Be("E");
        BanNhapHoaDonDto.Tu("/u", kq).TongTien.Should().Be(10m);
    }

    [Fact]
    public void BanNhap_TongTienNull_LayTongDong()
    {
        var kq = new KetQuaOCR("Quán A", null, null,
            [new MatHangOCR("X", 2, 10_000m), new MatHangOCR("Y", 1, 5_000m)]);

        var dto = BanNhapHoaDonDto.Tu("/uploads/hoa-don/a.png", kq);

        dto.TongTien.Should().Be(25_000m);
        dto.AnhUrl.Should().Be("/uploads/hoa-don/a.png");
        dto.MatHang.Should().HaveCount(2);
    }

    [Fact]
    public void BanNhap_CoTongTien_GiuNguyen()
    {
        var kq = new KetQuaOCR("Quán A", null, 99_000m, [new MatHangOCR("X", 1, 10_000m)]);

        BanNhapHoaDonDto.Tu("/u", kq).TongTien.Should().Be(99_000m);
    }
}

public class GeminiThieuCauHinhTests
{
    [Fact]
    public async Task NhanDang_ThieuApiKey_NemDomainExceptionKhongGoiMang()
    {
        var ocr = new GeminiDichVuOCR(new HttpClient(), Microsoft.Extensions.Options.Options.Create(new GeminiOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GeminiDichVuOCR>.Instance);

        var act = () => ocr.NhanDangAsync([1], "image/png", CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Không nhận dạng được hóa đơn");
    }
}
