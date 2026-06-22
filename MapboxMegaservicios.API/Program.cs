using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. DB CONTEXT
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()
    ));

// 2. CONTROLLERS + SWAGGER + BACKGROUND SERVICES
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<DataCleanupService>();

// 3. JWT SECRET KEY (from env var > config > fallback)
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? "HolaBolaCarambolaHastaLlegarALos32Caracteres";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

// 4. CUSTOM JWT AUTH
builder.Services.AddAuthentication("CustomJwt")
    .AddScheme<AuthenticationSchemeOptions, CustomJwtAuthenticationHandler>("CustomJwt", null);

// 5. AUTHORIZATION POLICIES
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var user = context.User;
            var hasAdmin = user.Claims.Any(c =>
                c.Value == "Administrador" &&
                (c.Type.EndsWith("role") || c.Type.Contains("role")));
            var isAdminUser = user.Identity?.Name == "admin";
            return hasAdmin || isAdminUser;
        });
    });

    options.AddPolicy("EmpleadoOnly", policy =>
        policy.RequireAuthenticatedUser());

    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// DB MIGRATIONS + SEED
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedData.Inicializar(dbContext);
    Console.WriteLine("BD inicializada correctamente");
}
catch (Exception ex)
{
    Console.WriteLine($"Error BD: {ex.Message}");
}

Console.WriteLine($"API iniciada en {app.Urls.FirstOrDefault() ?? "http://localhost:5001"}");

app.Run();

// ============ CUSTOM JWT HANDLER ============
public class CustomJwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly SymmetricSecurityKey _key;

    public CustomJwtAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? configuration["JwtSettings:SecretKey"]
            ?? "HolaBolaCarambolaHastaLlegarALos32Caracteres";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.NoResult();

        var authHeader = Request.Headers["Authorization"].ToString();

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Invalid Authorization header format");

        var token = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                return AuthenticateResult.Fail("Cannot read token");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = "role"
            };

            var principal = handler.ValidateToken(token, validationParameters, out _);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }
}
