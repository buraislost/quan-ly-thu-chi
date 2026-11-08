namespace QuanLyThuChi.Domain;

public class NguoiDung : TaiKhoan
{
    protected NguoiDung() { }

    public NguoiDung(string hoTen, string email, string matKhauHash, DateTime ngayTao)
        : base(hoTen, email, matKhauHash, ngayTao) { }

    public string DonViTienTe { get; private set; } = "VND";
    public CaiDatNhacNho CaiDat { get; private set; } = new();
}
