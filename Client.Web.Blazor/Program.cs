using Client.Web.Blazor.Services;
using Client.Web.Blazor.Services.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Extensions;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddJwtAuthentication(builder.Configuration); // Register JWT authentication from Shared.Extensions

// Add HttpClient for API calls
builder.Services.AddHttpClient<IApiClientService, ApiClientService>(client => client.BaseAddress = new Uri("http://flashtalk_api:8080/"));

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