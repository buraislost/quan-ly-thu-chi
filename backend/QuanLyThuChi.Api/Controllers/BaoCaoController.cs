using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/bao-cao")]
public class BaoCaoController(BaoCaoService dich) : ControllerBase
{
    private const string LoaiTepExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("tong-quan")]
    public async Task<ActionResult<TongQuanDto>> TongQuan([FromQuery] int? thang, [FromQuery] int? nam, CancellationToken ct) =>
        await dich.TongQuanAsync(User.MaTaiKhoan(), thang, nam, ct);

    [HttpGet("theo-danh-muc")]
    public async Task<ActionResult<List<TheoDanhMucDto>>> TheoDanhMuc(
        [FromQuery] DateOnly? tu, [FromQuery] DateOnly? den, [FromQuery] LoaiGiaoDich loai = LoaiGiaoDich.Chi,
        CancellationToken ct = default)
    {
        var (dau, cuoi) = BatBuocKhoang(tu, den);
        return await dich.TheoDanhMucAsync(User.MaTaiKhoan(), dau, cuoi, loai, ct);
    }

    [HttpGet("theo-thoi-gian")]
    public async Task<ActionResult<List<TheoThangDto>>> TheoThoiGian([FromQuery] int? nam, CancellationToken ct) =>
        await dich.TheoThoiGianAsync(User.MaTaiKhoan(), nam, ct);

    [HttpGet("thang")]
    public async Task<ActionResult<List<BaoCaoThangDto>>> LietKeThang(CancellationToken ct) =>
        await dich.LietKeBaoCaoAsync(User.MaTaiKhoan(), ct);

    [HttpGet("excel")]
    public async Task<IActionResult> Excel([FromQuery] DateOnly? tu, [FromQuery] DateOnly? den, CancellationToken ct)
    {
        var (dau, cuoi) = BatBuocKhoang(tu, den);
        var maTk = User.MaTaiKhoan();
        var giaoDich = await dich.LayGiaoDichDtoAsync(maTk, dau, cuoi, ct);
        var tongQuan = await dich.TongQuanKhoangAsync(maTk, dau, cuoi, ct);
        return File(ExcelXuatBaoCao.Tao(giaoDich, tongQuan), LoaiTepExcel, $"bao-cao-{dau:yyyyMMdd}-{cuoi:yyyyMMdd}.xlsx");
    }

    /// <summary>Báo cáo theo khoảng ngày bắt buộc có cả từ ngày và đến ngày.</summary>
    private static (DateOnly Tu, DateOnly Den) BatBuocKhoang(DateOnly? tu, DateOnly? den) =>
        tu is { } t && den is { } d ? (t, d) : throw new DomainException(BaoCaoService.LoiThieuKhoang);
}
