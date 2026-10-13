using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management system (PROG6212 PoE Part 2). " +
                      "Start with POST /api/auth/register, then POST /api/auth/login - the server creates a session " +
                      "(storing UserId and Role) and Swagger sends the session cookie automatically on every later call. " +
                      "Enum fields are numeric: role 0 = Organiser, 1 = Participant; eventType 0 = Run, 1 = Walk, 2 = Cycle."
    });

    foreach (var xmlFile in new[] { $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml", "RaceDay.Contracts.xml" })
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    }
});

builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Server-side session: UserId and Role are stored in server memory at login.
// The browser only holds an opaque session-id cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDaySession";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromHours(4);
});

builder.Services.AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        SessionAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

// NEW for Part 3: the MVC app runs on a different origin, so its browser-side
// requests to this API (if any client-side calls are added later) need CORS.
// Server-to-server calls from the MVC app's ApiClient don't strictly need this,
// but it's included for safety and for direct API testing from a browser.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", policy =>
    {
        policy.WithOrigins(builder.Configuration["MvcAppUrl"] ?? "https://localhost:7100")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
});

// Every controller is attribute-routed under "api/...", so nothing is mapped
// to the root path and hitting "/" returns 404. Send the root to Swagger
// instead, so the API opens on a usable page regardless of launch profile.
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.UseHttpsRedirection();

app.UseCors("AllowMvcApp");

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
