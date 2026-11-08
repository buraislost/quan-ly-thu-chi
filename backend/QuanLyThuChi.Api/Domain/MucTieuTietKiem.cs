namespace QuanLyThuChi.Domain;

public class MucTieuTietKiem
{
    protected MucTieuTietKiem() { }

    public MucTieuTietKiem(int maTaiKhoan, string tenMucTieu, decimal soTienMucTieu, DateOnly hanChot, DateOnly ngayTao)
    {
        if (string.IsNullOrWhiteSpace(tenMucTieu)) throw new DomainException("Tên mục tiêu không được để trống");
        if (soTienMucTieu <= 0) throw new DomainException("Số tiền mục tiêu phải lớn hơn 0");
        if (hanChot <= ngayTao) throw new DomainException("Hạn chót phải sau hôm nay");
        MaTaiKhoan = maTaiKhoan;
        TenMucTieu = tenMucTieu.Trim();
        SoTienMucTieu = soTienMucTieu;
        HanChot = hanChot;
        NgayTao = ngayTao;
    }

    public int MaMucTieu { get; private set; }
    public int MaTaiKhoan { get; private set; }
    public string TenMucTieu { get; private set; } = "";
    public decimal SoTienMucTieu { get; private set; }
    public decimal SoTienHienTai { get; private set; }
    public DateOnly HanChot { get; private set; }
    public DateOnly NgayTao { get; private set; }

    /// <summary>Sửa thông tin; mức mới thấp hơn số đã nạp thì mục tiêu coi là hoàn thành (không phát thông báo). Hạn chót so với hôm nay do tầng dịch vụ kiểm tra.</summary>
    public void DoiThongTin(string ten, decimal soTienMucTieu, DateOnly hanChot)
    {
        if (string.IsNullOrWhiteSpace(ten)) throw new DomainException("Tên mục tiêu không được để trống");
        if (soTienMucTieu <= 0) throw new DomainException("Số tiền mục tiêu phải lớn hơn 0");
        if (hanChot <= NgayTao) throw new DomainException("Hạn chót phải sau hôm nay");
        TenMucTieu = ten.Trim();
        SoTienMucTieu = soTienMucTieu;
        HanChot = hanChot;
    }

    /// <summary>Nạp tiền; trả true chỉ ở lần đầu đạt đủ mục tiêu. Cho phép nạp vượt.</summary>
    public bool NapTien(decimal soTien)
    {
        if (soTien <= 0) throw new DomainException("Số tiền phải lớn hơn 0");
        var daXongTruoc = DaHoanThanh();
        SoTienHienTai += soTien;
        return !daXongTruoc && DaHoanThanh();
    }

    public double TinhTienDo() => (double)(SoTienHienTai / SoTienMucTieu * 100m);

    public bool DaHoanThanh() => SoTienHienTai >= SoTienMucTieu;

    /// <summary>Chậm khi số đã nạp thấp hơn mức kỳ vọng tuyến tính theo thời gian; quá hạn mà chưa xong cũng là chậm.</summary>
    public bool LaChamTienDo(DateOnly homNay)
    {
        if (DaHoanThanh()) return false;
        if (homNay >= HanChot) return true;
        var tongNgay = HanChot.DayNumber - NgayTao.DayNumber;
        var daQua = Math.Max(0, homNay.DayNumber - NgayTao.DayNumber);
        var kyVong = SoTienMucTieu * daQua / tongNgay;
        return SoTienHienTai < kyVong;
    }

    /// <summary>Số tiền cần nạp mỗi tháng = còn lại / số tháng còn lại (làm tròn lên, tối thiểu 1), làm tròn đến đồng.</summary>
    public decimal SoTienCanMoiThang(DateOnly homNay)
    {
        var conLai = Math.Max(0m, SoTienMucTieu - SoTienHienTai);
        var soThang = (HanChot.Year - homNay.Year) * 12 + HanChot.Month - homNay.Month;
        if (soThang > 0 && homNay.AddMonths(soThang) < HanChot) soThang++;
        else if (soThang == 0 && homNay < HanChot) soThang = 1;
        soThang = Math.Max(1, soThang);
        return Math.Round(conLai / soThang, 0, MidpointRounding.AwayFromZero);
    }
}
