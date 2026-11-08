using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Data.Configurations;

public class TaiKhoanConfiguration : IEntityTypeConfiguration<TaiKhoan>
{
    public void Configure(EntityTypeBuilder<TaiKhoan> b)
    {
        b.ToTable("TaiKhoan");
        b.HasKey(x => x.MaTaiKhoan);
        b.Property(x => x.HoTen).IsRequired().HasMaxLength(100);
        b.Property(x => x.Email).IsRequired().HasMaxLength(200);
        b.Property(x => x.MatKhauHash).IsRequired().HasMaxLength(200);
        b.Property(x => x.TrangThai).HasConversion<int>();
        b.HasIndex(x => x.Email).IsUnique();

        b.HasDiscriminator<string>("LoaiTaiKhoan")
            .HasValue<NguoiDung>("NguoiDung")
            .HasValue<QuanTriVien>("QuanTriVien");
        b.Property("LoaiTaiKhoan").HasMaxLength(20);
    }
}

public class NguoiDungConfiguration : IEntityTypeConfiguration<NguoiDung>
{
    public void Configure(EntityTypeBuilder<NguoiDung> b)
    {
        b.Property(x => x.DonViTienTe).HasMaxLength(10);
        b.HasOne(x => x.CaiDat).WithOne()
            .HasForeignKey<CaiDatNhacNho>("MaTaiKhoan")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CaiDatNhacNhoConfiguration : IEntityTypeConfiguration<CaiDatNhacNho>
{
    public void Configure(EntityTypeBuilder<CaiDatNhacNho> b)
    {
        b.ToTable("CaiDatNhacNho");
        b.HasKey(x => x.MaCaiDat);
    }
}
