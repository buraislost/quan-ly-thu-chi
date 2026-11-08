using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Dtos;
using QuanLyThuChi.Services;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Authorize]
[Route("api/hoa-don")]
public class HoaDonController(HoaDonService dich) : ControllerBase
{
    // Giới hạn 6 MB (5 MB ảnh + dư cho phần đệm multipart) để ảnh quá cỡ vẫn tới được kiểm tra "Ảnh vượt quá 5 MB".
    [HttpPost("quet")]
    [RequestSizeLimit(6 * 1024 * 1024 + 256 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<BanNhapHoaDonDto>> Quet(IFormFile anh, CancellationToken ct) =>
        await dich.QuetAsync(anh, ct);
}
