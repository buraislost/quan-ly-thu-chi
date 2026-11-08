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

    public bool LaCuaNguoiDung(int maTk) => MaTaiKhoan == maTk;

    public bool DangDuocSuDung(bool coGiaoDich, bool coNganSach) => coGiaoDich || coNganSach;
}
