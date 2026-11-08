using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Infrastructure;

/// <summary>Ánh xạ exception sang phản hồi JSON <c>{ message }</c>; lỗi không lường trước chỉ ghi log, không lộ chi tiết.</summary>
public class XuLyLoiMiddleware(RequestDelegate tiep, ILogger<XuLyLoiMiddleware> logger)
{
    public const string ThongBaoLoiHeThong = "Đã xảy ra lỗi, vui lòng thử lại";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await tiep(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (trangThai, thongBao) = ex switch
            {
                DomainException => (StatusCodes.Status400BadRequest, ex.Message),
                KhongTimThayException => (StatusCodes.Status404NotFound, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, ThongBaoLoiHeThong),
            };

            if (trangThai == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Lỗi không xử lý được tại {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = trangThai;
            await context.Response.WriteAsJsonAsync(new { message = thongBao });
        }
    }
}
