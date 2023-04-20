
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OAuthLibrary.LineLoginService;
using OAuthLibrary.LineNotifyService;
using OAuthLineDemo.Data;
using System.Data;


var builder = WebApplication.CreateBuilder(args);

// �]�w appsettings.json �ɮת����|
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// ���U��Ʈw�s�u
//Scoped�G�`�J������b�P�@Request���A�ѦҪ����O�ۦP����(�A�bController�BView���`�J��IDbConnection���V�ۦP�Ѧ�)
builder.Services.AddScoped<IDbConnection, SqlConnection>(serviceProvider => {
    SqlConnection conn = new SqlConnection();
    //�����s�u�r��
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
