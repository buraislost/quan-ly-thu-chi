namespace QuanLyThuChi.Domain;

public class QuanTriVien : TaiKhoan
{
    protected QuanTriVien() { }

    public QuanTriVien(string hoTen, string email, string matKhauHash, DateTime ngayTao)
        : base(hoTen, email, matKhauHash, ngayTao) { }

    /// <summary>Khóa tài khoản. Việc gửi thông báo lý do thuộc về service.</summary>
    public void KhoaTaiKhoan(TaiKhoan tk, string lyDo)
    {
        if (ReferenceEquals(tk, this) || (MaTaiKhoan != 0 && tk.MaTaiKhoan == MaTaiKhoan))
            throw new DomainException("Không thể tự khóa tài khoản của mình");
        if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Vui lòng nhập lý do khóa");
        tk.DatTrangThai(TrangThaiTaiKhoan.BiKhoa);
    }

    public void MoKhoaTaiKhoan(TaiKhoan tk) => tk.DatTrangThai(TrangThaiTaiKhoan.HoatDong);
}
