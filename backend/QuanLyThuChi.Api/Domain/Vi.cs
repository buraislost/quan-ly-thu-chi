namespace QuanLyThuChi.Domain;

public class Vi
{
    protected Vi() { }

    public Vi(int maTaiKhoan, string tenVi, LoaiVi loaiVi, decimal soDuBanDau, DateTime ngayTao)
    {
        if (string.IsNullOrWhiteSpace(tenVi)) throw new DomainException("Tên ví không được để trống");
        if (soDuBanDau < 0) throw new DomainException("Số dư ban đầu không được âm");
        MaTaiKhoan = maTaiKhoan;
        TenVi = tenVi.Trim();
        LoaiVi = loaiVi;
        SoDu = soDuBanDau;
        NgayTao = ngayTao;
    }

    public int MaVi { get; private set; }
    public int MaTaiKhoan { get; private set; }
    public string TenVi { get; private set; } = "";
    public LoaiVi LoaiVi { get; private set; }
    public decimal SoDu { get; private set; }
    public DateTime NgayTao { get; private set; }

    public bool KiemTraSoDu(decimal soTien) => SoDu >= soTien;

    public void CongTien(decimal soTien)
    {
        KiemTraSoTien(soTien);
        SoDu += soTien;
    }

    public void TruTien(decimal soTien)
    {
        KiemTraSoTien(soTien);
        if (!KiemTraSoDu(soTien)) throw new DomainException("Số dư ví không đủ");
        SoDu -= soTien;
    }

    private static void KiemTraSoTien(decimal soTien)
    {
        if (soTien <= 0) throw new DomainException("Số tiền phải lớn hơn 0");
    }
}
