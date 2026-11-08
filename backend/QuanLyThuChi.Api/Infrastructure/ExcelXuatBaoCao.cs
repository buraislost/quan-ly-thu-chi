using ClosedXML.Excel;
using QuanLyThuChi.Domain;
using QuanLyThuChi.Dtos;

namespace QuanLyThuChi.Infrastructure;

/// <summary>Tạo tệp Excel báo cáo gồm hai sheet "Giao dịch" và "Tổng hợp" (UC 3.12).</summary>
public static class ExcelXuatBaoCao
{
    private const string DinhDangTien = "#,##0";

    public static byte[] Tao(IEnumerable<GiaoDichDto> giaoDich, TongQuanDto tongQuan)
    {
        using var wb = new XLWorkbook();
        VietSheetGiaoDich(wb.Worksheets.Add("Giao dịch"), giaoDich);
        VietSheetTongHop(wb.Worksheets.Add("Tổng hợp"), tongQuan);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void VietSheetGiaoDich(IXLWorksheet ws, IEnumerable<GiaoDichDto> giaoDich)
    {
        string[] tieuDe = ["Ngày", "Loại", "Danh mục", "Ví", "Số tiền", "Ghi chú"];
        for (var i = 0; i < tieuDe.Length; i++) ws.Cell(1, i + 1).Value = tieuDe[i];
        ws.Row(1).Style.Font.Bold = true;

        var dong = 2;
        foreach (var g in giaoDich)
        {
            ws.Cell(dong, 1).Value = g.NgayGiaoDich;
            ws.Cell(dong, 1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(dong, 2).Value = g.Loai == LoaiGiaoDich.Thu ? "Thu" : "Chi";
            ws.Cell(dong, 3).Value = g.TenDanhMuc;
            ws.Cell(dong, 4).Value = g.TenVi;
            ws.Cell(dong, 5).Value = g.SoTien;
            ws.Cell(dong, 5).Style.NumberFormat.Format = DinhDangTien;
            ws.Cell(dong, 6).Value = g.GhiChu ?? "";
            dong++;
        }
        ws.Columns().AdjustToContents();
    }

    private static void VietSheetTongHop(IXLWorksheet ws, TongQuanDto tq)
    {
        (string Nhan, decimal Tri)[] tong = [("Tổng thu", tq.TongThu), ("Tổng chi", tq.TongChi), ("Chênh lệch", tq.ChenhLech)];
        for (var i = 0; i < tong.Length; i++)
        {
            ws.Cell(i + 1, 1).Value = tong[i].Nhan;
            ws.Cell(i + 1, 1).Style.Font.Bold = true;
            ws.Cell(i + 1, 2).Value = tong[i].Tri;
            ws.Cell(i + 1, 2).Style.NumberFormat.Format = DinhDangTien;
        }

        ws.Cell(5, 1).Value = "Danh mục";
        ws.Cell(5, 2).Value = "Số tiền chi";
        ws.Cell(5, 3).Value = "Tỷ lệ (%)";
        ws.Range(5, 1, 5, 3).Style.Font.Bold = true;
        var dong = 6;
        foreach (var d in tq.ChiTheoDanhMuc)
        {
            ws.Cell(dong, 1).Value = d.TenDanhMuc;
            ws.Cell(dong, 2).Value = d.SoTien;
            ws.Cell(dong, 2).Style.NumberFormat.Format = DinhDangTien;
            ws.Cell(dong, 3).Value = d.TyLe;
            ws.Cell(dong, 3).Style.NumberFormat.Format = "0.0";
            dong++;
        }
        ws.Columns().AdjustToContents();
    }
}
