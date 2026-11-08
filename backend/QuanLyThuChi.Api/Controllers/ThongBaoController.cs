using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Infrastructure;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/thong-bao")]
public class ThongBaoController(ThongBaoService dich) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ThongBaoDto>>> LietKe([FromQuery] bool chuaDoc, CancellationToken ct) =>
        await dich.LietKeAsync(User.MaTaiKhoan(), chuaDoc, ct);

    [HttpGet("so-chua-doc")]
    public async Task<ActionResult<SoChuaDocDto>> SoChuaDoc(CancellationToken ct) =>
        new SoChuaDocDto(await dich.DemChuaDocAsync(User.MaTaiKhoan(), ct));

    [HttpPut("{id:int}/da-doc")]
    public async Task<IActionResult> DanhDauDaDoc(int id, CancellationToken ct)
    {
        await dich.DanhDauDaDocAsync(User.MaTaiKhoan(), id, ct);
        return NoContent();
    }

    [HttpPut("da-doc-tat-ca")]
    public async Task<IActionResult> DanhDauTatCaDaDoc(CancellationToken ct)
    {
        await dich.DanhDauTatCaDaDocAsync(User.MaTaiKhoan(), ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Xoa(int id, CancellationToken ct)
    {
        await dich.XoaAsync(User.MaTaiKhoan(), id, ct);
        return NoContent();
    }

    [HttpGet("cai-dat")]
    public async Task<ActionResult<CaiDatDto>> LayCaiDat(CancellationToken ct) =>
        await dich.LayCaiDatAsync(User.MaTaiKhoan(), ct);

    [HttpPut("cai-dat")]
    public async Task<ActionResult<CaiDatDto>> LuuCaiDat(CaiDatDto req, CancellationToken ct) =>
        await dich.LuuCaiDatAsync(User.MaTaiKhoan(), req, ct);

    [HttpPost("cai-dat/khoi-phuc")]
    public async Task<ActionResult<CaiDatDto>> KhoiPhucMacDinh(CancellationToken ct) =>
        await dich.KhoiPhucMacDinhAsync(User.MaTaiKhoan(), ct);
}
