using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Infrastructure;

namespace QuanLyThuChi.Data;

/// <summary>
/// Nạp dữ liệu demo cho môi trường Development (idempotent theo email demo): 3 ví, khoảng 90 giao dịch trong 3 tháng gần nhất,
/// hai ngân sách (một gần ngưỡng, một vượt), hai mục tiêu tiết kiệm (một đúng tiến độ, một chậm).
/// Mọi số liệu đi qua constructor/phương thức của Domain để số dư ví luôn khớp giao dịch; hạt giống ngẫu nhiên cố định nên kết quả lặp lại được.
/// </summary>
public static class DuLieuDemo
{
    public const string EmailDemo = "demo@demo.vn";
    public const string MatKhauDemo = "Demo@123";
    public const string HoTenDemo = "Nguyễn Văn Demo";

    public const decimal SoDuDauTienMat = 2_000_000m;
    public const decimal SoDuDauNganHang = 15_000_000m;
    public const decimal SoDuDauViDienTu = 500_000m;
    public const decimal Luong = 12_000_000m;

    private const int HatGiong = 2026;
    private const int SoChiNgauNhien = 80;
    private const int NgayNhanLuong = 5;

    private record MauChi(string DanhMuc, int TrongSo, (string GhiChu, int Min, int Max)[] Dong);

    // Số tiền tính theo nghìn đồng.
    private static readonly MauChi[] CacMauChi =
    [
        new("Ăn uống", 35, [("Ăn sáng bánh mì", 15, 30), ("Cơm trưa quán cơm tấm", 35, 60), ("Cà phê với bạn", 25, 55),
            ("Trà sữa", 35, 65), ("Ăn tối cùng nhóm bạn", 80, 200), ("Đi chợ mua thực phẩm", 100, 300)]),
        new("Đi lại", 18, [("Đổ xăng", 50, 120), ("Vé xe buýt", 5, 15), ("Grab đi học", 30, 80), ("Gửi xe", 5, 10)]),
        new("Mua sắm", 10, [("Mua quần áo", 150, 500), ("Mua đồ dùng học tập", 50, 200), ("Mua sắm online Shopee", 100, 400)]),
        new("Hóa đơn", 10, [("Tiền điện", 300, 600), ("Tiền internet", 200, 250), ("Tiền điện thoại", 100, 100), ("Tiền nước", 80, 150)]),
        new("Giải trí", 12, [("Xem phim cuối tuần", 90, 180), ("Nạp game", 50, 200), ("Đi chơi cuối tuần", 150, 400)]),
        new("Sức khỏe", 4, [("Mua thuốc", 50, 150), ("Khám sức khỏe", 200, 400)]),
        new("Giáo dục", 8, [("Mua sách giáo trình", 80, 250), ("Photo tài liệu", 10, 40), ("Học phí khóa tiếng Anh", 300, 800)]),
        new("Khác", 3, [("Quà sinh nhật bạn", 100, 300)]),
    ];

    private record Khoan(DateTime Ngay, Vi? ViCoDinh, DanhMuc DanhMuc, decimal SoTien, LoaiGiaoDich Loai, string GhiChu, int? MaMucTieu);

    public static async Task NapAsync(AppDbContext db, IBamMatKhau bam, IDongHo dongHo)
    {
        if (await db.TaiKhoan.AnyAsync(t => t.Email == EmailDemo)) return;

        var rng = new Random(HatGiong);
        var homNay = dongHo.HomNay;
        var bayGio = dongHo.BayGio;
        var ngayDau = new DateOnly(homNay.Year, homNay.Month, 1).AddMonths(-2); // 3 tháng: hai tháng trước và tháng này
        var dauKy = ngayDau.ToDateTime(TimeOnly.MinValue);

        var danhMuc = await db.DanhMuc.Where(d => d.MaTaiKhoan == null).ToListAsync();
        DanhMuc Tim(string ten, LoaiGiaoDich loai) =>
            danhMuc.FirstOrDefault(d => d.TenDanhMuc == ten && d.Loai == loai)
            ?? throw new InvalidOperationException($"Thiếu danh mục khởi tạo '{ten}'; cần chạy DuLieuKhoiTao trước");

        await using var tx = await db.Database.BeginTransactionAsync();

        var nguoiDung = new NguoiDung(HoTenDemo, EmailDemo, bam.Bam(MatKhauDemo), dauKy);
        db.TaiKhoan.Add(nguoiDung);
        await db.SaveChangesAsync();
        var maTk = nguoiDung.MaTaiKhoan;

        var tienMat = new Vi(maTk, "Tiền mặt", LoaiVi.TienMat, SoDuDauTienMat, dauKy);
        var nganHang = new Vi(maTk, "Vietcombank", LoaiVi.NganHang, SoDuDauNganHang, dauKy);
        var viDienTu = new Vi(maTk, "MoMo", LoaiVi.ViDienTu, SoDuDauViDienTu, dauKy);
        Vi[] cacVi = [tienMat, nganHang, viDienTu];
        db.Vi.AddRange(cacVi);

        var ngayTaoMucTieu = homNay.AddMonths(-2);
        var laptop = new MucTieuTietKiem(maTk, "Mua laptop", 20_000_000m, homNay.AddMonths(4), ngayTaoMucTieu);
        var daLat = new MucTieuTietKiem(maTk, "Du lịch Đà Lạt", 8_000_000m, homNay.AddMonths(2), ngayTaoMucTieu);
        db.MucTieuTietKiem.AddRange(laptop, daLat);
        await db.SaveChangesAsync();

        // 1. Lập danh sách khoản thu chi theo ngày.
        var khoans = new List<Khoan>();
        var luongDm = Tim("Lương", LoaiGiaoDich.Thu);
        for (var ngay = ngayDau; ngay <= homNay; ngay = ngay.AddMonths(1))
        {
            var ngayLuong = new DateOnly(ngay.Year, ngay.Month, NgayNhanLuong);
            if (ngayLuong > homNay) continue;
            khoans.Add(new Khoan(GioHopLe(ngayLuong.ToDateTime(new TimeOnly(8, 30)), bayGio), nganHang, luongDm, Luong, LoaiGiaoDich.Thu,
                $"Lương tháng {ngayLuong.Month:00}/{ngayLuong.Year}", null));
        }

        var tongTrongSo = CacMauChi.Sum(m => m.TrongSo);
        var soNgay = homNay.DayNumber - ngayDau.DayNumber + 1;
        MauChi ChonMau()
        {
            var x = rng.Next(tongTrongSo);
            foreach (var m in CacMauChi) { if (x < m.TrongSo) return m; x -= m.TrongSo; }
            return CacMauChi[0];
        }
        Khoan TaoChi(MauChi mau, DateOnly ngay)
        {
            var dong = mau.Dong[rng.Next(mau.Dong.Length)];
            var soTien = rng.Next(dong.Min, dong.Max + 1) * 1000m;
            var gio = new TimeOnly(rng.Next(7, 22), rng.Next(0, 60));
            return new Khoan(GioHopLe(ngay.ToDateTime(gio), bayGio), null, Tim(mau.DanhMuc, LoaiGiaoDich.Chi), soTien,
                LoaiGiaoDich.Chi, dong.GhiChu, null);
        }

        for (var i = 0; i < SoChiNgauNhien; i++)
            khoans.Add(TaoChi(ChonMau(), ngayDau.AddDays(rng.Next(soNgay))));

        // Bảo đảm tháng hiện tại có chi ăn uống và giải trí để dựng ngân sách, kể cả khi hôm nay là mùng 1.
        var dauThang = new DateOnly(homNay.Year, homNay.Month, 1);
        var soNgayThang = homNay.DayNumber - dauThang.DayNumber + 1;
        DateOnly NgayTrongThang() => dauThang.AddDays(rng.Next(soNgayThang));
        var anUong = CacMauChi.Single(m => m.DanhMuc == "Ăn uống");
        var giaiTri = CacMauChi.Single(m => m.DanhMuc == "Giải trí");
        for (var i = 0; i < 3; i++) khoans.Add(TaoChi(anUong, NgayTrongThang()));
        for (var i = 0; i < 2; i++) khoans.Add(TaoChi(giaiTri, NgayTrongThang()));

        // Nạp tiền mục tiêu qua danh mục hệ thống "Tiết kiệm"; Mua laptop đủ để đúng tiến độ, Đà Lạt nạp ít nên chậm.
        var tietKiem = Tim("Tiết kiệm", LoaiGiaoDich.Chi);
        (MucTieuTietKiem MucTieu, int SoNgayTruoc, decimal SoTien)[] napTien =
        [
            (laptop, 55, 3_000_000m), (laptop, 30, 3_000_000m), (laptop, 6, 2_000_000m),
            (daLat, 20, 1_000_000m),
        ];
        foreach (var (mucTieu, soNgayTruoc, soTien) in napTien)
        {
            var ngay = homNay.AddDays(-soNgayTruoc);
            if (ngay < ngayDau) ngay = ngayDau;
            khoans.Add(new Khoan(GioHopLe(ngay.ToDateTime(new TimeOnly(19, 0)), bayGio), nganHang, tietKiem, soTien,
                LoaiGiaoDich.Chi, $"Nạp mục tiêu: {mucTieu.TenMucTieu}", mucTieu.MaMucTieu));
        }

        // 2. Ghi nhận theo thứ tự thời gian để ví không bao giờ âm; khoản chi không ví nào đủ tiền thì bỏ qua.
        var daGhi = new List<GiaoDich>();
        foreach (var k in khoans.OrderBy(k => k.Ngay))
        {
            var vi = k.ViCoDinh ?? ChonVi(cacVi, k, rng);
            if (vi is null || (k.Loai == LoaiGiaoDich.Chi && !vi.KiemTraSoDu(k.SoTien))) continue;
            var gd = new GiaoDich(maTk, vi, k.DanhMuc, k.SoTien, k.Loai, k.Ngay, k.GhiChu, k.MaMucTieu);
            gd.GhiNhan();
            db.GiaoDich.Add(gd);
            daGhi.Add(gd);
            if (k.MaMucTieu is { } ma) (ma == laptop.MaMucTieu ? laptop : daLat).NapTien(k.SoTien);
        }

        // 3. Ngân sách tháng hiện tại, hạn mức suy ra từ số đã chi thật để tỷ lệ đúng như kịch bản.
        var chiThangNay = daGhi.Where(g => g.Loai == LoaiGiaoDich.Chi && g.Ngay >= dauThang.ToDateTime(TimeOnly.MinValue)).ToList();
        var dmAnUong = Tim("Ăn uống", LoaiGiaoDich.Chi);
        var dmGiaiTri = Tim("Giải trí", LoaiGiaoDich.Chi);

        var nsAnUong = LapNganSach(maTk, dmAnUong, homNay, chiThangNay, tiLe: 0.85m, bayGio, db);
        var nsGiaiTri = LapNganSach(maTk, dmGiaiTri, homNay, chiThangNay, tiLe: 1.25m, bayGio, db);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Giờ giao dịch không được vượt quá hiện tại của đồng hồ nghiệp vụ.</summary>
    private static DateTime GioHopLe(DateTime gio, DateTime bayGio) => gio > bayGio ? bayGio : gio;

    /// <summary>Chọn ngẫu nhiên một ví đủ tiền (khoản chi) hoặc ví bất kỳ (khoản thu); null nếu không ví nào đủ.</summary>
    private static Vi? ChonVi(Vi[] cacVi, Khoan k, Random rng)
    {
        var ungVien = k.Loai == LoaiGiaoDich.Thu ? cacVi : cacVi.Where(v => v.KiemTraSoDu(k.SoTien)).ToArray();
        return ungVien.Length == 0 ? null : ungVien[rng.Next(ungVien.Length)];
    }

    /// <summary>
    /// Lập ngân sách có hạn mức = số đã chi / tỉ lệ (làm tròn lên nghìn đồng), kiểm tra cờ qua <see cref="NganSach.KiemTraVuot"/>
    /// và thêm thông báo cảnh báo tương ứng với cùng nội dung như <c>NganSachService</c>.
    /// </summary>
    private static NganSach LapNganSach(int maTk, DanhMuc dm, DateOnly homNay, List<GiaoDich> chiThangNay, decimal tiLe,
        DateTime bayGio, AppDbContext db)
    {
        var soDaChi = chiThangNay.Where(g => g.MaDanhMuc == dm.MaDanhMuc).Sum(g => g.SoTien);
        if (soDaChi <= 0) throw new InvalidOperationException($"Chưa có chi tiêu '{dm.TenDanhMuc}' trong tháng để dựng ngân sách demo");
        var hanMuc = Math.Ceiling(soDaChi / tiLe / 1000m) * 1000m;
        var ns = new NganSach(maTk, dm.MaDanhMuc, hanMuc, homNay.Month, homNay.Year);
        var ketQua = ns.KiemTraVuot(soDaChi);
        var ky = string.Create(CultureInfo.InvariantCulture, $"{homNay.Month:00}/{homNay.Year}");
        string? noiDung = ketQua switch
        {
            KetQuaCanhBao.Vuot => $"Ngân sách {dm.TenDanhMuc} tháng {ky} đã vượt hạn mức",
            KetQuaCanhBao.DatNguong => $"Ngân sách {dm.TenDanhMuc} tháng {ky} đã dùng {(int)Math.Round(soDaChi / hanMuc * 100m, MidpointRounding.AwayFromZero)}%",
            _ => null,
        };
        db.NganSach.Add(ns);
        if (noiDung is not null) db.ThongBao.Add(new ThongBao(maTk, LoaiThongBao.CanhBaoNganSach, noiDung, bayGio));
        return ns;
    }
}
