namespace QuanLyThuChi.Domain;

public class GiaoDich
{
    protected GiaoDich() { }

    public GiaoDich(int maTaiKhoan, Vi vi, DanhMuc danhMuc, decimal soTien, LoaiGiaoDich loai, DateTime ngay,
        string? ghiChu, int? maMucTieu = null)
    {
        KiemTraHopLe(danhMuc, soTien, loai);
        MaTaiKhoan = maTaiKhoan;
        Vi = vi;
        MaVi = vi.MaVi;
        DanhMuc = danhMuc;
        MaDanhMuc = danhMuc.MaDanhMuc;
        SoTien = soTien;
        Loai = loai;
        Ngay = ngay;
        GhiChu = ghiChu;
        MaMucTieu = maMucTieu;
    }

    public int MaGiaoDich { get; private set; }
    public int MaTaiKhoan { get; private set; }
    public int MaVi { get; private set; }
    public Vi Vi { get; private set; } = null!;
    public int MaDanhMuc { get; private set; }
    public DanhMuc DanhMuc { get; private set; } = null!;
    public int? MaMucTieu { get; private set; }
    public decimal SoTien { get; private set; }
    public LoaiGiaoDich Loai { get; private set; }
    public DateTime Ngay { get; private set; }
    public string? GhiChu { get; private set; }
    public HoaDon? HoaDon { get; private set; }

    /// <summary>Áp giao dịch vào ví: thu cộng, chi trừ.</summary>
    public void GhiNhan() => ApDung(Vi, SoTien, Loai);

    /// <summary>Hoàn tác giao dịch khỏi ví. Hoàn tác khoản thu có thể ném "Số dư ví không đủ".</summary>
    public void Huy()
    {
        if (Loai == LoaiGiaoDich.Thu) Vi.TruTien(SoTien);
        else Vi.CongTien(SoTien);
    }

    /// <summary>Hoàn tác giá trị cũ rồi áp giá trị mới. Nếu thất bại, giữ nguyên trạng thái ban đầu của giao dịch và ví.</summary>
    public void CapNhat(Vi vi, DanhMuc dm, decimal soTien, LoaiGiaoDich loai, DateTime ngay, string? ghiChu)
    {
        KiemTraHopLe(dm, soTien, loai);

        var viCu = Vi; var tienCu = SoTien; var loaiCu = Loai;
        Huy();
        try
        {
            ApDung(vi, soTien, loai);
        }
        catch
        {
            ApDung(viCu, tienCu, loaiCu); // khôi phục ví cũ
            throw;
        }

        Vi = vi; MaVi = vi.MaVi;
        DanhMuc = dm; MaDanhMuc = dm.MaDanhMuc;
        SoTien = soTien; Loai = loai; Ngay = ngay; GhiChu = ghiChu;
    }

    /// <summary>Chuyển giao dịch sang danh mục khác cùng loại; ví và số dư không đổi.</summary>
    public void ChuyenDanhMuc(DanhMuc moi)
    {
        if (moi.Loai != Loai) throw new DomainException("Danh mục không đúng loại giao dịch");
        DanhMuc = moi;
        MaDanhMuc = moi.MaDanhMuc;
    }

    public void GanHoaDon(HoaDon hoaDon) => HoaDon = hoaDon;

    private static void ApDung(Vi vi, decimal soTien, LoaiGiaoDich loai)
    {
        if (loai == LoaiGiaoDich.Thu) vi.CongTien(soTien);
        else vi.TruTien(soTien);
    }

    private static void KiemTraHopLe(DanhMuc danhMuc, decimal soTien, LoaiGiaoDich loai)
    {
        if (soTien <= 0) throw new DomainException("Số tiền phải lớn hơn 0");
        if (danhMuc.Loai != loai) throw new DomainException("Danh mục không đúng loại giao dịch");
    }
}
