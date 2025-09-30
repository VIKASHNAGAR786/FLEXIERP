using ClosedXML.Excel;
using DocumentFormat.OpenXml.ExtendedProperties;
using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using SelectPdf;

namespace FLEXIERP.BusinessLayer
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepo _saleRepo;
        private readonly IAccountRepo accountRepo;
        public SaleService(ISaleRepo saleRepo, IAccountRepo accountRepo)
        {
            _saleRepo = saleRepo;
            this.accountRepo = accountRepo;
        }

        #region Product By Barcode
        public async Task<ProductByBarcode_DTO?> GetProductByBarcode(string barCode)
        {
            return await _saleRepo.GetProductByBarcode(barCode);
        }
        #endregion

        #region make Sale
        public async Task<int> InsertSaleAsync(Sale sale)
        {
            return await _saleRepo.InsertSaleAsync(sale);
        }
        #endregion

        #region Get Sale
        public async Task<List<Sale_DTO>> GetSalesAsync(PaginationFilter pagination)
        {
            return await _saleRepo.GetSalesAsync(pagination);
        }
        public async Task<byte[]> GetSalesReportPdf(PaginationFilter filter)
        {
            // Dummy data
            List<Sale_DTO> saledata = await _saleRepo.GetSalesAsync(filter);
            CompanyInfoDto? company = await this.accountRepo.GetCompanyInfoByUserAsync(2);

            // Build HTML
            var html = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; font-size: 10pt; margin: 5px; color: #333; }}
        .company-header {{ text-align:center; margin-bottom: 15px; }}
        .company-header h1 {{ font-size: 22pt; color: #1f4e79; margin-bottom: 2px; }}
        .company-header h3 {{ font-size: 11pt; color: #555; margin: 2px 0; }}
        .report-title {{ 
            background-color: #00bfff; 
            color: white; 
            padding: 8px; 
            font-weight: bold; 
            font-size: 14pt; 
            margin-top: 10px; 
            border-radius: 5px;
            display: inline-block;
        }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background-color: #00bfff; color: white; border: 1px solid #555; padding: 6px; text-align: center; }}
        td {{ border: 1px solid #ccc; padding: 5px; text-align: center; }}
        tbody tr:nth-child(even) {{ background-color: #f2f7fb; }} /* zebra stripes */
        tbody tr:hover {{ background-color: #d6eefc; }} /* hover effect */
    </style>
</head>
<body>
    <div class='company-header'>
        <h1>{company.CompanyName}</h1>
        <h3>{company.Address}</h3>
        <h3>{company.ContactNo} | {company.Email}</h3>
        <div class='report-title'>Product Table Report</div>
    </div>

    <table>
        <thead>
            <tr>
                <th>SrNo</th>
                <th>CustomerName</th>
                <th>TotalItems</th>
                <th>TotalAmount</th>
                <th>TotalDiscount</th>
                <th>OrderDate</th>
                <th>FullName</th>
                <th>TotalRows</th>
              
            </tr>
        </thead>
        <tbody>";

            int srno = 1;
            foreach (var p in saledata)
            {
                html += $@"
            <tr>
                <td>{p.SrNo}</td>
                <td>{p.CustomerName}</td>
                <td>{p.TotalItems}</td>
                <td>{p.TotalAmount}</td>
                <td>{p.TotalDiscount}</td>
                <td>{p.OrderDate}</td>
                <td>{p.FullName}</td>
                <td>{p.TotalRows}</td>
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

        public async Task<byte[]> GetSalesReportExcel(PaginationFilter filter)
        {
            // Get product data
            IEnumerable<Sale_DTO> products = await _saleRepo.GetSalesAsync(filter);
            CompanyInfoDto? company = await this.accountRepo.GetCompanyInfoByUserAsync(2);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sold Product Report");

            int currentRow = 1;

            // --- Company Header ---
            worksheet.Cell(currentRow, 1).Value = company.CompanyName;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 22;
            worksheet.Range(currentRow, 1, currentRow, 8).Merge();
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = company.Address;
            worksheet.Range(currentRow, 1, currentRow, 8).Merge();
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = @$"{company.ContactNo} | {company.Email}";
            worksheet.Range(currentRow, 1, currentRow, 8).Merge();
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow += 2;

            worksheet.Cell(currentRow, 1).Value = "Product Table Report";
            worksheet.Range(currentRow, 1, currentRow, 8).Merge();
            worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.DeepSkyBlue;
            worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
            currentRow++;

            // --- Table Header ---
            string[] headers = new string[]
            {
        "SrNo",
        "CustomerName",
        "TotalItems",
        "TotalAmount",
        "TotalDiscount",
        "OrderDate",
        "FullName",
        "TotalRows",
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
                worksheet.Cell(currentRow, 1).Value = p.SrNo;
                worksheet.Cell(currentRow, 2).Value = p.CustomerName;
                worksheet.Cell(currentRow, 3).Value = p.TotalItems;
                worksheet.Cell(currentRow, 4).Value = p.TotalAmount;
                worksheet.Cell(currentRow, 5).Value = p.TotalDiscount;
                worksheet.Cell(currentRow, 6).Value = p.OrderDate;
                worksheet.Cell(currentRow, 7).Value = p.FullName;
                worksheet.Cell(currentRow, 8).Value = p.TotalRows;
              

                // Zebra stripe effect
                if (srno % 2 == 0)
                {
                    worksheet.Range(currentRow, 1, currentRow, 8).Style.Fill.BackgroundColor = XLColor.LightCyan;
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

        #region Old customer 
        public Task<List<OldCustomerDTO>> GetOldCustomersAsync(PaginationFilter pagination)
        {
            return _saleRepo.GetOldCustomersAsync(pagination);
        }
        #endregion

        #region Get Customer with sales 
        public  async Task<List<CustomerWithSalesDTO>> GetCustomersWithSalesAsync(PaginationFilter pagination)
        {
            return await _saleRepo.GetCustomersWithSalesAsync(pagination);
        }
        #endregion
    }
}
