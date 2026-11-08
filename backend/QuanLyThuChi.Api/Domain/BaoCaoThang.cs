namespace QuanLyThuChi.Domain;

public class BaoCaoThang
{
    protected BaoCaoThang() { }

    public BaoCaoThang(int maTaiKhoan, int thang, int nam)
    {
        if (thang is < 1 or > 12) throw new DomainException("Tháng không hợp lệ");
        MaTaiKhoan = maTaiKhoan;
        Thang = thang;
        Nam = nam;
    }

    public int MaBaoCao { get; private set; }
    public int MaTaiKhoan { get; private set; }
    public int Thang { get; private set; }
    public int Nam { get; private set; }
    public decimal TongThu { get; private set; }
    public decimal TongChi { get; private set; }
    public int SoDanhMucVuot { get; private set; }
    public DateTime NgayLap { get; private set; }

    /// <summary>Ghi đè số liệu và ngày lập (cập nhật lặp lại không tạo bản ghi mới, R9).</summary>
    public void LapBaoCao(decimal thu, decimal chi, int soVuot, DateTime luc)
    {
        TongThu = thu;
        TongChi = chi;
        SoDanhMucVuot = soVuot;
        NgayLap = luc;
    }
}
