using System.ComponentModel.DataAnnotations;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Dtos;

public record DanhMucDto(
    int MaDanhMuc, string TenDanhMuc, LoaiGiaoDich Loai, string? BieuTuong, string? MauSac,
    bool LaMacDinh, bool LaHeThong);

public record TaoDanhMucRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
    [MaxLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
    string TenDanhMuc,
    [EnumDataType(typeof(LoaiGiaoDich), ErrorMessage = "Loại danh mục không hợp lệ")]
    LoaiGiaoDich Loai,
    [MaxLength(50, ErrorMessage = "Biểu tượng tối đa 50 ký tự")] string? BieuTuong = null,
    [MaxLength(20, ErrorMessage = "Màu sắc tối đa 20 ký tự")] string? MauSac = null);

/// <summary>Loại danh mục không sửa được.</summary>
public record SuaDanhMucRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
    [MaxLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
    string TenDanhMuc,
    [MaxLength(50, ErrorMessage = "Biểu tượng tối đa 50 ký tự")] string? BieuTuong = null,
    [MaxLength(20, ErrorMessage = "Màu sắc tối đa 20 ký tự")] string? MauSac = null);
