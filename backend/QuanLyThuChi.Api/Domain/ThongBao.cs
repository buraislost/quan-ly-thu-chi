namespace QuanLyThuChi.Domain;

public class ThongBao
{
    protected ThongBao() { }

    public ThongBao(int maTaiKhoan, LoaiThongBao loai, string noiDung, DateTime ngayTao)
    {
        if (string.IsNullOrWhiteSpace(noiDung)) throw new DomainException("Nội dung thông báo không được để trống");
        MaTaiKhoan = maTaiKhoan;
        Loai = loai;
        NoiDung = noiDung;
        NgayTao = ngayTao;
    }

    public int MaThongBao { get; private set; }
    public int MaTaiKhoan { get; private set; }
    public LoaiThongBao Loai { get; private set; }
    public string NoiDung { get; private set; } = "";
    public DateTime NgayTao { get; private set; }
    public bool DaDoc { get; private set; }

    public void DanhDauDaDoc() => DaDoc = true;
}
