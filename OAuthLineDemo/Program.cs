
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OAuthLibrary.LineLoginService;
using OAuthLibrary.LineNotifyService;
using OAuthLineDemo.Data;
using System.Data;


var builder = WebApplication.CreateBuilder(args);

// 設定 appsettings.json 檔案的路徑
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// 註冊資料庫連線
//Scoped：注入的物件在同一Request中，參考的都是相同物件(你在Controller、View中注入的IDbConnection指向相同參考)
builder.Services.AddScoped<IDbConnection, SqlConnection>(serviceProvider => {
    SqlConnection conn = new SqlConnection();
    //指派連線字串
    conn.ConnectionString = builder.Configuration.GetConnectionString(builder.Configuration["DB:RunDB"]);
    return conn;
});
builder.Services.AddTransient<LineLoginService>();
builder.Services.AddTransient<LineNotifyService>();



// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
