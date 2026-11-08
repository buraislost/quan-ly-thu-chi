using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyThuChi.Tests.Fakes;

/// <summary>Controller chỉ có trong project test (nạp qua ApplicationPart) để kiểm tra phản hồi lỗi ModelState.</summary>
[ApiController]
[Route("api/_test")]
public class TestModelStateController : ControllerBase
{
    [HttpPost("dto")]
    public IActionResult Nhan(TestDto dto) => Ok();
}

public class TestDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên")]
    public string? TenGoi { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
    public int SoLuong { get; set; } = 1;
}
