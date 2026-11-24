using Microsoft.EntityFrameworkCore;
using Calculator16.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение к БД
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CalculatorContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(10, 5, 15))));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 2. Ожидание базы данных и создание таблиц
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalculatorContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    int retries = 0;
    while (retries < 15)
    {
        try
        {
            logger.LogInformation("Попытка подключения к базе данных...");
            db.Database.EnsureCreated(); // Создает БД и таблицы, если их нет
            logger.LogInformation("База данных готова!");
            break;
        }
        catch (Exception ex)
        {
            retries++;
            logger.LogWarning($"База данных еще не готова (попытка {retries}/15): {ex.Message}");
            Thread.Sleep(3000); // Ждем 3 секунды
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Calculator}/{action=Index}/{id?}");

app.Run();
