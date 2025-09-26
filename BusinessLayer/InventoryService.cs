using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS;
using SelectPdf;
using ClosedXML.Excel;
using System.Drawing;

namespace FLEXIERP.BusinessLayer
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepo inventoryRepo;
        public InventoryService(IInventoryRepo _inventoryRepo)
        {
            this.inventoryRepo = _inventoryRepo;
        }

        public async Task<int> AddCategory(Product_Category product_Category)
        {
            return await inventoryRepo.AddCategory(product_Category);
        }
        public async Task<IEnumerable<ProductCategory_DTO>> GetCategories()
        {
            return await inventoryRepo.GetCategories();
        }

        #region Save Product
        public Task<string> AddProduct(ProductModel product)
        {
            return inventoryRepo.AddProduct(product);
        }
        public async Task<IEnumerable<Product_DTO>> GetProducts(PaginationFilter filter)
        {
            return await inventoryRepo.GetProducts(filter);
        }

        public async Task<byte[]> GetProductReportPdf(PaginationFilter filter)
        {
            // Dummy data
            IEnumerable<Product_DTO> products = await inventoryRepo.GetProducts(filter);

            // Build HTML
            var html = @"
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; font-size: 10pt; margin: 5px; color: #333; }
        .company-header { text-align:center; margin-bottom: 15px; }
        .company-header h1 { font-size: 22pt; color: #1f4e79; margin-bottom: 2px; }
        .company-header h3 { font-size: 11pt; color: #555; margin: 2px 0; }
        .report-title { 
            background-color: #00bfff; 
            color: white; 
            padding: 8px; 
            font-weight: bold; 
            font-size: 14pt; 
            margin-top: 10px; 
            border-radius: 5px;
            display: inline-block;
        }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th { background-color: #00bfff; color: white; border: 1px solid #555; padding: 6px; text-align: center; }
        td { border: 1px solid #ccc; padding: 5px; text-align: center; }
        tbody tr:nth-child(even) { background-color: #f2f7fb; } /* zebra stripes */
        tbody tr:hover { background-color: #d6eefc; } /* hover effect */
    </style>
</head>
<body>
    <div class='company-header'>
        <h1>FLEXIERP Pvt. Ltd.</h1>
        <h3>123 Business Street, City, Country</h3>
        <h3>Phone: +91-1234567890 | Email: info@flexierp.com</h3>
        <div class='report-title'>Product Table Report</div>
    </div>

    <table>
        <thead>
            <tr>
                <th>SrNo</th>
                <th>ProductCode</th>
                <th>BarCode</th>
                <th>ProductName</th>
                <th>CategoryName</th>
                <th>ProductType</th>
                <th>PackedDate</th>
                <th>PackedWeight</th>
                <th>PackedHeight</th>
                <th>PackedDepth</th>
                <th>PackedWidth</th>
                <th>IsPerishable</th>
                <th>CreatedDate</th>
                <th>PurchasePrice</th>
                <th>SellingPrice</th>
                <th>TaxRate</th>
                <th>Discount</th>
                <th>FullName</th>
                <th>TotalRecords</th>
              
            </tr>
        </thead>
        <tbody>";

            int srno = 1;
            foreach (var p in products)
            {
                string isprisnable = (p.IsPerishable == true ? "YES" : "NO");
                html += $@"
            <tr>
                <td>{srno}</td>
                <td>{p.ProductCode}</td>
                <td>{p.BarCode}</td>
                <td>{p.ProductName}</td>
                <td>{p.CategoryName}</td>
                <td>{p.ProductType}</td>
                <td>{p.PackedDate}</td>
                <td>{p.PackedWeight}</td>
                <td>{p.PackedHeight}</td>
                <td>{p.PackedDepth}</td>
                <td>{p.PackedWidth}</td>
                <td>{isprisnable}</td>
                <td>{p.CreatedDate}</td>
                <td>{p.PurchasePrice}</td>
                <td>{p.SellingPrice}</td>
                <td>{p.TaxRate}</td>
                <td>{p.Discount}</td>
                <td>{p.FullName}</td>
                <td>{p.TotalRecords}</td>

            </tr>";
                srno++;
            }

            html += @"
        </tbody>
    </table>
</body>
</html>";

            // Convert HTML to PDF
            HtmlToPdf converter = new HtmlToPdf();
            converter.Options.MarginTop = 10;
            converter.Options.MarginBottom = 10;
            converter.Options.MarginLeft = 10;
            converter.Options.MarginRight = 10;
            PdfDocument doc = converter.ConvertHtmlString(html);

            using var stream = new MemoryStream();
            doc.Save(stream);
            doc.Close();

            return stream.ToArray();
        }

        public async Task<byte[]> GetProductReportExcel(PaginationFilter filter)
        {
            // Get product data
            IEnumerable<Product_DTO> products = await inventoryRepo.GetProducts(filter);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Product Report");

            int currentRow = 1;

            // --- Company Header ---
            worksheet.Cell(currentRow, 1).Value = "FLEXIERP Pvt. Ltd.";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 22;
            worksheet.Range(currentRow, 1, currentRow, 19).Merge();
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "123 Business Street, City, Country";
            worksheet.Range(currentRow, 1, currentRow, 19).Merge();
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Phone: +91-1234567890 | Email: info@flexierp.com";
            worksheet.Range(currentRow, 1, currentRow, 19).Merge();
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow += 2;

            worksheet.Cell(currentRow, 1).Value = "Product Table Report";
            worksheet.Range(currentRow, 1, currentRow, 19).Merge();
            worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.DeepSkyBlue;
            worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
            currentRow++;

            // --- Table Header ---
            string[] headers = new string[]
            {
        "SrNo","ProductCode","BarCode","ProductName","CategoryName","ProductType",
        "PackedDate","PackedWeight","PackedHeight","PackedDepth","PackedWidth",
        "IsPerishable","CreatedDate","PurchasePrice","SellingPrice","TaxRate",
        "Discount","FullName","TotalRecords"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = headers[i];
                worksheet.Cell(currentRow, i + 1).Style.Fill.BackgroundColor = XLColor.DeepSkyBlue;
                worksheet.Cell(currentRow, i + 1).Style.Font.FontColor = XLColor.White;
                worksheet.Cell(currentRow, i + 1).Style.Font.Bold = true;
                worksheet.Cell(currentRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, i + 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Cell(currentRow, i + 1).Style.Border.OutsideBorderColor = XLColor.Black;
            }

            currentRow++;

            // --- Table Data ---
            int srno = 1;
            foreach (var p in products)
            {
                string isprisnable = "";
                if (p.IsPerishable != null)
                {
                    isprisnable = (bool)p.IsPerishable ? "YES" : "NO";
                }

                worksheet.Cell(currentRow, 1).Value = srno;
                worksheet.Cell(currentRow, 2).Value = p.ProductCode;
                worksheet.Cell(currentRow, 3).Value = p.BarCode;
                worksheet.Cell(currentRow, 4).Value = p.ProductName;
                worksheet.Cell(currentRow, 5).Value = p.CategoryName;
                worksheet.Cell(currentRow, 6).Value = p.ProductType;
                worksheet.Cell(currentRow, 7).Value = p.PackedDate;
                worksheet.Cell(currentRow, 8).Value = p.PackedWeight;
                worksheet.Cell(currentRow, 9).Value = p.PackedHeight;
                worksheet.Cell(currentRow, 10).Value = p.PackedDepth;
                worksheet.Cell(currentRow, 11).Value = p.PackedWidth;
                worksheet.Cell(currentRow, 12).Value = isprisnable;
                worksheet.Cell(currentRow, 13).Value = p.CreatedDate;
                worksheet.Cell(currentRow, 14).Value = p.PurchasePrice;
                worksheet.Cell(currentRow, 15).Value = p.SellingPrice;
                worksheet.Cell(currentRow, 16).Value = p.TaxRate;
                worksheet.Cell(currentRow, 17).Value = p.Discount;
                worksheet.Cell(currentRow, 18).Value = p.FullName;
                worksheet.Cell(currentRow, 19).Value = p.TotalRecords;

                // Zebra stripe effect
                if (srno % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 19).Style.Fill.BackgroundColor = XLColor.LightCyan;
                }

                currentRow++;
                srno++;
            }

            // --- Adjust column widths ---
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }


        #endregion


    }
}
