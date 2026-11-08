using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Data.Configurations;

public class NganSachConfiguration : IEntityTypeConfiguration<NganSach>
{
    public void Configure(EntityTypeBuilder<NganSach> b)
    {
        b.ToTable("NganSach");
        b.HasKey(x => x.MaNganSach);
        b.Property(x => x.HanMuc).HasColumnType("decimal(18,2)");
        b.HasIndex(x => new { x.MaTaiKhoan, x.MaDanhMuc, x.Thang, x.Nam }).IsUnique();
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<DanhMuc>().WithMany().HasForeignKey(x => x.MaDanhMuc).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MucTieuTietKiemConfiguration : IEntityTypeConfiguration<MucTieuTietKiem>
{
    public void Configure(EntityTypeBuilder<MucTieuTietKiem> b)
    {
        b.ToTable("MucTieuTietKiem");
        b.HasKey(x => x.MaMucTieu);
        b.Property(x => x.TenMucTieu).IsRequired().HasMaxLength(200);
        b.Property(x => x.SoTienMucTieu).HasColumnType("decimal(18,2)");
        b.Property(x => x.SoTienHienTai).HasColumnType("decimal(18,2)");
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ThongBaoConfiguration : IEntityTypeConfiguration<ThongBao>
{
    public void Configure(EntityTypeBuilder<ThongBao> b)
    {
        b.ToTable("ThongBao");
        b.HasKey(x => x.MaThongBao);
        b.Property(x => x.Loai).HasConversion<int>();
        b.Property(x => x.NoiDung).IsRequired().HasMaxLength(1000);
        b.HasIndex(x => new { x.MaTaiKhoan, x.NgayTao });
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BaoCaoThangConfiguration : IEntityTypeConfiguration<BaoCaoThang>
{
    public void Configure(EntityTypeBuilder<BaoCaoThang> b)
    {
        b.ToTable("BaoCaoThang");
        b.HasKey(x => x.MaBaoCao);
        b.Property(x => x.TongThu).HasColumnType("decimal(18,2)");
        b.Property(x => x.TongChi).HasColumnType("decimal(18,2)");
        b.HasIndex(x => new { x.MaTaiKhoan, x.Thang, x.Nam }).IsUnique();
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaTaiKhoan).OnDelete(DeleteBehavior.Cascade);
    }
}
