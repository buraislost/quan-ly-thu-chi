namespace QuanLyThuChi.Domain;

public class HoaDon
{
    protected HoaDon() { }

    public HoaDon(string? tenCuaHang, DateTime ngayHoaDon, string? anhHoaDon)
    {
        TenCuaHang = tenCuaHang;
        NgayHoaDon = ngayHoaDon;
        AnhHoaDon = anhHoaDon;
    }

    public int MaHoaDon { get; private set; }
    public int MaGiaoDich { get; private set; }
    public string? TenCuaHang { get; private set; }
    public DateTime NgayHoaDon { get; private set; }
    public decimal TongTien { get; private set; }
    public string? AnhHoaDon { get; private set; }
    public List<ChiTietHoaDon> ChiTiet { get; private set; } = [];

    public void ThemChiTiet(ChiTietHoaDon chiTiet)
    {
        ChiTiet.Add(chiTiet);
        TongTien = TinhTongTien();
    }

    public decimal TinhTongTien() => ChiTiet.Sum(c => c.ThanhTien());
}

public class ChiTietHoaDon
{
    protected ChiTietHoaDon() { }

    public ChiTietHoaDon(string tenMatHang, decimal soLuong, decimal donGia)
    {
        if (soLuong <= 0) throw new DomainException("Số lượng phải lớn hơn 0");
        if (donGia < 0) throw new DomainException("Đơn giá không được âm");
        TenMatHang = tenMatHang;
        SoLuong = soLuong;
        DonGia = donGia;
    }

    public int MaChiTiet { get; private set; }
    public int MaHoaDon { get; private set; }
    public string TenMatHang { get; private set; } = "";
    public decimal SoLuong { get; private set; }
    public decimal DonGia { get; private set; }

    public decimal ThanhTien() => SoLuong * DonGia;
}
