using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/giao-dich")]
public class GiaoDichController(GiaoDichService dich) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TrangKetQua<GiaoDichDto>>> TimKiem(
        [FromQuery] DateOnly? tu, [FromQuery] DateOnly? den, [FromQuery] LoaiGiaoDich? loai,
        [FromQuery] int? viId, [FromQuery] int? danhMucId, [FromQuery] string? q, [FromQuery] int trang = 1,
        CancellationToken ct = default) =>
        await dich.TimKiemAsync(User.MaTaiKhoan(), tu, den, loai, viId, danhMucId, q, trang, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GiaoDichChiTietDto>> LayChiTiet(int id, CancellationToken ct) =>
        await dich.LayChiTietAsync(User.MaTaiKhoan(), id, ct);

    [HttpPost]
    public async Task<IActionResult> Tao(TaoGiaoDichRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created,
            GiaoDichService.ToDto(await dich.TaoAsync(User.MaTaiKhoan(), req, null, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GiaoDichDto>> Sua(int id, SuaGiaoDichRequest req, CancellationToken ct) =>
        GiaoDichService.ToDto(await dich.SuaAsync(User.MaTaiKhoan(), id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        await dich.XoaAsync(User.MaTaiKhoan(), id, ct);
        return NoContent();
    }
}
