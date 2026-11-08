using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/muc-tieu")]
public class MucTieuController(MucTieuService dich) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MucTieuDto>>> LietKe(CancellationToken ct) =>
        await dich.LietKeAsync(User.MaTaiKhoan(), ct);

    [HttpPost]
    public async Task<IActionResult> Tao(TaoMucTieuRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await dich.TaoAsync(User.MaTaiKhoan(), req, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MucTieuDto>> Sua(int id, SuaMucTieuRequest req, CancellationToken ct) =>
        await dich.SuaAsync(User.MaTaiKhoan(), id, req, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        await dich.XoaAsync(User.MaTaiKhoan(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/nap-tien")]
    public async Task<ActionResult<MucTieuDto>> NapTien(int id, NapTienRequest req, CancellationToken ct) =>
        await dich.NapTienAsync(User.MaTaiKhoan(), id, req, ct);
}
