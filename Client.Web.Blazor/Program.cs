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
builder.Services.AddHttpClient<IApiClientService, ApiClientService>(client => client.BaseAddress = new Uri("http://flashtalk_api:8080/")); // Add HttpClient for API calls (Transient lifetime - created for each request)
builder.Services.AddScoped<IChatSignalServiceClient, ChatSignalServiceClient>();
#endregion

#region Data Protection
var keysFolder = new DirectoryInfo("/app/DataProtection-Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keysFolder)
    .SetApplicationName("FlashTalkMessager");
#endregion

builder.WebHost.ConfigureKestrel(opts =>
{
    opts.ListenAnyIP(5000); // Listen on port 8080
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();