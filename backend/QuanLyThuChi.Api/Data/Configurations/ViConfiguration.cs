using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Data.Configurations;

public class ViConfiguration : IEntityTypeConfiguration<Vi>
{
    public void Configure(EntityTypeBuilder<Vi> b)
    {
        b.ToTable("Vi");
        b.HasKey(x => x.MaVi);
        b.Property(x => x.TenVi).IsRequired().HasMaxLength(100);
        b.Property(x => x.LoaiVi).HasConversion<int>();
        b.Property(x => x.SoDu).HasColumnType("decimal(18,2)");
        b.HasIndex(x => new { x.MaTaiKhoan, x.TenVi }).IsUnique();
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Cascade);
    }
}
