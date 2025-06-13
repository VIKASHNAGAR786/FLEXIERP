using FLEXIERP.BusinesLayer_Interfaces;
using FLEXIERP.BusinessLayer;
using FLEXIERP.DataAccessLayer;
using FLEXIERP.DataAccessLayer_Interfaces;
using FLEXIERP.DATABASE;

var builder = WebApplication.CreateBuilder(args);

//builder.Configuration
//                .AddJsonFile("appsettings.json", optional: true)
//                .AddUserSecrets<Program>()  // 🔐 This line enables user secrets
//                .AddEnvironmentVariables();


//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));
builder.Services.AddTransient<IDataBaseOperation, DataBaseOperation>();
builder.Services.AddScoped<IAccountServices, AccountService>();
builder.Services.AddScoped<IAccountRepo, AccountRepo>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
