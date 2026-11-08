using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize(Roles = "QuanTriVien")]
[Route("api/admin")]
public class AdminController(QuanTriService dich) : ControllerBase
{
    [HttpGet("nguoi-dung")]
    public async Task<ActionResult<List<NguoiDungAdminDto>>> LietKeNguoiDung([FromQuery] string? q, CancellationToken ct) =>
        await dich.LietKeNguoiDungAsync(q, ct);

    [HttpPut("nguoi-dung/{id:int}/khoa")]
    public async Task<IActionResult> Khoa(int id, KhoaTaiKhoanRequest req, CancellationToken ct)
    {
        await dich.KhoaAsync(User.MaTaiKhoan(), id, req.LyDo, ct);
        return NoContent();
    }

    [HttpPut("nguoi-dung/{id:int}/mo-khoa")]
    public async Task<IActionResult> MoKhoa(int id, CancellationToken ct)
    {
        await dich.MoKhoaAsync(User.MaTaiKhoan(), id, ct);
        return NoContent();
    }

    [HttpGet("danh-muc-mac-dinh")]
    public async Task<ActionResult<List<DanhMucDto>>> LietKeMacDinh(CancellationToken ct) =>
        await dich.LietKeMacDinhAsync(ct);

    [HttpPost("danh-muc-mac-dinh")]
    public async Task<IActionResult> TaoMacDinh(TaoDanhMucRequest req, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await dich.TaoMacDinhAsync(req, ct));

    [HttpPut("danh-muc-mac-dinh/{id:int}")]
    public async Task<ActionResult<DanhMucDto>> SuaMacDinh(int id, SuaDanhMucRequest req, CancellationToken ct) =>
        await dich.SuaMacDinhAsync(id, req, ct);

    [HttpDelete("danh-muc-mac-dinh/{id:int}")]
    public async Task<IActionResult> XoaMacDinh(int id, CancellationToken ct)
    {
        await dich.XoaMacDinhAsync(id, ct);
        return NoContent();
    }

    [HttpGet("thong-ke")]
    public async Task<ActionResult<ThongKeDto>> ThongKe(CancellationToken ct) => await dich.ThongKeAsync(ct);

    [HttpPost("chay-tong-ket")]
    public async Task<ActionResult<KetQuaTongKetDto>> ChayTongKet([FromQuery] int? thang, [FromQuery] int? nam, CancellationToken ct) =>
        await dich.ChayTongKetAsync(thang, nam, ct);
}
