using Application.Extentions;
using GatewayApi.Extensions;
using GatewayApi.Hubs;
using GatewayApi.Middleware;
using Infrastructure.Services.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.OpenApi.Models;
using Prometheus;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

#region Logging
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff UTC ";
    options.UseUtcTimestamp = true;
    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
    options.IncludeScopes = false;
});
#endregion

#region CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder =>
    {
        policyBuilder.WithOrigins("https://localhost:444", "https://192.168.27.5:444") // addresses of the client application
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .AllowCredentials();
    });
});
#endregion

#region HSTS (HTTP Strict Transport Security) configuration
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
    //options.ExcludedHosts.Add("example.com");
    //options.ExcludedHosts.Add("www.example.com");
});
#endregion

// DataContext registration
builder.Services.AddDataContext(builder.Configuration);

#region Registration
builder.Services.AddControllers();
builder.Services.AddValidators(); // FluentValidation registration
builder.Services.AddOptions(builder.Configuration); // Options registration
builder.Services.AddInfrastructureServices(); // Infrastructure services registration
builder.Services.AddJwtAuthenticationOptions(builder.Configuration); // JWT authentication options registration
builder.Services.AddJwtAuthentication(builder.Configuration); // JWT authentication registration
builder.Services.AddEndpointsApiExplorer(); // Swagger/OpenAPI
builder.Services.AddSwaggerGen(); // SwaggerGen
builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; }); // SignalR registration
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
    options.ListenAnyIP(443, listenOptions => listenOptions.UseHttps("/https/server.pfx", "flash7000$")); // HTTPs, SSL cert
});
#endregion

var app = builder.Build();

#region Migration
if (args.Length > 0 && args[0].Equals("migrate", StringComparison.InvariantCultureIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    await migrationService.MigrateDatabaseAsync(cts.Token);

    return; // Application will exit after migration
}
#endregion

#region Middleware
app.UseMiddleware<ApiExceptionHandler>(); // Custom exception handling middleware
app.UseHttpMetrics(); // Prometheus
#endregion

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
//app.UseStatusCodePages(); 

if (app.Environment.IsDevelopment())
{
    //app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")); // redirect from "/" to "/swagger"
}
else
{
    //app.UseExceptionHandler("/Error");
    app.UseHsts(); // turning on HSTS (HTTP Strict Transport Security) header to inform browsers that the site should only be accessed over HTTPS
}

app.UseAuthentication();
app.UseAuthorization();

// Register the SignalR hub route
app.MapHub<ChatHub>("/chatHub"); 
app.MapMetrics(); // Prometheus metrics
app.MapControllers();

await app.RunAsync();