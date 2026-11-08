using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Data.Configurations;

public class GiaoDichConfiguration : IEntityTypeConfiguration<GiaoDich>
{
    public void Configure(EntityTypeBuilder<GiaoDich> b)
    {
        b.ToTable("GiaoDich");
        b.HasKey(x => x.MaGiaoDich);
        b.Property(x => x.SoTien).HasColumnType("decimal(18,2)");
        b.Property(x => x.Loai).HasConversion<int>();
        b.Property(x => x.GhiChu).HasMaxLength(500);
        b.HasIndex(x => new { x.MaTaiKhoan, x.Ngay });

        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Vi).WithMany().HasForeignKey(x => x.MaVi).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DanhMuc).WithMany().HasForeignKey(x => x.MaDanhMuc).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MucTieuTietKiem>().WithMany().HasForeignKey(x => x.MaMucTieu).IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.HoaDon).WithOne().HasForeignKey<HoaDon>(h => h.MaGiaoDich)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class HoaDonConfiguration : IEntityTypeConfiguration<HoaDon>
{
    public void Configure(EntityTypeBuilder<HoaDon> b)
    {
        b.ToTable("HoaDon");
        b.HasKey(x => x.MaHoaDon);
        b.Property(x => x.TenCuaHang).HasMaxLength(200);
        b.Property(x => x.TongTien).HasColumnType("decimal(18,2)");
        b.Property(x => x.AnhHoaDon).HasMaxLength(500);
        b.HasMany(x => x.ChiTiet).WithOne().HasForeignKey(c => c.MaHoaDon).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChiTietHoaDonConfiguration : IEntityTypeConfiguration<ChiTietHoaDon>
{
    public void Configure(EntityTypeBuilder<ChiTietHoaDon> b)
    {
        b.ToTable("ChiTietHoaDon");
        b.HasKey(x => x.MaChiTiet);
        b.Property(x => x.TenMatHang).IsRequired().HasMaxLength(200);
        b.Property(x => x.SoLuong).HasColumnType("decimal(18,2)");
        b.Property(x => x.DonGia).HasColumnType("decimal(18,2)");
    }
}
