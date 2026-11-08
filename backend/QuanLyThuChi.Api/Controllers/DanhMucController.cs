using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/danh-muc")]
public class DanhMucController(DanhMucService dich) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<DanhMucDto>>> LietKe([FromQuery] LoaiGiaoDich? loai, CancellationToken ct) =>
        await dich.LietKeAsync(User.MaTaiKhoan(), loai, ct);

    [HttpPost]
    public async Task<IActionResult> Tao(TaoDanhMucRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await dich.TaoAsync(User.MaTaiKhoan(), req, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DanhMucDto>> Sua(int id, SuaDanhMucRequest req, CancellationToken ct) =>
        await dich.SuaAsync(User.MaTaiKhoan(), id, req, ct);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Xoa(int id, [FromQuery] int? chuyenSang, CancellationToken ct)
    {
        await dich.XoaAsync(User.MaTaiKhoan(), id, chuyenSang, ct);
        return NoContent();
    }
}
