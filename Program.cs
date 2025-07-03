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
using Web0524.Models.SystemMessage;
using MySql.Data.MySqlClient; // ← 一定要用這個命名空間！





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
//builder.Services.AddScoped<IDbConnection>(sp => new SqlConnection(configuration.GetConnectionString("WebDB")));
builder.Services.AddScoped<IDbConnection>(sp =>
    new MySqlConnection(configuration.GetConnectionString("WebDB")));

builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddHostedService<CouponDispatchBackgroundService>(); // 背景排程

builder.Services.AddSingleton<Web0524.Models.MyData>();

builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddHttpClient<LineMessageService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPgroupService, PgroupService>();
builder.Services.AddScoped<IYearReportService, YearReportService>();

builder.Services.AddScoped<INewService, NewListService>();
builder.Services.AddScoped<IMarketingService, MarketingService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IPortfolioGroupService, PortfolioGroupService>();
builder.Services.AddHostedService<DailyOrderCompletionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IMyService, MyService>(); // ✅ 正確


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

// 初始化靜態 My 資料（來自資料庫）
using (var scope = app.Services.CreateScope())
{
    var myService = scope.ServiceProvider.GetRequiredService<IMyService>();
    var baseData = myService.GetBaseData();

    if (baseData != null)
    {
        My.Id = baseData.Id;
        My.Name = baseData.Name;
        My.Name_short = baseData.Name_short;
        My.Phone = baseData.Phone;
        My.Email = baseData.Email;
        My.Line = baseData.Line;
        My.WebURL = baseData.WebURL;
        My.LineBotURL = baseData.LineBotURL;
        My.Fb_Url = baseData.Fb_Url;
        My.Ig_Url = baseData.Ig_Url;
        My.Yt_Url = baseData.Yt_Url;
        My.Tk_Url = baseData.Tk_Url;
        My.Line_Url = baseData.Line_Url;

        My.CreateOrderSandLineMsgSw = baseData.CreateOrderSandLineMsgSw;
        My.CancelSandLineMsgSw = baseData.CancelSandLineMsgSw;

        My.Max_Order_Oneday = baseData.Max_Order_Oneday;
        My.Max_Reg_Oneday = baseData.Max_Reg_Oneday;
        My.CancelLimitHours = baseData.CancelLimitHours;

        My.Msg_BindOk = baseData.Msg_BindOk;

        // About 相關屬性（攤平設定）
        My.PageTitle = baseData.PageTitle;
        My.HeroTitle = baseData.HeroTitle;

        My.Section1_Title = baseData.Section1_Title;
        My.Section1_Paragraph1 = baseData.Section1_Paragraph1;
        My.Section1_Paragraph2 = baseData.Section1_Paragraph2;

        My.Section2_Title = baseData.Section2_Title;
        My.Section2_Paragraph1 = baseData.Section2_Paragraph1;
        My.Section2_Paragraph2 = baseData.Section2_Paragraph2;

        My.Section3_Title = baseData.Section3_Title;
        My.Section3_Item1 = baseData.Section3_Item1;
        My.Section3_Item2 = baseData.Section3_Item2;
        My.Section3_Item3 = baseData.Section3_Item3;
        My.Section3_Item4 = baseData.Section3_Item4;

        My.OpenTime = baseData.OpenTime;
        My.CloseTime = baseData.CloseTime;
    }
}

app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseAuthentication(); // 確保在 UseAuthorization 之前調用
app.UseAuthorization();

app.MapRazorPages();

app.Run();
Console.WriteLine("連線字串：" + configuration.GetConnectionString("WebDB"));
