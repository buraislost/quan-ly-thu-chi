namespace QuanLyThuChi.Domain;

public enum KetQuaCanhBao { KhongCo, DatNguong, Vuot }

public class NganSach
{
    protected NganSach() { }

    public NganSach(int maTaiKhoan, int maDanhMuc, decimal hanMuc, int thang, int nam, int nguongCanhBao = 80)
    {
        if (hanMuc <= 0) throw new DomainException("Hạn mức phải lớn hơn 0");
        if (thang is < 1 or > 12) throw new DomainException("Tháng không hợp lệ");
        if (nguongCanhBao is < 1 or > 100) throw new DomainException("Ngưỡng cảnh báo phải từ 1 đến 100");
        MaTaiKhoan = maTaiKhoan;
        MaDanhMuc = maDanhMuc;
        HanMuc = hanMuc;
        Thang = thang;
        Nam = nam;
        NguongCanhBao = nguongCanhBao;
    }

    public int MaNganSach { get; private set; }
    public int MaTaiKhoan { get; private set; }
    public int MaDanhMuc { get; private set; }
    public decimal HanMuc { get; private set; }
    public int Thang { get; private set; }
    public int Nam { get; private set; }
    public int NguongCanhBao { get; private set; } = 80;
    public bool DaCanhBaoNguong { get; private set; }
    public bool DaCanhBaoVuot { get; private set; }

    /// <summary>Tổng chi của danh mục này; người gọi đã lọc theo tháng, ở đây chỉ lọc loại Chi và danh mục.</summary>
    public decimal TinhSoDaChi(IEnumerable<GiaoDich> chiTrongThang) =>
        chiTrongThang.Where(g => g.Loai == LoaiGiaoDich.Chi && g.MaDanhMuc == MaDanhMuc).Sum(g => g.SoTien);

    /// <summary>Tiến độ chi theo phần trăm hạn mức.</summary>
    public double TinhTienDo(decimal soDaChi) => (double)(soDaChi / HanMuc * 100m);

    /// <summary>Bật cờ và mỗi mức chỉ trả về một lần (R6). Vượt thì bật luôn cờ ngưỡng, chỉ trả Vuot.</summary>
    public KetQuaCanhBao KiemTraVuot(decimal soDaChi)
    {
        var tienDo = soDaChi / HanMuc * 100m;
        if (tienDo > 100m && !DaCanhBaoVuot)
        {
            DaCanhBaoVuot = true;
            DaCanhBaoNguong = true;
            return KetQuaCanhBao.Vuot;
        }
        if (tienDo >= NguongCanhBao && !DaCanhBaoNguong)
        {
            DaCanhBaoNguong = true;
            return KetQuaCanhBao.DatNguong;
        }
        return KetQuaCanhBao.KhongCo;
    }

    /// <summary>Đổi hạn mức và tính lại hai cờ theo tiến độ mới; không phát cảnh báo.</summary>
    public void CapNhatHanMuc(decimal hanMuc, decimal soDaChi)
    {
        if (hanMuc <= 0) throw new DomainException("Hạn mức phải lớn hơn 0");
        HanMuc = hanMuc;
        var tienDo = soDaChi / HanMuc * 100m;
        DaCanhBaoNguong = tienDo >= NguongCanhBao;
        DaCanhBaoVuot = tienDo > 100m;
    }
}
