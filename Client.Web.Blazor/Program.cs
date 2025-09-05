using Client.Web.Blazor.Extensions;
using Client.Web.Blazor.Services;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

#region Logging
builder.Logging.ClearProviders();
builder.Logging
    .AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff UTC ";
        options.UseUtcTimestamp = true;
        options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
        options.IncludeScopes = false;
    })
    .AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
#endregion

#region Registration
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddOptions(builder.Configuration);
builder.Services.AddHttpServices(builder.Configuration);
#endregion

#region Data Protection
var keysFolder = new DirectoryInfo("/app/DataProtection-Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keysFolder)
    .SetApplicationName("FlashTalkMessager");
#endregion

#region Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(444, listenOptions => listenOptions.UseHttps("/https/server.pfx", "flashtalk7000$")); // HTTPs, SSL cert
});
#endregion

var app = builder.Build();

app.UseRouting();
app.UseHttpsRedirection();
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();    
}

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();