using Microsoft.AspNetCore.Mvc;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(IDongHo dongHo) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { trangThai = "OK", bayGio = dongHo.BayGio });
}
