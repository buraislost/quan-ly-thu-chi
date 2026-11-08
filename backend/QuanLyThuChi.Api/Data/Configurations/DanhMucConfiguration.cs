using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Data.Configurations;

public class DanhMucConfiguration : IEntityTypeConfiguration<DanhMuc>
{
    public void Configure(EntityTypeBuilder<DanhMuc> b)
    {
        b.ToTable("DanhMuc");
        b.HasKey(x => x.MaDanhMuc);
        b.Property(x => x.TenDanhMuc).IsRequired().HasMaxLength(100);
        b.Property(x => x.Loai).HasConversion<int>();
        b.Property(x => x.BieuTuong).HasMaxLength(50);
        b.Property(x => x.MauSac).HasMaxLength(20);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaTaiKhoan).IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
