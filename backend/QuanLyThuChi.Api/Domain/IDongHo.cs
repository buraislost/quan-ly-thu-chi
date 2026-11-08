namespace QuanLyThuChi.Domain;

/// <summary>
/// Đồng hồ nghiệp vụ. Mọi logic "hôm nay/bây giờ" phải dùng giờ Việt Nam (UTC+7) qua interface này.
/// </summary>
public interface IDongHo
{
    /// <summary>Giờ Việt Nam hiện tại, <see cref="DateTimeKind.Unspecified"/>.</summary>
    DateTime BayGio { get; }

    /// <summary>Ngày hiện tại theo giờ Việt Nam.</summary>
    DateOnly HomNay { get; }

    /// <summary>Đổi giờ Utc/Local sang giờ Việt Nam; giá trị Unspecified coi như đã là giờ Việt Nam. Kết quả luôn Unspecified.</summary>
    DateTime SangGioVietNam(DateTime gio);
}
