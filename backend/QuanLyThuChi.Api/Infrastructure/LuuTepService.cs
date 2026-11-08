using Microsoft.Extensions.Options;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Infrastructure;

public class LuuTepOptions
{
    public const string TenMuc = "LuuTep";

    /// <summary>Thư mục gốc chứa tệp tải lên; tương đối thì tính từ ContentRootPath.</summary>
    public string ThuMuc { get; set; } = "uploads";
}

public record AnhDaDoc(byte[] DuLieu, string Mime, string Duoi);

public class LuuTepService(IWebHostEnvironment moiTruong, IOptions<LuuTepOptions> cauHinh)
{
    public const int DungLuongToiDa = 5 * 1024 * 1024;
    public const string ThuMucCon = "hoa-don";
    public const string TienToUrl = "/uploads";

    public static string ThuMucGoc(string contentRoot, LuuTepOptions opt) => Path.GetFullPath(Path.Combine(contentRoot, opt.ThuMuc));

    /// <summary>Kiểm tra dung lượng, ContentType và chữ ký tệp (PNG/JPEG) rồi đọc nội dung.</summary>
    public async Task<AnhDaDoc> DocAnhAsync(IFormFile tep, CancellationToken ct)
    {
        if (tep.Length > DungLuongToiDa) throw new DomainException("Ảnh vượt quá 5 MB");
        var loaiKhaiBao = tep.ContentType?.ToLowerInvariant();
        if (loaiKhaiBao is not ("image/jpeg" or "image/png")) throw new DomainException("Chỉ nhận ảnh JPG hoặc PNG");

        using var ms = new MemoryStream((int)tep.Length);
        await using (var s = tep.OpenReadStream()) await s.CopyToAsync(ms, ct);
        var b = ms.ToArray();

        var thuc = PhatHienLoai(b);
        if (thuc is null || thuc.Value.Mime != loaiKhaiBao) throw new DomainException("Chỉ nhận ảnh JPG hoặc PNG");
        return new AnhDaDoc(b, thuc.Value.Mime, thuc.Value.Duoi);
    }

    private static (string Mime, string Duoi)? PhatHienLoai(byte[] b)
    {
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ("image/png", "png");
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ("image/jpeg", "jpg");
        return null;
    }

    /// <summary>Ghi ảnh đã kiểm tra, trả URL dạng <c>/uploads/hoa-don/{guid}.{ext}</c>.</summary>
    public async Task<string> GhiAsync(AnhDaDoc anh, CancellationToken ct)
    {
        var thuMuc = Path.Combine(ThuMucGoc(moiTruong.ContentRootPath, cauHinh.Value), ThuMucCon);
        Directory.CreateDirectory(thuMuc);
        var ten = $"{Guid.NewGuid():N}.{anh.Duoi}";
        await File.WriteAllBytesAsync(Path.Combine(thuMuc, ten), anh.DuLieu, ct);
        return $"{TienToUrl}/{ThuMucCon}/{ten}";
    }

    public async Task<string> LuuAnhAsync(IFormFile tep, CancellationToken ct = default) =>
        await GhiAsync(await DocAnhAsync(tep, ct), ct);
}
