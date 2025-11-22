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
            sale.invoiceno = await this.commonmaster.GetInvoiceNumber();
            int result =  await _saleRepo.InsertSaleAsync(sale);
            if(result > 0)
            {
                await this.commonmaster.UpdateInvoiceNumber((int)sale.CreatedBy!);
                return result;
            }
            else
            {
                throw new Exception("Something went wrong");
            }
        }
        #endregion

        #region Get Sale
        public async Task<List<Sale_DTO>> GetSalesAsync(PaginationFilter pagination)
        {
            return await _saleRepo.GetSalesAsync(pagination);
        }
        public async Task<byte[]> GetSalesReportPdf(PaginationFilter filter, int userid)
        {
            // Dummy data
            List<Sale_DTO> saledata = await _saleRepo.GetSalesAsync(filter);
            CompanyInfoDto? company = await this.accountRepo.GetCompanyInfoByUserAsync(userid);

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
                <th>TotalExtraCharges</th>
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
                <td>{p.extracharges}</td>
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

        public async Task<byte[]> GetSalesReportExcel(PaginationFilter filter, int userid)
        {
            // Get product data
            IEnumerable<Sale_DTO> products = await _saleRepo.GetSalesAsync(filter);
            CompanyInfoDto? company = await this.accountRepo.GetCompanyInfoByUserAsync(userid);

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
        "TotalExtraCharges",
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
                worksheet.Cell(currentRow, 5).Value = p.extracharges;
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

            // --- Create PDF ---
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(TextStyle.Default.FontSize(10));

                    // ---------------- HEADER (Red Banner) ----------------
                    page.Header().Column(col =>
                    {
                        col.Item().Background("#cc0000").Padding(10).AlignCenter().Column(h =>
                        {
                            h.Item().Text(company.CompanyName.ToUpper())
                                .FontSize(18).Bold().FontColor("#ffffff");

                            h.Item().Text(company.Address ?? "")
                                .FontSize(9).FontColor("#ffffff");

                            h.Item().Text($"MOBILE: {company.ContactNo}   EMAIL: {company.Email}")
                                .FontSize(9).FontColor("#ffffff");
                        });

                        col.Item().PaddingTop(5).AlignCenter()
                            .Text("INVOICE").FontSize(14).Bold();
                    });

                    // ---------------- CUSTOMER INFO BOX ----------------
                    page.Content().Column(content =>
                    {
                        // 3 Column box
                        content.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                            });

                            // Row 1
                            t.Cell().Border(1).Padding(5).Text($"COSTUMER NAME\n{customer.CustomerName}").Bold();
                            t.Cell().Border(1).Padding(5).Text($"INVOICE DATE\n{DateTime.Now}").Bold();
                            t.Cell().Border(1).Padding(5).Text($"INVOICE NO.\n{customer.invoiceno}").Bold();
                        });

                        content.Item().PaddingVertical(8);

                        // ---------------- SALE DETAILS TABLE ----------------
                        // ---------------- SALE DETAILS TABLE ----------------
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(30);     // Sr No
                                c.RelativeColumn(4);      // Description
                                c.RelativeColumn(2);      // Qty
                                c.RelativeColumn(2);      // Amount
                                c.RelativeColumn(2);      // Total
                            });

                            // Header row
                            table.Header(h =>
                            {
                                HeaderCell(h, "NO.");
                                HeaderCell(h, "DESCRIPTION OF GOODS");
                                HeaderCell(h, "QUANTITY");
                                HeaderCell(h, "AMOUNT");
                                HeaderCell(h, "TOTAL AMOUNT");
                            });

                            int index = 1;

                            foreach (var d in details)
                            {
                                table.Cell().Border(1).Padding(4).AlignCenter().Text(index.ToString());
                                table.Cell().Border(1).Padding(4).Text(d.ProductName);
                                table.Cell().Border(1).Padding(4).AlignCenter().Text(d.Quantity.ToString());
                                table.Cell().Border(1).Padding(4).AlignCenter().Text(d.Price.ToString("N2"));
                                table.Cell().Border(1).Padding(4).AlignCenter().Text(d.TotalAmount.ToString("N2"));
                                index++;
                            }

                            // ---- ADD MINIMUM 3 EMPTY ROWS ----
                            int blankRows = Math.Max(0, 3 - details.Count);

                            for (int i = 0; i < blankRows; i++)
                            {
                                table.Cell().Border(1).Padding(4).AlignCenter().Text("");
                                table.Cell().Border(1).Padding(4).Text("");
                                table.Cell().Border(1).Padding(4).AlignCenter().Text("");
                                table.Cell().Border(1).Padding(4).AlignCenter().Text("");
                                table.Cell().Border(1).Padding(4).AlignCenter().Text("");
                            }

                            // -------- TOTAL QUANTITY ROW --------
                            table.Cell().Border(1).Padding(4).AlignCenter().Text("1");
                            table.Cell().Border(1).Padding(4).Text("TOTAL QUANTITY").Bold();
                            table.Cell().Border(1).Padding(4).Text("");
                            table.Cell().Border(1).Padding(4).Text("BARDANA").Bold();
                            table.Cell().Border(1).Padding(4).Text("");

                            // -------- GRAND TOTAL ROW --------
                            table.Cell().ColumnSpan(4).Border(1).Padding(5).AlignRight().Text("GRAND TOTAL").Bold();
                            table.Cell().Border(1).Padding(5).AlignCenter().Text(grandTotal.ToString("N2")).Bold();
                        });

                    });

                    page.Footer().AlignCenter().PaddingTop(10)
                        .Text("Thank you for your business!").FontSize(9);
                });
            });

            // helper
           
            return document.GeneratePdf();
        }

        static void HeaderCell(QuestPDF.Fluent.TableCellDescriptor h, string text)
        {
            h.Cell().Background("#e6e6e6").Border(1).Padding(5)
                .Text(text).Bold().AlignCenter();
        }

        #endregion
    }
}
