using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Infrastructure;

/// <summary>Đọc hóa đơn bằng Gemini (REST generateContent, ảnh gửi inline base64, trả JSON theo schema).</summary>
public class GeminiDichVuOCR(HttpClient http, IOptions<GeminiOptions> cauHinh, ILogger<GeminiDichVuOCR> logger) : IDichVuOCR
{
    public const string ThongBaoLoi = "Không nhận dạng được hóa đơn";

    internal const string LoiHeader = "x-goog-api-key";

    /// <summary>Giá trị tiền lớn nhất chấp nhận từ kết quả quét (bằng giới hạn số tiền của giao dịch).</summary>
    internal const decimal GioiHanSo = 9_999_999_999_999_999m;

    private const string Prompt =
        "Đây là ảnh một hóa đơn mua hàng của Việt Nam. Hãy đọc hóa đơn và chỉ trả về JSON đúng theo schema đã cho, " +
        "không thêm lời giải thích. tenCuaHang là tên cửa hàng; ngay là ngày trên hóa đơn theo định dạng yyyy-MM-dd; " +
        "tongTien là tổng thanh toán; matHang là từng dòng hàng gồm ten, soLuong, donGia (đơn giá một đơn vị). " +
        "Các số viết liền, không có dấu phân cách hàng nghìn (ví dụ 15000, không phải 15.000). " +
        "Nếu không đọc được trường nào thì bỏ trống trường đó.";

    public async Task<KetQuaOCR> NhanDangAsync(byte[] anh, string mimeType, CancellationToken ct)
    {
        var opt = cauHinh.Value;
        if (string.IsNullOrWhiteSpace(opt.ApiKey))
        {
            logger.LogWarning("Chưa cấu hình Gemini:ApiKey nên không thể quét hóa đơn");
            throw new DomainException(ThongBaoLoi);
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(opt.Model)}:generateContent")
            {
                Content = JsonContent.Create(TaoNoiDungYeuCau(anh, mimeType)),
            };
            req.Headers.Add(LoiHeader, opt.ApiKey);

            using var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini trả về HTTP {Status}", (int)res.StatusCode);
                throw new DomainException(ThongBaoLoi);
            }

            var goc = await res.Content.ReadFromJsonAsync<JsonElement>(ct);
            var text = goc.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            return PhanTich(text ?? "");
        }
        catch (DomainException) { throw; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException or ArgumentOutOfRangeException
                                       or OverflowException)
        {
            // Thông điệp lỗi HttpClient không chứa khóa API (khóa nằm trong header, không nằm trong URL).
            logger.LogWarning(ex, "Gọi Gemini thất bại: {LoaiLoi}", ex.GetType().Name);
            throw new DomainException(ThongBaoLoi);
        }
    }

    private static object TaoNoiDungYeuCau(byte[] anh, string mimeType) => new
    {
        contents = new[]
        {
            new
            {
                parts = new object[]
                {
                    new { text = Prompt },
                    new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(anh) } },
                },
            },
        },
        generationConfig = new
        {
            response_mime_type = "application/json",
            response_schema = new
            {
                type = "OBJECT",
                properties = new
                {
                    tenCuaHang = new { type = "STRING" },
                    ngay = new { type = "STRING" },
                    tongTien = new { type = "NUMBER" },
                    matHang = new
                    {
                        type = "ARRAY",
                        items = new
                        {
                            type = "OBJECT",
                            properties = new
                            {
                                ten = new { type = "STRING" },
                                soLuong = new { type = "NUMBER" },
                                donGia = new { type = "NUMBER" },
                            },
                            required = new[] { "ten", "soLuong", "donGia" },
                        },
                    },
                },
            },
        },
    };

    /// <summary>Phân tích chuỗi JSON Gemini trả về; JSON hỏng thì ném <see cref="DomainException"/>.</summary>
    public static KetQuaOCR PhanTich(string jsonTraVe)
    {
        try
        {
            var node = JsonNode.Parse(BoRaoMarkdown(jsonTraVe)) as JsonObject
                       ?? throw new JsonException("Gốc JSON không phải đối tượng");

            DateOnly? ngay = DateOnly.TryParseExact(DocChuoi(node["ngay"]), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var n) ? n : null;

            var matHang = new List<MatHangOCR>();
            if (node["matHang"] is JsonArray dong)
            {
                foreach (var d in dong.OfType<JsonObject>())
                {
                    var ten = DocChuoi(d["ten"])?.Trim();
                    var soLuong = d["soLuong"] is null ? 1m : DocSo(d["soLuong"]); // thiếu số lượng thì coi là 1
                    var donGia = d["donGia"] is null ? 0m : DocSo(d["donGia"]);
                    // Bỏ dòng tên rỗng, số không đọc được/ngoài giới hạn, số lượng không dương, đơn giá âm hoặc thành tiền quá lớn.
                    if (string.IsNullOrEmpty(ten) || soLuong is not { } sl || donGia is not { } dg || sl <= 0 || dg < 0
                        || (dg > 0 && sl > GioiHanSo / dg)) continue;
                    matHang.Add(new MatHangOCR(ten, sl, dg));
                }
            }

            var tongTien = DocSo(node["tongTien"]);
            if (tongTien < 0) tongTien = null; // tổng âm coi như không đọc được
            return new KetQuaOCR(DocChuoi(node["tenCuaHang"])?.Trim(), ngay, tongTien, matHang);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            throw new DomainException(ThongBaoLoi);
        }
    }

    private static string BoRaoMarkdown(string s)
    {
        s = s.Trim();
        if (!s.StartsWith("```")) return s;
        var xuongDong = s.IndexOf('\n');
        s = xuongDong < 0 ? "" : s[(xuongDong + 1)..];
        if (s.EndsWith("```")) s = s[..^3];
        return s.Trim();
    }

    private static string? DocChuoi(JsonNode? n) =>
        n is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    /// <summary>
    /// Đọc số từ JSON (số hoặc chuỗi). Giá trị vượt <see cref="GioiHanSo"/> theo trị tuyệt đối (kể cả ngoài phạm vi decimal,
    /// ví dụ 1e30) coi như không đọc được và trả null, để không ném OverflowException.
    /// </summary>
    private static decimal? DocSo(JsonNode? n)
    {
        if (n is not JsonValue v) return null;
        decimal? kq = null;
        if (v.TryGetValue<decimal>(out var d)) kq = d;
        else if (v.TryGetValue<double>(out var f))
        {
            if (double.IsFinite(f) && Math.Abs(f) <= (double)GioiHanSo) kq = (decimal)f;
        }
        else if (v.TryGetValue<string>(out var s)
                 && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var p)) kq = p;
        return kq is { } x && Math.Abs(x) <= GioiHanSo ? x : null;
    }
}
