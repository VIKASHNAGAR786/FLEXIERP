using ClosedXML.Excel;
using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
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
    public class CommonMasterService : ICommonMasterService
    {
        private readonly ICommonMasterRepo commonmaster;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CommonMasterService(ICommonMasterRepo commonmaster, IHttpContextAccessor httpContextAccessor)
        {
            this.commonmaster = commonmaster;
            _httpContextAccessor = httpContextAccessor;
        }
        #region Get DashBoard
        public async Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate)
        {
            return await this.commonmaster.GetDashboardMetricsAsync(startDate, endDate);
        }

        public async Task<byte[]> GenerateDashboardPdf(string startDate, string endDate)
        {
            var dashboard = await this.commonmaster.GetDashboardMetricsAsync(startDate, endDate);
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    page.Header()
                        .Column(header =>
                        {
                            header.Item().Text("Finance Dashboard").FontSize(20).Bold().FontColor(Colors.Teal.Darken2);
                            header.Item().Text("Overview of your cash & cheque transactions").FontColor(Colors.Grey.Medium);
                        });

                    // Metrics Cards
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Container().Background(Colors.Grey.Darken3).Padding(10).Border(Colors.Teal.Medium, 1).Column(c =>
                            {
                                c.Item().Text("Total Cash Received").FontSize(10).FontColor(Colors.Grey.Lighten1);
                                c.Item().Text($"₹ {dashboard.TotalCashReceived:N2}").FontSize(16).Bold().FontColor(Colors.Teal.Medium);
                                c.Item().Text($"Growth: {dashboard.CashGrowthPercent:N2}%").FontColor(dashboard.CashGrowthPercent < 0 ? Colors.Red.Medium : Colors.Green.Medium).FontSize(10);
                            });

                            row.RelativeItem().Container().Background(Colors.Grey.Darken3).Padding(10).Border(Colors.Purple.Medium, 1).Column(c =>
                            {
                                c.Item().Text("Total Cheque Received").FontSize(10).FontColor(Colors.Grey.Lighten1);
                                c.Item().Text($"₹ {dashboard.TotalChequeReceived:N2}").FontSize(16).Bold().FontColor(Colors.Purple.Medium);
                                c.Item().Text($"Growth: {dashboard.ChequeGrowthPercent:N2}%").FontColor(dashboard.ChequeGrowthPercent < 0 ? Colors.Red.Medium : Colors.Green.Medium).FontSize(10);
                            });

                            row.RelativeItem().Container().Background(Colors.Grey.Darken3).Padding(10).Border(Colors.Yellow.Medium, 1).Column(c =>
                            {
                                c.Item().Text("Total Balance Due").FontSize(10).FontColor(Colors.Grey.Lighten1);
                                c.Item().Text($"₹ {dashboard.TotalBalanceDue:N2}").FontSize(16).Bold().FontColor(Colors.Yellow.Medium);
                            });

                            row.RelativeItem().Container().Background(Colors.Grey.Darken3).Padding(10).Border(Colors.Pink.Medium, 1).Column(c =>
                            {
                                c.Item().Text("Trends & Growth").FontSize(10).FontColor(Colors.Grey.Lighten1);
                                c.Item().Text("📈 Increasing").FontSize(16).Bold().FontColor(Colors.Pink.Medium);
                            });
                        });

                        // Transactions Table
                        column.Item().Table(table =>
                        {
                            // Define 7 columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80); // Date
                                columns.RelativeColumn(2);  // Customer
                                columns.RelativeColumn();   // Received Amount
                                columns.RelativeColumn();   // Balance Due
                                columns.RelativeColumn();   // Total Amount
                                columns.RelativeColumn();   // Payment Type
                                columns.RelativeColumn();   // Transaction Type
                            });

                            // Header row with background and border
                            table.Header(header =>
                            {
                                header.Cell().Border(1).Background("#263238").Padding(4).Text("Date").FontColor(Colors.White).Bold();
                                header.Cell().Border(1).Background("#263238").Padding(4).Text("Customer").FontColor(Colors.White).Bold();
                                header.Cell().Border(1).Background("#263238").Padding(4).Text("Received Amount").FontColor(Colors.White).Bold();
                                header.Cell().Border(1).Background("#263238").Padding(4).Text("Balance Due").FontColor(Colors.White).Bold();
                                header.Cell().Border(1).Background("#263238").Padding(4).Text("Total Amount").FontColor(Colors.White).Bold();
                                header.Cell().Border(1).Background("#263238").Padding(4).Text("Payment Type").FontColor(Colors.White).Bold();
                                header.Cell().Border(1).Background("#263238").Padding(4).Text("Transaction Type").FontColor(Colors.White).Bold();
                            });

                            // Data rows with borders
                            foreach (var t in dashboard.recenttransaction)
                            {
                                table.Cell().Border(1).Padding(4).Text(t.Date);
                                table.Cell().Border(1).Padding(4).Text(t.CustomerName);
                                table.Cell().Border(1).Padding(4).Text($"₹ {t.ReceivedAmount:N0}");
                                table.Cell().Border(1).Padding(4).Text($"₹ {t.BalanceDue:N0}");
                                table.Cell().Border(1).Padding(4).Text($"₹ {t.TotalAmount:N0}");
                                table.Cell().Border(1).Padding(4).Text(t.PaymentType);
                                table.Cell().Border(1).Padding(4).Text(t.TransactionType);
                            }
                        });

                    });

                    // Footer
                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:dd-MMM-yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });

            return document.GeneratePdf();
        }
        public async Task<byte[]> GenerateDashboardExcel(string startDate, string endDate)
        {
            var dashboard = await this.commonmaster.GetDashboardMetricsAsync(startDate, endDate);

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Finance Dashboard");

                int row = 1;

                // ====== Title ======
                ws.Cell(row, 1).Value = "Finance Dashboard Summary";
                var title = ws.Range(row, 1, row, 7);
                title.Merge();
                title.Style.Font.Bold = true;
                title.Style.Font.FontSize = 20;
                title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                title.Style.Font.FontColor = XLColor.FromHtml("#1A237E");
                title.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF6");
                row += 2;

                // ====== Date Range ======
                ws.Cell(row, 1).Value = $"Report Period: {startDate} to {endDate}";
                ws.Range(row, 1, row, 7).Merge();
                ws.Row(row).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(row).Style.Font.Italic = true;
                ws.Row(row).Style.Font.FontColor = XLColor.FromHtml("#37474F");
                row += 2;

                // ====== Summary Section ======
                ws.Cell(row, 1).Value = "Metrics";
                ws.Cell(row, 2).Value = "Amount (₹)";
                ws.Range(row, 1, row, 2).Style.Font.Bold = true;
                ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#263238");
                ws.Range(row, 1, row, 2).Style.Font.FontColor = XLColor.White;
                ws.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;

                ws.Cell(row, 1).Value = "Total Cash Received";
                ws.Cell(row, 2).Value = dashboard.TotalCashReceived;
                row++;

                ws.Cell(row, 1).Value = "Total Cheque Received";
                ws.Cell(row, 2).Value = dashboard.TotalChequeReceived;
                row++;

                ws.Cell(row, 1).Value = "Total Balance Due";
                ws.Cell(row, 2).Value = dashboard.TotalBalanceDue;
                row += 2;

                var summaryRange = ws.Range(3, 1, row - 1, 2);
                summaryRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                summaryRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                summaryRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // ====== Transactions Table ======
                ws.Cell(row, 1).Value = "Date";
                ws.Cell(row, 2).Value = "Customer";
                ws.Cell(row, 3).Value = "Received Amount (₹)";
                ws.Cell(row, 4).Value = "Balance Due (₹)";
                ws.Cell(row, 5).Value = "Total Amount (₹)";
                ws.Cell(row, 6).Value = "Payment Type";
                ws.Cell(row, 7).Value = "Transaction Type";

                var header = ws.Range(row, 1, row, 7);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D47A1");
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                row++;

                foreach (var t in dashboard.recenttransaction)
                {
                    ws.Cell(row, 1).Value = t.Date;
                    ws.Cell(row, 2).Value = t.CustomerName;
                    ws.Cell(row, 3).Value = t.ReceivedAmount;
                    ws.Cell(row, 4).Value = t.BalanceDue;
                    ws.Cell(row, 5).Value = t.TotalAmount;
                    ws.Cell(row, 6).Value = t.PaymentType;
                    ws.Cell(row, 7).Value = t.TransactionType;

                    row++;
                }

                var tableRange = ws.Range(8, 1, row - 1, 7);
                tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                tableRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // ====== Footer Note ======
                ws.Cell(row + 1, 1).Value = "Report Generated on: " + DateTime.Now.ToString("dd MMM yyyy HH:mm");
                ws.Range(row + 1, 1, row + 1, 7).Merge();
                ws.Row(row + 1).Style.Font.Italic = true;
                ws.Row(row + 1).Style.Font.FontColor = XLColor.FromHtml("#607D8B");
                ws.Row(row + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // ====== Auto-fit & Zoom ======
                ws.Columns().AdjustToContents();
                ws.SheetView.ZoomScale = 100;

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        #endregion

        #region cheque details 
        public async Task<List<ReceivedChequeDto>> GetReceivedChequesAsync(PaginationFilter pagination)
        {
            return await this.commonmaster.GetReceivedChequesAsync(pagination);
        }
        #endregion

        #region notes
        public async Task<int> SaveNoteAsync(SaveNotes note)
        {
            return await this.commonmaster.SaveNoteAsync(note);
        }
        public async Task<List<NoteDto>> GetAllNotesAsync()
        {
            return await this.commonmaster.GetAllNotesAsync();
        }
        public async Task<NoteDetailsDto> GetNoteDetailsByIdAsync(int rowid)
        {
            return await this.commonmaster.GetNoteDetailsByIdAsync(rowid);
        }
        #endregion

    }
}
