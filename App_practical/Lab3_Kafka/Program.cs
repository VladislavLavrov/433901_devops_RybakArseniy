using Microsoft.EntityFrameworkCore;
using Calculator16.Models;
using Calculator16.Services;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

string mariadbCS = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection is missing");

builder.Services.AddDbContext<CalculatorContext>(options =>
{
    options.UseMySql(
        mariadbCS,
        new MariaDbServerVersion(new Version(10, 5, 15)));
});

builder.Services.AddRazorPages();

builder.Services.AddHttpClient();

builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddSingleton<KafkaProducerHandler>();

builder.Services.AddSingleton<KafkaProducerService<Null, string>>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Calculator}/{action=Index}/{id?}");

app.Run();
