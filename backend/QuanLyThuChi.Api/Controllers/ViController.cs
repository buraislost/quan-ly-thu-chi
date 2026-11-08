using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/vi")]
public class ViController(ViService dich) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ViDto>>> LietKe(CancellationToken ct) =>
        await dich.LietKeAsync(User.MaTaiKhoan(), ct);

    [HttpPost]
    public async Task<IActionResult> Tao(TaoViRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await dich.TaoAsync(User.MaTaiKhoan(), req, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ViDto>> Sua(int id, SuaViRequest req, CancellationToken ct) =>
        await dich.SuaAsync(User.MaTaiKhoan(), id, req, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        await dich.XoaAsync(User.MaTaiKhoan(), id, ct);
        return NoContent();
    }
}
