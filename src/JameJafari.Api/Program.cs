using System.Text;
using System.Text.Json.Serialization;
using JameJafari.Api.Filters;
using JameJafari.Api.Services;
using JameJafari.Core.Options;
using JameJafari.Infrastructure;
using JameJafari.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<PermissionFilter>();
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<BaleOptions>(options =>
{
    builder.Configuration.GetSection(BaleOptions.SectionName).Bind(options);
    options.BotToken = builder.Configuration["BALE_BOT_TOKEN"]
        ?? builder.Configuration["Bale:BotToken"]
        ?? "";
    options.BotUsername = builder.Configuration["BALE_BOT_USERNAME"]
        ?? builder.Configuration["Bale:BotUsername"];
    options.WebhookSecret = builder.Configuration["BALE_WEBHOOK_SECRET"]
        ?? builder.Configuration["Bale:WebhookSecret"];
    options.UploadsRootPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
});
builder.Services.AddScoped<ImageProcessingService>();
builder.Services.AddScoped<FileStorageService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is required. Set Jwt__Key environment variable or appsettings.Development.json for local dev.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "uploads"));

await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});

app.Run();
