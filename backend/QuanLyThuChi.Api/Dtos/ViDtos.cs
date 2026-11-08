using System.ComponentModel.DataAnnotations;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Dtos;

public record ViDto(int MaVi, string TenVi, LoaiVi LoaiVi, decimal SoDu);

public record TaoViRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên ví")]
    [MaxLength(100, ErrorMessage = "Tên ví tối đa 100 ký tự")]
    string TenVi,
    [EnumDataType(typeof(LoaiVi), ErrorMessage = "Loại ví không hợp lệ")]
    LoaiVi LoaiVi,
    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Số dư ban đầu không được âm")]
    decimal SoDuBanDau);

/// <summary>Số dư không sửa được qua đây; chỉ thay đổi nhờ giao dịch.</summary>
public record SuaViRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên ví")]
    [MaxLength(100, ErrorMessage = "Tên ví tối đa 100 ký tự")]
    string TenVi,
    [EnumDataType(typeof(LoaiVi), ErrorMessage = "Loại ví không hợp lệ")]
    LoaiVi LoaiVi);
