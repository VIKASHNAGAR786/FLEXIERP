using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DTOs;
using FLEXIERP.MODELS;
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

        #region login/logout/register
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

        #endregion

        #region companu info
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
        #endregion

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


        public async Task<byte[]> GetCustomerledgerdetailspdf(int customerid, string startDate, string endDate, int userId)
        {
            // Fetch data
            var data = (List<CustomerledgerdetailDto?>)await accountRepo.GetCustomerledgerdetails(customerid, startDate, endDate);
            var company = await accountRepo.GetCompanyInfoByUserAsync(userId);

            if (data == null || data.Count == 0)
                throw new Exception("No ledger data found.");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // --- Page Setup ---
                    page.Size(PageSizes.A4);
                    page.Margin(15);
                    page.DefaultTextStyle(TextStyle.Default.FontSize(10));

                    // --- Background Watermark ---
                    page.Background().AlignCenter().AlignMiddle().Element(c =>
                    {
                        c.Rotate(-45)
                         .Text(company.CompanyName ?? "Company")
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
                                col.Item().Text(company.CompanyName ?? "Company Name")
                                    .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                                col.Item().Text(company.Address ?? "—")
                                    .FontSize(9).FontColor(Colors.Grey.Darken2);
                                col.Item().Text($"Phone: {company.ContactNo ?? "—"} | Email: {company.Email ?? "—"}")
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
                        // --- Customer Info ---
                        content.Item().Padding(10)
                            .Background(Colors.Grey.Darken3.WithAlpha(0.6f))
                            .Border(1).BorderColor(Colors.Teal.Medium)
                            .Row(row =>
                            {
                                row.RelativeColumn().Text($"Party Name: {data[0]!.customername!.ToUpper()}")
                                    .FontColor(Colors.White).SemiBold();

                                row.RelativeColumn().Text($"Contact No: {data[0]!.contactno}")
                                    .FontColor(Colors.White);
                            });

                        content.Item().PaddingTop(10);

                        // --- Ledger Table ---
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                for (int i = 0; i < 10; i++)
                                    columns.RelativeColumn();
                            });

                            // --- Table Header ---
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

                            // --- Table Rows ---
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
                                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
                        });
                    });

                    // --- Footer ---
                    page.Footer().AlignCenter()
                        .Text("Thank you for your business!")
                        .FontColor(Colors.Grey.Medium);
                });
            });

            return document.GeneratePdf();
        }

        #endregion

        #region Balance Due
        public async Task<IEnumerable<BalanceDueDto?>> GetBalanceDueListAsync(int pageNumber, int pageSize, string? searchTerm)
        {
            return await this.accountRepo.GetBalanceDueListAsync(pageNumber, pageSize, searchTerm);
        }
        #endregion
    }

}
