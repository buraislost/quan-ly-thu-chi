namespace QuanLyThuChi.Domain;

public abstract class TaiKhoan
{
    protected TaiKhoan() { }

    protected TaiKhoan(string hoTen, string email, string matKhauHash, DateTime ngayTao)
    {
        if (string.IsNullOrWhiteSpace(hoTen)) throw new DomainException("Vui lòng nhập họ tên");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Vui lòng nhập email");
        HoTen = hoTen.Trim();
        Email = email.Trim();
        MatKhauHash = matKhauHash;
        NgayTao = ngayTao;
        TrangThai = TrangThaiTaiKhoan.HoatDong;
    }

    public int MaTaiKhoan { get; private set; }
    public string HoTen { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string MatKhauHash { get; private set; } = "";
    public TrangThaiTaiKhoan TrangThai { get; private set; } = TrangThaiTaiKhoan.HoatDong;
    public DateTime NgayTao { get; private set; }

    /// <summary>Kiểm tra mật khẩu. Tìm tài khoản theo email là việc của service.</summary>
    public bool DangNhap(string matKhau, IBamMatKhau bam)
    {
        if (TrangThai == TrangThaiTaiKhoan.BiKhoa) throw new DomainException("Tài khoản đã bị khóa");
        return bam.KiemTra(matKhau, MatKhauHash);
    }

    public bool DoiMatKhau(string cu, string moi, IBamMatKhau bam)
    {
        if (!bam.KiemTra(cu, MatKhauHash)) return false;
        MatKhauHash = bam.Bam(moi);
        return true;
    }

    public void CapNhatThongTin(string hoTen)
    {
        if (string.IsNullOrWhiteSpace(hoTen)) throw new DomainException("Vui lòng nhập họ tên");
        HoTen = hoTen.Trim();
    }

    internal void DatTrangThai(TrangThaiTaiKhoan trangThai) => TrangThai = trangThai;
}
