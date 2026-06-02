using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using MapboxMegaservicios.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ¡¡¡MOSTRAR TODOS LOS ERRORES!!!
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
Environment.SetEnvironmentVariable("ASPNETCORE_DETAILEDERRORS", "true");

// 1. DB CONTEXT
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.UseNetTopologySuite()
    ));

// 2. CONTROLLERS
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. SWAGGER
builder.Services.AddSwaggerGen();

// 4. SECRET KEY
var secretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? "HolaBolaCarambolaHastaLlegarALos32Caracteres";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

Console.WriteLine($"🔐 Clave: {secretKey.Length} chars, Key length: {key.KeySize} bits");

// 5. AUTHENTICATION CON HANDLER CUSTOM
builder.Services.AddAuthentication("CustomJwt")
    .AddScheme<AuthenticationSchemeOptions, CustomJwtAuthenticationHandler>("CustomJwt", null);

// 6. AUTHORIZATION
// 6. AUTHORIZATION - BUSCAR EN TODOS LOS FORMATOS
builder.Services.AddAuthorization(options =>
{
    // AdminOnly - buscar "Administrador" en TODOS los claims posibles
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var user = context.User;
            Console.WriteLine($"🔍 Verificando AdminOnly para: {user.Identity?.Name}");

            // Listar TODOS los claims
            Console.WriteLine("   Claims del usuario:");
            foreach (var claim in user.Claims)
            {
                Console.WriteLine($"     {claim.Type} = {claim.Value}");
            }

            // Buscar "Administrador" en CUALQUIER claim
            var hasAdmin = user.Claims.Any(c =>
                c.Value == "Administrador" &&
                (c.Type.EndsWith("role") || c.Type.Contains("role")));

            Console.WriteLine($"   ¿Tiene 'Administrador'? {hasAdmin}");

            // También verificar si es el usuario admin por nombre
            var isAdminUser = user.Identity?.Name == "admin";
            Console.WriteLine($"   ¿Es usuario 'admin'? {isAdminUser}");

            return hasAdmin || isAdminUser;
        });
    });

    // EmpleadoOnly - cualquier usuario autenticado
    options.AddPolicy("EmpleadoOnly", policy =>
        policy.RequireAuthenticatedUser());

    // Política por defecto: requerir autenticación
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// 7. CORS
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

// CONFIGURAR PIPELINE
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

// DEBUG MIDDLEWARE
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        Console.WriteLine($"\n=== REQUEST: {context.Request.Method} {context.Request.Path}");

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            Console.WriteLine($"🔑 Auth Header presente");

            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring(7);
                Console.WriteLine($"📦 Token length: {token.Length}");

                // Intentar validar MANUALMENTE
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    Console.WriteLine($"✅ Token leído manualmente:");
                    Console.WriteLine($"   Algorithm: {jwtToken.Header.Alg}");
                    Console.WriteLine($"   Type: {jwtToken.Header.Typ}");
                    Console.WriteLine($"   Claims: {jwtToken.Claims.Count()}");

                    foreach (var claim in jwtToken.Claims)
                    {
                        Console.WriteLine($"     {claim.Type} = {claim.Value}");
                    }

                    // Intentar validar
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    var principal = handler.ValidateToken(token, validationParameters, out _);
                    Console.WriteLine($"✅✅ Token VÁLIDO según validación manual");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error leyendo/validando token manualmente:");
                    Console.WriteLine($"   {ex.GetType().Name}: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine($"⚠️  SIN Auth Header");
        }
    }

    await next();
});

// BD
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"Ubicaciones\" ADD COLUMN IF NOT EXISTS \"IsPossibleSpoofing\" boolean NOT NULL DEFAULT false;"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ No se pudo alterar la tabla Ubicaciones: {ex.Message}");
    }
    await SeedData.Inicializar(dbContext);
    Console.WriteLine("✅ BD inicializada!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error BD: {ex.Message}");
}

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine("🚀 API INICIADA: http://localhost:5001");
Console.WriteLine("📚 Swagger: http://localhost:5001/swagger");
Console.WriteLine("🔐 Authentication: CustomJwt (bypass)");
Console.WriteLine(new string('=', 50) + "\n");

app.Run("http://localhost:5001");

// ============ CLASE CUSTOM JWT HANDLER ============
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
        var secretKey = configuration["JwtSettings:SecretKey"]
            ?? "HolaBolaCarambolaHastaLlegarALos32Caracteres";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Console.WriteLine($"\n🎯 CustomJwtAuthenticationHandler: {Request.Path}");

        // 1. Obtener token del header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            Console.WriteLine("   ⚠️  No Authorization header");
            return AuthenticateResult.NoResult();
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        Console.WriteLine($"   📨 Auth Header: {authHeader.Substring(0, Math.Min(50, authHeader.Length))}...");

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("   ❌ No es Bearer token");
            return AuthenticateResult.Fail("Invalid Authorization header format");
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        Console.WriteLine($"   📦 Token length: {token.Length}");

        try
        {
            // 2. LEER token sin validar primero
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                Console.WriteLine($"   ❌ Cannot read token");
                return AuthenticateResult.Fail("Cannot read token");
            }

            // 3. Parsear token (solo leer)
            var jwtToken = handler.ReadJwtToken(token);
            Console.WriteLine($"   ✅ Token parsed:");
            Console.WriteLine($"      Algorithm: {jwtToken.Header.Alg}");
            Console.WriteLine($"      Type: {jwtToken.Header.Typ}");
            Console.WriteLine($"      Claims: {jwtToken.Claims.Count()}");

            // 4. Validar MANUALMENTE
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

            Console.WriteLine($"   ✅✅ Token VÁLIDO!");
            Console.WriteLine($"      User: {principal.Identity?.Name}");
            foreach (var claim in principal.Claims)
            {
                Console.WriteLine($"      Claim: {claim.Type} = {claim.Value}");
            }

            // 5. Crear ticket de autenticación
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌❌ ERROR en autenticación:");
            Console.WriteLine($"      Type: {ex.GetType().FullName}");
            Console.WriteLine($"      Message: {ex.Message}");
            Console.WriteLine($"      StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"      Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }

            return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
        }
    }
}