using Hangfire.Logging;
using MDP.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Quartz;
using System.Data;
using System.Diagnostics;
using Web0524.Models;
using Microsoft.Extensions.DependencyInjection;
using Web0524.Models.Helper;
using Web0524.Models.Marketing;
using Web0524.Models.LineMessage;





var builder = WebApplication.CreateBuilder(args);




builder.AddMdp(); // 掛載MDP
//builder.WebHost.UseUrls("http://0.0.0.0:5000");


builder.Services.AddAntiforgery(options =>
{

});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    
});


builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/ProductList"); // 這將對全站的頁面添加 Authorize 屬性。
    //指定不用授權就可使用的頁面
    options.Conventions.AllowAnonymousToFolder("/Account");
});

builder.Services.AddScoped<IGoogleCalendarHelper>(provider =>
{
    var path = Path.Combine("wwwroot", "credentials", "gen-lang-client-0206008601-176e3adc3481.json");
    var calendarId = "29f882b8d6108305c9a9064e11d25577a699b83939cc49a477bbb32558306a6d@group.calendar.google.com"; // 你的日曆 ID
    return new GoogleCalendarHelper(path, calendarId);
});

var configuration = builder.Configuration;



builder.Services.AddSession();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IDbConnection>(sp => new SqlConnection(configuration.GetConnectionString("WebDB")));
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddHostedService<CouponDispatchBackgroundService>(); // 背景排程

builder.Services.AddSingleton<Web0524.Models.My>();


builder.Services.AddHttpClient<LineMessageService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPgroupService, PgroupService>();
builder.Services.AddScoped<IYearReportService, YearReportService>();
builder.Services.AddScoped<IMyService,  MyService>();
builder.Services.AddScoped<INewService, NewListService>();
builder.Services.AddScoped<IMarketingService, MarketingService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPortfolioGroupService, PortfolioGroupService>();
builder.Services.AddHostedService<DailyOrderCompletionService>();
builder.Services.AddScoped<IReportService, ReportService>();


builder.Services.AddHostedService<CouponDispatchBackgroundService>();

builder.Services.AddDistributedMemoryCache();
//builder.Services.AddControllers();
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);


builder.Services.AddQuartz(quartz =>
{
    quartz.UseMicrosoftDependencyInjectionJobFactory();

    //// 建立 Job
    //var jobKey = new JobKey("AutoNotify", "AutoNotifyGroup");
    //quartz.AddJob<AutoNotify>(opts =>
    //{
    //    opts.WithIdentity(jobKey);
    //    opts.StoreDurably();
    //});

    //// 建立觸發器，自動執行 Job
    //quartz.AddTrigger(opts =>
    //{
    //    opts.ForJob(jobKey);
    //    opts.WithIdentity("AutoNotifyTrigger", "AutoNotifyGroup");
    //    //opts.WithCronSchedule("0 30 00 * * ?");
    //    opts.WithCronSchedule("0 20 22 * * ?");
    //    //opts.WithSimpleSchedule(x => x.WithIntervalInSeconds(100).RepeatForever());
    //});
});




var app = builder.Build();
//app.UseRouting();

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//});
// Configure the HTTP request pipeline.
//Configure 新增


app.MapDefaultControllerRoute();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}



app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication(); // 確保在 UseAuthorization 之前調用
app.UseAuthorization();

app.MapRazorPages();

app.Run();
