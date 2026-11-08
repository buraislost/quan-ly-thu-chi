using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Data;

/// <summary>Nạp dữ liệu khởi tạo (idempotent): danh mục mặc định, danh mục hệ thống và tài khoản admin demo.</summary>
public static class DuLieuKhoiTao
{
    public const string EmailAdmin = "admin@demo.vn";
    public const string MatKhauAdmin = "Demo@123";

    private static readonly (string Ten, LoaiGiaoDich Loai, string BieuTuong, string MauSac, bool HeThong)[] DanhMucMacDinh =
    [
        ("Ăn uống", LoaiGiaoDich.Chi, "restaurant", "#F97316", false),
        ("Đi lại", LoaiGiaoDich.Chi, "directions_bus", "#3B82F6", false),
        ("Mua sắm", LoaiGiaoDich.Chi, "shopping_cart", "#EC4899", false),
        ("Hóa đơn", LoaiGiaoDich.Chi, "receipt_long", "#EAB308", false),
        ("Giải trí", LoaiGiaoDich.Chi, "movie", "#8B5CF6", false),
        ("Sức khỏe", LoaiGiaoDich.Chi, "favorite", "#EF4444", false),
        ("Giáo dục", LoaiGiaoDich.Chi, "school", "#14B8A6", false),
        ("Khác", LoaiGiaoDich.Chi, "more_horiz", "#6B7280", false),
        ("Lương", LoaiGiaoDich.Thu, "payments", "#22C55E", false),
        ("Thưởng", LoaiGiaoDich.Thu, "card_giftcard", "#10B981", false),
        ("Khác", LoaiGiaoDich.Thu, "more_horiz", "#6B7280", false),
        ("Tiết kiệm", LoaiGiaoDich.Chi, "savings", "#0EA5E9", true),
    ];

    /// <param name="dongHo">Đồng hồ nghiệp vụ; mặc định dùng giờ Việt Nam thật.</param>
    public static async Task NapAsync(AppDbContext db, IBamMatKhau bam, IDongHo? dongHo = null)
    {
        dongHo ??= new DongHoVietNam(TimeProvider.System);
        await NapDanhMucAsync(db);
        await NapAdminAsync(db, bam, dongHo);
    }

    private static async Task NapDanhMucAsync(AppDbContext db)
    {
        var daCo = await db.DanhMuc.Where(d => d.MaTaiKhoan == null)
            .Select(d => new { d.TenDanhMuc, d.Loai }).ToListAsync();

        foreach (var (ten, loai, bieuTuong, mauSac, heThong) in DanhMucMacDinh)
        {
            if (daCo.Any(d => d.TenDanhMuc == ten && d.Loai == loai)) continue;
            db.DanhMuc.Add(new DanhMuc(null, ten, loai, bieuTuong, mauSac, laMacDinh: true, laHeThong: heThong));
        }
        await db.SaveChangesAsync();
    }

    private static async Task NapAdminAsync(AppDbContext db, IBamMatKhau bam, IDongHo dongHo)
    {
        if (await db.TaiKhoan.AnyAsync(t => t.Email == EmailAdmin)) return;
        db.TaiKhoan.Add(new QuanTriVien("Quản trị viên", EmailAdmin, bam.Bam(MatKhauAdmin), dongHo.BayGio));
        await db.SaveChangesAsync();
    }
}
