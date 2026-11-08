using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuChi.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoaiTaiKhoan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DonViTienTe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                });

            migrationBuilder.CreateTable(
                name: "BaoCaoThang",
                columns: table => new
                {
                    MaBaoCao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    TongThu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongChi = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoDanhMucVuot = table.Column<int>(type: "int", nullable: false),
                    NgayLap = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoThang", x => x.MaBaoCao);
                    table.ForeignKey(
                        name: "FK_BaoCaoThang_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaiDatNhacNho",
                columns: table => new
                {
                    MaCaiDat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhacHangNgay = table.Column<bool>(type: "bit", nullable: false),
                    GioNhac = table.Column<TimeOnly>(type: "time", nullable: false),
                    NhacNganSach = table.Column<bool>(type: "bit", nullable: false),
                    NhacMucTieu = table.Column<bool>(type: "bit", nullable: false),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaiDatNhacNho", x => x.MaCaiDat);
                    table.ForeignKey(
                        name: "FK_CaiDatNhacNho_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhMuc",
                columns: table => new
                {
                    MaDanhMuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    BieuTuong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MauSac = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LaMacDinh = table.Column<bool>(type: "bit", nullable: false),
                    LaHeThong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMuc", x => x.MaDanhMuc);
                    table.ForeignKey(
                        name: "FK_DanhMuc_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MucTieuTietKiem",
                columns: table => new
                {
                    MaMucTieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    TenMucTieu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoTienMucTieu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoTienHienTai = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HanChot = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayTao = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucTieuTietKiem", x => x.MaMucTieu);
                    table.ForeignKey(
                        name: "FK_MucTieuTietKiem_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBao_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vi",
                columns: table => new
                {
                    MaVi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    TenVi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LoaiVi = table.Column<int>(type: "int", nullable: false),
                    SoDu = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vi", x => x.MaVi);
                    table.ForeignKey(
                        name: "FK_Vi_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NganSach",
                columns: table => new
                {
                    MaNganSach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    MaDanhMuc = table.Column<int>(type: "int", nullable: false),
                    HanMuc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    NguongCanhBao = table.Column<int>(type: "int", nullable: false),
                    DaCanhBaoNguong = table.Column<bool>(type: "bit", nullable: false),
                    DaCanhBaoVuot = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NganSach", x => x.MaNganSach);
                    table.ForeignKey(
                        name: "FK_NganSach_DanhMuc_MaDanhMuc",
                        column: x => x.MaDanhMuc,
                        principalTable: "DanhMuc",
                        principalColumn: "MaDanhMuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NganSach_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GiaoDich",
                columns: table => new
                {
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    MaVi = table.Column<int>(type: "int", nullable: false),
                    MaDanhMuc = table.Column<int>(type: "int", nullable: false),
                    MaMucTieu = table.Column<int>(type: "int", nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDich", x => x.MaGiaoDich);
                    table.ForeignKey(
                        name: "FK_GiaoDich_DanhMuc_MaDanhMuc",
                        column: x => x.MaDanhMuc,
                        principalTable: "DanhMuc",
                        principalColumn: "MaDanhMuc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDich_MucTieuTietKiem_MaMucTieu",
                        column: x => x.MaMucTieu,
                        principalTable: "MucTieuTietKiem",
                        principalColumn: "MaMucTieu",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDich_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiaoDich_Vi_MaVi",
                        column: x => x.MaVi,
                        principalTable: "Vi",
                        principalColumn: "MaVi",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                columns: table => new
                {
                    MaHoaDon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false),
                    TenCuaHang = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayHoaDon = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnhHoaDon = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDon", x => x.MaHoaDon);
                    table.ForeignKey(
                        name: "FK_HoaDon_GiaoDich_MaGiaoDich",
                        column: x => x.MaGiaoDich,
                        principalTable: "GiaoDich",
                        principalColumn: "MaGiaoDich",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDon",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoaDon = table.Column<int>(type: "int", nullable: false),
                    TenMatHang = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDon", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDon_HoaDon_MaHoaDon",
                        column: x => x.MaHoaDon,
                        principalTable: "HoaDon",
                        principalColumn: "MaHoaDon",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoThang_MaTaiKhoan_Thang_Nam",
                table: "BaoCaoThang",
                columns: new[] { "MaTaiKhoan", "Thang", "Nam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaiDatNhacNho_MaTaiKhoan",
                table: "CaiDatNhacNho",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDon_MaHoaDon",
                table: "ChiTietHoaDon",
                column: "MaHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_DanhMuc_MaTaiKhoan",
                table: "DanhMuc",
                column: "MaTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDich_MaDanhMuc",
                table: "GiaoDich",
                column: "MaDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDich_MaMucTieu",
                table: "GiaoDich",
                column: "MaMucTieu");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDich_MaTaiKhoan_Ngay",
                table: "GiaoDich",
                columns: new[] { "MaTaiKhoan", "Ngay" });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDich_MaVi",
                table: "GiaoDich",
                column: "MaVi");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDon_MaGiaoDich",
                table: "HoaDon",
                column: "MaGiaoDich",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MucTieuTietKiem_MaTaiKhoan",
                table: "MucTieuTietKiem",
                column: "MaTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_NganSach_MaDanhMuc",
                table: "NganSach",
                column: "MaDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_NganSach_MaTaiKhoan_MaDanhMuc_Thang_Nam",
                table: "NganSach",
                columns: new[] { "MaTaiKhoan", "MaDanhMuc", "Thang", "Nam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_Email",
                table: "TaiKhoan",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaTaiKhoan_NgayTao",
                table: "ThongBao",
                columns: new[] { "MaTaiKhoan", "NgayTao" });

            migrationBuilder.CreateIndex(
                name: "IX_Vi_MaTaiKhoan_TenVi",
                table: "Vi",
                columns: new[] { "MaTaiKhoan", "TenVi" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaoCaoThang");

            migrationBuilder.DropTable(
                name: "CaiDatNhacNho");

            migrationBuilder.DropTable(
                name: "ChiTietHoaDon");

            migrationBuilder.DropTable(
                name: "NganSach");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "HoaDon");

            migrationBuilder.DropTable(
                name: "GiaoDich");

            migrationBuilder.DropTable(
                name: "DanhMuc");

            migrationBuilder.DropTable(
                name: "MucTieuTietKiem");

            migrationBuilder.DropTable(
                name: "Vi");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}
