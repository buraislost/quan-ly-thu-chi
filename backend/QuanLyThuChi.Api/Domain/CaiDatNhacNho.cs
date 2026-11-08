namespace QuanLyThuChi.Domain;

public class CaiDatNhacNho
{
    public static readonly TimeOnly GioNhacMacDinh = new(20, 0);

    public int MaCaiDat { get; private set; }
    public bool NhacHangNgay { get; set; } = true;
    public TimeOnly GioNhac { get; set; } = GioNhacMacDinh;
    public bool NhacNganSach { get; set; } = true;
    public bool NhacMucTieu { get; set; } = true;

    public void KhoiPhucMacDinh()
    {
        NhacHangNgay = true;
        GioNhac = GioNhacMacDinh;
        NhacNganSach = true;
        NhacMucTieu = true;
    }
}
