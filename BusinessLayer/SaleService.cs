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
using System.Text;


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
            ReceiptDTO? receipt = await _saleRepo.GetReceiptDetail(saleId);
            if (receipt == null || receipt.CustomerInfo == null)
                throw new Exception("Receipt data not found.");
            BankAccountforprintDTO bankdata = await this.commonmaster.GetCompanyBankAccountsForPrint();

            var company = await accountRepo.GetCompanyInfoByUserAsync(userId);
            var customer = receipt.CustomerInfo;
            var details = receipt.SaleDetails;
            var extracharges = receipt.extracharges;

            decimal subTotal = details.Sum(d => d.TotalAmount);
            decimal extraTotal = extracharges?.Sum(e => e.chargeamount) ?? 0;
            decimal grandTotal = subTotal + extraTotal;

            HtmlTemplate? htmladdons = await this.commonmaster.GethtmlContent(1);

            if (htmladdons is null)
                throw new Exception("No template found.");

            // --- Build Sale Details Table ---
            var sb = new StringBuilder();
            int index = 1;

            // Table start
            sb.AppendLine("<table style='width:100%; border-collapse:collapse; font-size:12px;'>");
            sb.AppendLine("<thead><tr style='background:#f0f0f0;'>");
            sb.AppendLine("<th style='border:1px solid #ccc;'>NO.</th>");
            sb.AppendLine("<th style='border:1px solid #ccc;'>DESCRIPTION OF GOODS</th>");
            sb.AppendLine("<th style='border:1px solid #ccc;'>QUANTITY</th>");
            sb.AppendLine("<th style='border:1px solid #ccc;'>AMOUNT</th>");
            sb.AppendLine("<th style='border:1px solid #ccc;'>TOTAL AMOUNT</th>");
            sb.AppendLine("</tr></thead><tbody>");

            // Sale items
            foreach (var d in details)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'>{index}</td>");
                sb.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'>{d.ProductName}</td>");
                sb.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'>{d.Quantity}</td>");
                sb.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'>{d.Price:N2}</td>");
                sb.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'>{d.TotalAmount:N2}</td>");
                sb.AppendLine("</tr>");
                index++;
            }

            // Grand total
            sb.AppendLine("<tr>");
            sb.AppendLine("<td colspan='4' style='border:1px solid #ccc; text-align:right;'><b> TOTAL </b></td>");
            sb.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'><b>{details.Sum(d => d.TotalAmount):N2}</b></td>");
            sb.AppendLine("</tr>");

            sb.AppendLine("</tbody></table>");

            string saleDetailsTable = sb.ToString();

            //Extra Charges 
            var sbExtra = new StringBuilder();
            int chargeIndex = 1;

            sbExtra.AppendLine("<table style='width:100%; border-collapse:collapse; font-size:12px; margin-top:20px;'>");
            sbExtra.AppendLine("<thead><tr style='background:#f0f0f0;'>");
            sbExtra.AppendLine("<th style='border:1px solid #ccc;'>NO.</th>");
            sbExtra.AppendLine("<th style='border:1px solid #ccc;' colspan='3'>CHARGE NAME</th>");
            sbExtra.AppendLine("<th style='border:1px solid #ccc;'>AMOUNT</th>");
            sbExtra.AppendLine("</tr></thead><tbody>");

            if (extracharges != null && extracharges.Any())
            {
                foreach (var charge in extracharges)
                {
                    sbExtra.AppendLine("<tr>");
                    sbExtra.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'>{chargeIndex}</td>");
                    sbExtra.AppendLine($"<td colspan='3' style='border:1px solid #ccc;'>{charge.chargename}</td>");
                    sbExtra.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'>{charge.chargeamount:N2}</td>");
                    sbExtra.AppendLine("</tr>");
                    chargeIndex++;
                }

                // Total of extra charges
                sbExtra.AppendLine("<tr>");
                sbExtra.AppendLine("<td colspan='4' style='border:1px solid #ccc; text-align:right;'><b>EXTRA CHARGES TOTAL</b></td>");
                sbExtra.AppendLine($"<td style='border:1px solid #ccc; text-align:center;'><b>{extracharges.Sum(c => c.chargeamount):N2}</b></td>");
                sbExtra.AppendLine("</tr>");
            }
            else
            {
                sbExtra.AppendLine("<tr>");
                sbExtra.AppendLine("<td colspan='5' style='border:1px solid #ccc; text-align:center;'>No extra charges</td>");
                sbExtra.AppendLine("</tr>");
            }

            sbExtra.AppendLine("</tbody></table>");

            string extraChargesTable = sbExtra.ToString();

            // --- Replace placeholders in template ---
            string html = htmladdons.htmlcontent!
                .Replace("{{CustomerName}}", customer.CustomerName)
                .Replace("{{PhoneNo}}", customer.PhoneNo)
                .Replace("{{Email}}", customer.Email)
                .Replace("{{PaymentMode}}", customer.PaymentMode)
                .Replace("{{Remark}}", customer.Remark)
                .Replace("{{TotalItems}}", customer.TotalItems.ToString())
                .Replace("{{TotalAmount}}", customer.TotalAmount.ToString("N2"))
                .Replace("{{TotalDiscount}}", customer.TotalDiscount.ToString("N2"))
                .Replace("{{paidamt}}", customer.paidamt.ToString("N2"))
                .Replace("{{baldue}}", customer.baldue.ToString("N2"))
                .Replace("{{invoiceno}}", customer.invoiceno)
                .Replace("{{CompanyName}}", company.CompanyName)
                .Replace("{{CompanyEmail}}", company.Email)
                .Replace("{{ContactNo}}", company.ContactNo)
                .Replace("{{WhatsAppNo}}", company.WhatsAppNo)
                .Replace("{{Address}}", company.Address)
                .Replace("{{FullName}}", company.FullName)
                .Replace("{{CompanyLogo}}", $"<img src='{company.CompanyLogo}' height='60' />")
                .Replace("{{SaleDetailsTable}}", saleDetailsTable)
                .Replace("{{ExtraCharges}}", extraChargesTable)
                .Replace("{{AccountName}}", bankdata.account_name)
                .Replace("{{BankName}}", bankdata.bank_name)
                .Replace("{{AccountNumber}}", bankdata.account_number)
                .Replace("{{IFSCCode}}", bankdata.ifsc_code)
                .Replace("{{InvoiceDate}}", DateTime.Now.ToString("hh:mm tt"))
                .Replace("{{BranchName}}", bankdata.branch_name);

            // --- Wrap with CSS ---
            string fullHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        {htmladdons.csscontent}
        body {{ font-family: Arial, sans-serif; font-size: 12px; }}
        h1,h2,h3 {{ margin: 0; }}
    </style>
</head>
<body>
    {html}
    <footer style='text-align:center;margin-top:20px;font-size:10px'>
        Thank you for your business!
    </footer>
</body>
</html>";

            // --- Convert to PDF ---
            HtmlToPdf converter = new HtmlToPdf();
            converter.Options.MarginTop = 20;
            converter.Options.MarginBottom = 20;
            converter.Options.MarginLeft = 15;
            converter.Options.MarginRight = 15;

            PdfDocument doc = converter.ConvertHtmlString(fullHtml);
            using var ms = new MemoryStream();
            doc.Save(ms);
            doc.Close();

            return ms.ToArray();
        }
        #endregion
    }
}
