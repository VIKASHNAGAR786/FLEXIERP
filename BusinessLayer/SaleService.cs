using ClosedXML.Excel;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.InkML;
using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
using Microsoft.AspNetCore.Http.HttpResults;
using QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using Razorpay.Api;
using SelectPdf;


namespace FLEXIERP.BusinessLayer
{
    public class SaleService : ISaleService
    {
        private readonly ISaleRepo _saleRepo;
        private readonly IAccountRepo accountRepo;
        private readonly ICommonMasterRepo commonmaster;
        public SaleService(ISaleRepo saleRepo, IAccountRepo accountRepo, ICommonMasterRepo _commonmaster)
        {
            _saleRepo = saleRepo;
            this.accountRepo = accountRepo;
            this.commonmaster = _commonmaster;
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
            int payid = 0;
            if (sale.Customer is not null) {
                if (sale.Customer.PaymentMode == 1)
                {
                    SaveCashPaymentDto cash = new SaveCashPaymentDto
                    {

                        Amount = (decimal)sale.Customer.PaidAmt!,
                        PaymentDate = DateTime.UtcNow,
                        CreatedBy = sale.CreatedBy,
                       
                    };
                    payid = await this.commonmaster.SaveCashPaymentAsync(cash);
                }
                else if(sale.Customer.PaymentMode == 2)
                {
                    SaveChequePaymentDto cheque = new SaveChequePaymentDto
                    {
                        ChequeNumber  = sale.Customer.chequepayment.ChequeNumber,
                        BankName = sale.Customer.chequepayment.BankName,
                        BranchName = sale.Customer.chequepayment.BranchName,
                        ChequeDate = sale.Customer.chequepayment.ChequeDate,
                        Amount = sale.Customer.chequepayment.Amount,
                        IFSC_Code = sale.Customer.chequepayment.IFSC_Code,
                        CreatedBy = sale.CreatedBy,

                    };
                    payid = await this.commonmaster.SaveChequePaymentAsync(cheque);
                }
                sale.Customer.payid = payid;
            }
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

        #region Sale Invoice
        public async Task<byte[]> GetReceiptPdf(int saleId, int userId)
        {
            // --- Fetch data ---
            var receipt = await _saleRepo.GetReceiptDetail(saleId);
            var company = await accountRepo.GetCompanyInfoByUserAsync(userId);

            if (receipt == null || receipt.CustomerInfo == null)
                throw new Exception("Receipt data not found.");

            var customer = receipt.CustomerInfo;
            var details = receipt.SaleDetails;
            var extracharges = receipt.extracharges;

            decimal subTotal = details.Sum(d => d.TotalAmount);
            decimal extraTotal = extracharges?.Sum(e => e.chargeamount) ?? 0;
            decimal grandTotal = subTotal + extraTotal;

            // --- Create PDF document ---
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(TextStyle.Default.FontSize(10));

                    // --- Background watermark ---
                    page.Background().AlignCenter().AlignMiddle().Element(c =>
                    {
                        c.Rotate(-45)
                         .Text(company.CompanyName)
                         .FontSize(80)
                         .Bold()
                         .FontColor(Colors.Grey.Lighten3)
                         .AlignCenter();
                    });

                    // --- Header Section ---
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            // Company Info
                            row.RelativeColumn().Column(col =>
                            {
                                col.Item().Text(company.CompanyName)
                                    .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                                col.Item().Text(company.Address ?? "—")
                                    .FontSize(9).FontColor(Colors.Grey.Darken2);
                                col.Item().Text($"Phone: {company.ContactNo} | Email: {company.Email}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                            });

                            // Company Logo
                            row.ConstantColumn(80).AlignRight().AlignMiddle().Element(e =>
                            {
                                var logoPath = string.IsNullOrWhiteSpace(company.CompanyLogo)
                                    ? null
                                    : Path.Combine("Documents", company.CompanyLogo);

                                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                                    e.Image(logoPath, ImageScaling.FitArea);
                                else
                                    e.Border(1).BorderColor(Colors.Grey.Lighten2)
                                     .AlignCenter().AlignMiddle().Height(50)
                                     .Text("No Logo").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });

                        header.Item().PaddingVertical(5).LineHorizontal(1);
                    });

                    // --- Content Section ---
                    page.Content().Column(content =>
                    {
                        content.Spacing(10);

                        // Invoice title + Date
                        content.Item().Text("INVOICE / RECEIPT")
                            .FontSize(14).Bold().AlignCenter().FontColor(Colors.Black);
                        content.Item().AlignRight().Text($"Date: {DateTime.Now:dd-MM-yyyy HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);

                        // Customer Info
                        content.Item().PaddingVertical(10).Text("Customer Information")
                            .FontSize(11).Bold().FontColor(Colors.Blue.Medium);

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(4);
                            });

                            void AddRow(string label, string? value)
                            {
                                table.Cell().Element(CellLabel).Text(label).Bold();
                                table.Cell().Element(CellValue).Text(value ?? "N/A");
                            }

                            static IContainer CellLabel(IContainer container) =>
                                container.PaddingVertical(2).PaddingLeft(5);
                            static IContainer CellValue(IContainer container) =>
                                container.PaddingVertical(2);

                            AddRow("Name:", customer.CustomerName);
                            AddRow("Phone:", customer.PhoneNo);
                            AddRow("Email:", customer.Email);
                            AddRow("Payment Mode:", customer.PaymentMode);
                        });

                        // --- Sale Details Table ---
                        content.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Product
                                columns.RelativeColumn(1); // Qty
                                columns.RelativeColumn(1.5f); // Price
                                columns.RelativeColumn(1.5f); // Discount
                                columns.RelativeColumn(1.5f); // Tax
                                columns.RelativeColumn(1.5f); // Total
                            });

                            // Header
                            table.Header(header =>
                            {
                                string HeaderStyle = Colors.Blue.Medium;

                                header.Cell().Element(CellHeader).Text("Product");
                                header.Cell().Element(CellHeader).Text("Qty");
                                header.Cell().Element(CellHeader).Text("Price");
                                header.Cell().Element(CellHeader).Text("Discount");
                                header.Cell().Element(CellHeader).Text("Tax");
                                header.Cell().Element(CellHeader).Text("Total");

                                static IContainer CellHeader(IContainer container) =>
                                    container.Background(Colors.Blue.Medium)
                                             .Padding(5)
                                             .AlignCenter()
                                             .DefaultTextStyle(TextStyle.Default.FontColor(Colors.White).Bold());
                            });

                            // Rows (with alternate shading)
                            bool alternate = false;
                            foreach (var d in details)
                            {
                                var bg = alternate ? Colors.Grey.Lighten4 : Colors.White;
                                alternate = !alternate;

                                table.Cell().Element(c => RowCell(c, bg)).Text(d.ProductName);
                                table.Cell().Element(c => RowCell(c, bg)).AlignCenter().Text(d.Quantity.ToString());
                                table.Cell().Element(c => RowCell(c, bg)).AlignRight().Text($"₹{d.Price:N2}");
                                table.Cell().Element(c => RowCell(c, bg)).AlignRight().Text($"₹{d.TotalDiscount:N2}");
                                table.Cell().Element(c => RowCell(c, bg)).AlignRight().Text($"₹{d.Tax:N2}");
                                table.Cell().Element(c => RowCell(c, bg)).AlignRight().Text($"₹{d.TotalAmount:N2}");
                            }

                            static IContainer RowCell(IContainer container, string bg) =>
                                container.Background(bg)
                                         .Padding(5)
                                         .BorderBottom(1)
                                         .BorderColor(Colors.Grey.Lighten2)
                                         .DefaultTextStyle(TextStyle.Default.FontSize(10));

                            // --- Extra Charges ---
                            if (extracharges != null && extracharges.Any())
                            {
                                table.Cell().ColumnSpan(6).PaddingTop(8);
                                table.Cell().ColumnSpan(6)
                                    .Element(c => c.Background(Colors.Grey.Lighten3).Padding(5))
                                    .Text("Additional Charges")
                                    .FontColor(Colors.Blue.Darken2)
                                    .Bold();

                                foreach (var e in extracharges)
                                {
                                    table.Cell().ColumnSpan(4).Element(c => RowCell(c, Colors.White))
                                        .Text($"• {e.chargename}");
                                    table.Cell().ColumnSpan(2).Element(c => RowCell(c, Colors.White))
                                        .AlignRight().Text($"₹{e.chargeamount:N2}");
                                }
                            }

                            // --- Subtotal, Extra, and Grand Total ---
                            void AddSummaryRow(string label, decimal value, bool highlight = false)
                            {
                                table.Cell().ColumnSpan(4);
                                table.Cell().ColumnSpan(2)
                                    .Element(c => c.Background(highlight ? Colors.Grey.Lighten3 : Colors.White)
                                                 .Padding(5)
                                                 .AlignRight())
                                    .Text($"{label} ₹{value:N2}")
                                    .Bold();
                            }

                            AddSummaryRow("Subtotal:", subTotal);
                            if (extraTotal > 0)
                                AddSummaryRow("Extra Charges:", extraTotal);
                            AddSummaryRow("Grand Total:", grandTotal, true);

                            // --- Paid & Balance Info ---
                            table.Cell().ColumnSpan(4);
                            table.Cell().ColumnSpan(2)
                                .Element(c => c.PaddingTop(8).AlignRight())
                                .Text($"Paid Amount: ₹{customer.paidamt:N2}\nBalance Due: ₹{customer.baldue:N2}")
                                .FontSize(10)
                                .Bold();
                        });
                    });

                    // --- Footer Section ---
                    page.Footer().AlignCenter()
                        .PaddingTop(10)
                        .Text("Thank you for your business!")
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            return document.GeneratePdf();
        }

        #endregion
    }
}
