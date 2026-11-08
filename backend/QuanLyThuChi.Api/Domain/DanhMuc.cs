namespace QuanLyThuChi.Domain;

public class DanhMuc
{
    protected DanhMuc() { }

    public DanhMuc(int? maTaiKhoan, string tenDanhMuc, LoaiGiaoDich loai, string? bieuTuong, string? mauSac,
        bool laMacDinh, bool laHeThong)
    {
        if (string.IsNullOrWhiteSpace(tenDanhMuc)) throw new DomainException("Tên danh mục không được để trống");
        MaTaiKhoan = maTaiKhoan;
        TenDanhMuc = tenDanhMuc.Trim();
        Loai = loai;
        BieuTuong = bieuTuong;
        MauSac = mauSac;
        LaMacDinh = laMacDinh;
        LaHeThong = laHeThong;
    }

    public int MaDanhMuc { get; private set; }
    /// <summary>Null với danh mục mặc định dùng chung.</summary>
    public int? MaTaiKhoan { get; private set; }
    public string TenDanhMuc { get; private set; } = "";
    public LoaiGiaoDich Loai { get; private set; }
    public string? BieuTuong { get; private set; }
    public string? MauSac { get; private set; }
    public bool LaMacDinh { get; private set; }
    public bool LaHeThong { get; private set; }

    /// <summary>Đổi tên, biểu tượng và màu; loại danh mục không đổi được.</summary>
    public void DoiThongTin(string ten, string? bieuTuong, string? mauSac)
    {
        if (string.IsNullOrWhiteSpace(ten)) throw new DomainException("Tên danh mục không được để trống");
        TenDanhMuc = ten.Trim();
        BieuTuong = bieuTuong;
        MauSac = mauSac;
    }

    public bool LaCuaNguoiDung(int maTk) => MaTaiKhoan == maTk;

    public bool DangDuocSuDung(bool coGiaoDich, bool coNganSach) => coGiaoDich || coNganSach;
}
