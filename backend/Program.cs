using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using HotelChannelManager.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddNewtonsoftJson(opt =>
    {
        // ╔══════════════════════════════════════════════════════════════════╗
        // ║  ROOT CAUSE FIX — THE ONLY REASON LOGIN WAS FAILING             ║
        // ║                                                                  ║
        // ║  Newtonsoft.Json defaults to PascalCase serialization:           ║
        // ║    { "Success": true, "Data": { "Token": "...", ... } }          ║
        // ║                                                                  ║
        // ║  The JavaScript frontend expects camelCase:                      ║
        // ║    d.success, d.data.token, d.data.fullName, d.data.role        ║
        // ║                                                                  ║
        // ║  Because "Success" !== "success", d.success was always           ║
        // ║  undefined (falsy), so the if(d.success) block was NEVER        ║
        // ║  entered — login always showed "Invalid credentials" even        ║
        // ║  when the API returned HTTP 200 with a valid token.              ║
        // ║                                                                  ║
        // ║  Fix: Add CamelCasePropertyNamesContractResolver so JSON         ║
        // ║  outputs { "success": true, "data": { "token": "..." } }        ║
        // ╚══════════════════════════════════════════════════════════════════╝
        opt.SerializerSettings.ContractResolver =
            new CamelCasePropertyNamesContractResolver();

        opt.SerializerSettings.DateFormatString = "yyyy-MM-dd'T'HH:mm:ss";
        opt.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<AuthService>();

// ── JWT Authentication ─────────────────────────────────────────────────────────
// .NET automatically maps env vars with __ separator to config sections.
// e.g. Railway env var  Jwt__Key  overrides  appsettings.json "Jwt": { "Key": ... }
// We read the final resolved value here and log it at startup so mismatches are
// immediately visible in Railway logs (Settings → Logs).
var jwtKey      = builder.Configuration["Jwt:Key"]      ?? throw new InvalidOperationException("Jwt:Key is not configured. Set the Jwt__Key environment variable in Railway.");
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "HotelChannelManager";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HotelChannelManagerClients";

Console.WriteLine($"🔑 JWT Issuer   : {jwtIssuer}");
Console.WriteLine($"🔑 JWT Audience : {jwtAudience}");
Console.WriteLine($"🔑 JWT Key hash : {jwtKey.GetHashCode()} (first 4 chars: {jwtKey[..Math.Min(4, jwtKey.Length)]}***)");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        opt.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Authentication required. Please login.\",\"errorCode\":\"UNAUTHORIZED\"}");
            },
            OnForbidden = ctx =>
            {
                ctx.Response.StatusCode = 403;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"You do not have permission to access this resource.\",\"errorCode\":\"FORBIDDEN\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
// AllowAnyOrigin is required — do NOT combine with AllowCredentials().
builder.Services.AddCors(opt =>
    opt.AddPolicy("HotelCors", p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Channel Manager API",
        Version = "v1",
        Description = "Complete Hotel PMS & Channel Manager REST API"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization: Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────────
// ORDER: Swagger → StaticFiles → CORS → Authentication → Authorization → Controllers
// Static files are served from wwwroot/ (frontend HTML lives there).
// API routes (/api/...) are matched AFTER static files so they are never intercepted.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Channel Manager API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Hotel Channel Manager API";
});

// Serve frontend files from wwwroot/ on the same port as the API.
// UseDefaultFiles must come before UseStaticFiles so "/" resolves to index.html.
app.UseDefaultFiles();    // "/" → wwwroot/index.html
app.UseStaticFiles();     // serves admin.html, index.html, any CSS/JS/images

app.UseCors("HotelCors");      // CORS before Auth
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Health endpoint ────────────────────────────────────────────────────────────
app.MapGet("/health", async (DatabaseService db) =>
{
    var dbStatus = "unknown";
    try { dbStatus = await db.TestConnection() ? "connected" : "error"; }
    catch (Exception ex) { dbStatus = $"error: {ex.Message}"; }

    return Results.Ok(new
    {
        status = "healthy",
        service = "Hotel Channel Manager API",
        version = "1.0.0",
        database = dbStatus,
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/", () => Results.Redirect("/swagger"));

// ── Startup output ─────────────────────────────────────────────────────────────
Console.WriteLine("");
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       Hotel Channel Manager  — Running               ║");
Console.WriteLine("║  Frontend : http://localhost:5000/                   ║");
Console.WriteLine("║  Admin    : http://localhost:5000/admin.html         ║");
Console.WriteLine("║  Swagger  : http://localhost:5000/swagger            ║");
Console.WriteLine("║  Health   : http://localhost:5000/health             ║");
Console.WriteLine("║  API      : http://localhost:5000/api                ║");
Console.WriteLine("║  Login    : admin / Admin@2024                       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine("");

// Test DB on startup — shows immediately in console if MySQL is unreachable
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    try
    {
        var ok = await db.TestConnection();
        Console.WriteLine(ok
            ? "✅ MySQL connection: OK"
            : "❌ MySQL connection: FAILED — check appsettings.json");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ MySQL connection ERROR: {ex.Message}");
        Console.WriteLine("   Check: MySQL running? Credentials correct? DB created?");
    }
}
Console.WriteLine("");

// Railway injects PORT env var — must listen on it, not hardcoded 5000
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
