using Microsoft.EntityFrameworkCore;
using QuanLyThuChi.Domain;

namespace QuanLyThuChi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaiKhoan> TaiKhoan => Set<TaiKhoan>();
    public DbSet<Vi> Vi => Set<Vi>();
    public DbSet<DanhMuc> DanhMuc => Set<DanhMuc>();
    public DbSet<GiaoDich> GiaoDich => Set<GiaoDich>();
    public DbSet<HoaDon> HoaDon => Set<HoaDon>();
    public DbSet<ChiTietHoaDon> ChiTietHoaDon => Set<ChiTietHoaDon>();
    public DbSet<NganSach> NganSach => Set<NganSach>();
    public DbSet<MucTieuTietKiem> MucTieuTietKiem => Set<MucTieuTietKiem>();
    public DbSet<ThongBao> ThongBao => Set<ThongBao>();
    public DbSet<BaoCaoThang> BaoCaoThang => Set<BaoCaoThang>();
    public DbSet<CaiDatNhacNho> CaiDatNhacNho => Set<CaiDatNhacNho>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
