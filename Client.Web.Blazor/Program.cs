using Client.Web.Blazor.Services;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Shared.Extensions;
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
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddJwtAuthentication(builder.Configuration); // Register JWT authentication from Shared.Extensions
builder.Services
    .AddHttpClient<IApiClientService, ApiClientService>(client => client.BaseAddress = new Uri("https://flashtalk_api:443/")) // Add HttpClient for API calls (Transient lifetime - created for each request)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator }); // Отключить проверку сертификата (только для разработки!)
builder.Services.AddScoped<IChatSignalServiceClient, ChatSignalServiceClient>();
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
    options.ListenAnyIP(444, listenOptions => listenOptions.UseHttps("/https/server.pfx", "flash7000$")); // HTTPs, SSL cert
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

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();