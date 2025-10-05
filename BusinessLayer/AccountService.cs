using DocumentFormat.OpenXml.Spreadsheet;
using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS;
using FLEXIERP.MODELS.AGRIMANDI.Model;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using Razorpay.Api;
using System.Runtime.Intrinsics.X86;
using Colors = QuestPDF.Helpers.Colors;

namespace FLEXIERP.BusinessLayer
{
    public class AccountService : IAccountServices
    {
        private readonly IAccountRepo accountRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountService(IAccountRepo accountRepo, IHttpContextAccessor httpContextAccessor)
        {
            this.accountRepo = accountRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        #region common
        private (string Browser, string OS) Parse(string userAgent)
        {
            string browser = "Unknown";
            string os = "Unknown";

            // --- Detect Browser ---
            if (userAgent.Contains("Edg/"))
                browser = "Microsoft Edge";
            else if (userAgent.Contains("Chrome/") && !userAgent.Contains("Edg/"))
                browser = "Google Chrome";
            else if (userAgent.Contains("Firefox/"))
                browser = "Mozilla Firefox";
            else if (userAgent.Contains("Safari/") && !userAgent.Contains("Chrome/"))
                browser = "Apple Safari";
            else if (userAgent.Contains("MSIE") || userAgent.Contains("Trident/"))
                browser = "Internet Explorer";
            else browser = "Desktop";

            // --- Detect OS ---
            if (userAgent.Contains("Windows NT 10.0"))
                os = "Windows 10 / 11";  // Microsoft kept NT 10.0 for Windows 11
            else if (userAgent.Contains("Windows NT 6.3"))
                os = "Windows 8.1";
            else if (userAgent.Contains("Windows NT 6.2"))
                os = "Windows 8";
            else if (userAgent.Contains("Windows NT 6.1"))
                os = "Windows 7";
            else if (userAgent.Contains("Mac OS X"))
                os = "Mac OS X";
            else if (userAgent.Contains("Android"))
                os = "Android";
            else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
                os = "iOS";

            return (browser, os);
        }
        #endregion
        public async Task<User1?> Login(string? email, string? UserName, string password)
        {
            User1? result = await this.accountRepo.Login(email, UserName, password);
            if (result != null)
            {
                var httpContext = _httpContextAccessor.HttpContext; // Get current context
                var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();

                // Use helper to parse User-Agent
                var (browser, os) = this.Parse(userAgent ?? "");

                StartUserSession data = new StartUserSession
                {
                    Username = result.Username,
                    Password = result.PasswordHash,
                    IPAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    DeviceInfo = (browser, os).ToString()
                };

                await this.accountRepo.StartUserSession(data);


            }
            return result; // Add missing return statement
        }

        public Task<User1> Register(User1 user)
        {
            return this.accountRepo.Register(user);
        }

        public async Task<bool> LogoutUser(int userId)
        {
            return await this.accountRepo.LogoutUser(userId);
        }
        public async Task<IEnumerable<UserLoginHistoryDTO>> GetUserLoginHistory(int pageNo = 1, int pageSize = 20)
        {
            return await this.accountRepo.GetUserLoginHistory(pageNo, pageSize);
        }

        public async Task<CompanyInfoDto?> GetCompanyInfoByUserAsync(int userId)
        {
            return await this.accountRepo.GetCompanyInfoByUserAsync(userId);
        }
        public async Task<int> UpdateCompanyInfo(UpdateCompanyInfo UpdateCompanyInfo, IFormFile? file)
        {
            try
            {
                if (file != null)
                {
                    string commaseperatenames = string.Empty;
                    string newFileName = $"{Path.GetFileName(file.FileName)}";
                    string imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "Documents");

                    if (!Directory.Exists(imagesFolder))
                    {
                        Directory.CreateDirectory(imagesFolder);
                    }

                    string filePath = Path.Combine(imagesFolder, newFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    UpdateCompanyInfo.CompanyLogo = newFileName;
                }
                return await this.accountRepo.UpdateCompanyInfo(UpdateCompanyInfo);
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving product images: " + ex.Message);
                throw;
            }
        }

        #region Customer Ledger
        public Task<int> Savecustomerledger(Customerledgermodel customerledger)
        {
            return accountRepo.Savecustomerledger(customerledger);
        }
        public async Task<IEnumerable<CustomerledgerDto?>> GetCustomerledger(int pageNo = 1, int pageSize = 20)
        {
            return await this.accountRepo.GetCustomerledger(pageNo, pageSize);
        }
        public async Task<IEnumerable<CustomerledgerdetailDto?>> GetCustomerledgerdetails(int customerid, string StartDate, string EndDate)
        {
            return await this.accountRepo.GetCustomerledgerdetails(customerid, StartDate, EndDate);
        }


        public async Task<byte[]> GetCustomerledgerdetailspdf(int customerid, string StartDate, string EndDate)
        {
            List<CustomerledgerdetailDto?> data = (List<CustomerledgerdetailDto?>)await this.accountRepo.GetCustomerledgerdetails(customerid, StartDate, EndDate);
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(TextStyle.Default.FontSize(10));

                    // --- Header ---
                    page.Header().Row(header =>
                    {
                        header.RelativeColumn().Text("Customer Ledger Report").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                        header.ConstantColumn(100).Text($"Date: {DateTime.Now:dd-MMM-yyyy}").FontSize(9).AlignRight();
                    });

                    page.Content().Column(content =>
                    {
                        // --- Customer Info ---
                        content.Item().Padding(10).Background(Colors.Grey.Darken3.WithAlpha(0.6f)).Border(1).BorderColor(Colors.Teal.Medium)
                            .Row(row =>
                            {
                                row.RelativeColumn().Text($"Party Name: {data[0]!.customername!.ToUpper()}")
                                    .FontColor(Colors.White)
                                    .SemiBold();

                                row.RelativeColumn().Text($"Contact No: {data[0]!.contactno}")
                                    .FontColor(Colors.White);
                            });

                        content.Item().PaddingTop(10);

                        // --- Table ---
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                for (int i = 0; i < 10; i++)
                                    columns.RelativeColumn();
                            });

                            // Header
                            table.Header(header =>
                            {
                                string[] headers = new[]
                                {
                                "Paid Amount","Balance Due","Total Amt","Payment Mode",
                                "Transaction Type","Sale Date","Total Items","Discount","Tax","Transaction Date"
                            };

                                foreach (var h in headers)
                                    header.Cell().Element(CellHeaderStyle).Text(h);

                                static IContainer CellHeaderStyle(IContainer container) =>
                                    container.Background(Colors.Blue.Medium)
                                             .Padding(5)
                                             .AlignCenter()
                                             .DefaultTextStyle(TextStyle.Default.FontColor(Colors.White).SemiBold());
                            });

                            // Rows
                            foreach (var record in data)
                            {
                                table.Cell().Element(CellStyle).Text(record.paidamt.ToString("F2")).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.balancedue.ToString("F2")).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.totalamount.ToString("F2")).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.paymentmode.ToString()).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.transactiontype).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.saledate.ToString()).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.totalitems.ToString()).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.totaldiscount.ToString("F2")).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.tax.ToString()).AlignCenter();
                                table.Cell().Element(CellStyle).Text(record.transactiondate.ToString()).AlignCenter();
                            }

                           

                            static IContainer CellStyle(IContainer container) =>
                                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                         .Padding(4);
                        });
                    });

                    // --- Footer ---
                    page.Footer().AlignCenter()
                        .Text("Thank you for your business!").FontColor(Colors.Grey.Medium);
                });
            });

            return document.GeneratePdf();
        }

        #endregion
    }
}
