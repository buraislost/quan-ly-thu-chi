using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/tai-khoan")]
public class TaiKhoanController(TaiKhoanService dich) : ControllerBase
{
    [HttpGet("toi")]
    public async Task<ActionResult<HoSoResponse>> Toi(CancellationToken ct) =>
        await dich.LayHoSoAsync(User.MaTaiKhoan(), ct);

    [HttpPut("toi")]
    public async Task<ActionResult<HoSoResponse>> CapNhat(CapNhatHoSoRequest req, CancellationToken ct) =>
        await dich.CapNhatHoSoAsync(User.MaTaiKhoan(), req, ct);

    [HttpPut("mat-khau")]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauRequest req, CancellationToken ct)
    {
        await dich.DoiMatKhauAsync(User.MaTaiKhoan(), req, ct);
        return NoContent();
    }
}
