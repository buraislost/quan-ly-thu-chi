using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(TaiKhoanService dich) : ControllerBase
{
    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy(DangKyRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await dich.DangKyAsync(req, ct));

    [HttpPost("dang-nhap")]
    public async Task<ActionResult<DangNhapResponse>> DangNhap(DangNhapRequest req, CancellationToken ct) =>
        await dich.DangNhapAsync(req, ct);
}
