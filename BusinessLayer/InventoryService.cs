using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.MODELS;
using SelectPdf;

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

        #endregion

       
    }
}
