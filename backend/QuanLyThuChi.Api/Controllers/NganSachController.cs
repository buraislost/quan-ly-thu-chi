using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/ngan-sach")]
public class NganSachController(NganSachService dich) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NganSachDto>>> LietKe([FromQuery] int? thang, [FromQuery] int? nam, CancellationToken ct) =>
        await dich.LietKeAsync(User.MaTaiKhoan(), thang, nam, ct);

    [HttpPost]
    public async Task<IActionResult> Lap(TaoNganSachRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await dich.LapAsync(User.MaTaiKhoan(), req, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<NganSachDto>> Sua(int id, SuaNganSachRequest req, CancellationToken ct) =>
        await dich.SuaAsync(User.MaTaiKhoan(), id, req, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        await dich.XoaAsync(User.MaTaiKhoan(), id, ct);
        return NoContent();
    }
}
