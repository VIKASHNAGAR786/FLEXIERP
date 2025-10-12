using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//quest pdf use
QuestPDF.Settings.License = LicenseType.Community;

// Services DI
builder.Services.AddTransient<IDataBaseOperation, DataBaseOperation>();
builder.Services.AddScoped<IAccountServices, AccountService>();
builder.Services.AddScoped<IAccountRepo, AccountRepo>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IInventoryRepo, InventoryRepo>();
builder.Services.AddScoped<ISaleRepo, SaleRepo>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddScoped<ICommonMasterRepo, CommonMasterRepo>();
builder.Services.AddScoped<ICommonMasterService, CommonMasterService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddHttpContextAccessor();



// ✅ GLOBAL CORS ALLOW ALL - for dev use
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true) // allow any origin (useful for Tauri)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ✅ JWT Authentication setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"])
        ),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
});


var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ✅ Use CORS before auth
app.UseCors("AllowAll");

// ✅ Enable Auth
app.UseAuthentication();
app.UseAuthorization();

// Static files
app.UseStaticFiles();

var Documents = Path.Combine(Directory.GetCurrentDirectory(), "Documents");
if (!Directory.Exists(Documents))
    Directory.CreateDirectory(Documents);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Documents),
    RequestPath = "/Documents"
});

app.MapControllers();

app.Run();



//dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true
//publish path -  C:\Users\VIKAS NAGAR\source\repos\FLEXIERP\bin\Release\net8.0\win-x64\publish