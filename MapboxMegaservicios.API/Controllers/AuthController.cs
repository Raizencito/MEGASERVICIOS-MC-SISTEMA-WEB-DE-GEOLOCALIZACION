using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.DTOs;
using MapboxMegaservicios.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace MapboxMegaservicios.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login para: {Usuario}", request.Usuario);

                // 1. Buscar usuario
                var empleado = await _context.Empleados
                    .Include(e => e.Rol)
                    .FirstOrDefaultAsync(e => e.Usuario == request.Usuario && e.Activo);

                if (empleado == null)
                {
                    return Unauthorized(new AuthResult
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    });
                }

                // 2. Verificar contraseña
                if (!VerifyPassword(request.Password, empleado.PasswordHash))
                {
                    return Unauthorized(new AuthResult
                    {
                        Success = false,
                        Message = "Contraseña incorrecta"
                    });
                }

                // 3. Generar token
                var token = GenerateJwtToken(empleado);

                // 4. Crear DTO de empleado
                var empleadoDTO = new EmpleadoDTO
                {
                    Id = empleado.Id,
                    NombreCompleto = $"{empleado.Nombres} {empleado.Paterno}",
                    Usuario = empleado.Usuario,
                    Rol = empleado.Rol?.Nombre ?? "Sin rol",
                    LugarActual = "No asignado"
                };

                _logger.LogInformation("Login exitoso para: {Usuario}", request.Usuario);

                // 5. Retornar respuesta
                return Ok(new AuthResult
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = token,
                    Empleado = empleadoDTO
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, new AuthResult
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Sesión cerrada" });
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<ActionResult<EmpleadoDTO>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int empleadoId))
                {
                    return Unauthorized();
                }

                var empleado = await _context.Empleados
                    .Include(e => e.Rol)
                    .AsNoTracking()
                    .Include(e => e.LugarTrabajoActual)
                    .FirstOrDefaultAsync(e => e.Id == empleadoId && e.Activo);

                if (empleado == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                return Ok(new EmpleadoDTO
                {
                    Id = empleado.Id,
                    NombreCompleto = $"{empleado.Nombres} {empleado.Paterno}",
                    Usuario = empleado.Usuario,
                    Rol = empleado.Rol?.Nombre ?? "Sin rol",
                    LugarActual = empleado.LugarTrabajoActual?.Nombre ?? "No asignado",
                    Telefono = empleado.Telefono,
                    Ci = empleado.Ci
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario actual");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        private string GenerateJwtToken(Empleado empleado)
        {
            try
            {
                var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                    ?? _configuration["JwtSettings:SecretKey"]
                    ?? "HolaBolaCarambolaHastaLlegarALos32Caracteres";

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, empleado.Id.ToString()),
                    new Claim(ClaimTypes.Name, empleado.Usuario),
                    new Claim("role", empleado.Usuario == "admin" ? "Administrador" : "Empleado")
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(8),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando JWT token");
                throw;
            }
        }

        [HttpGet("check")]
        [Authorize]
        public IActionResult HealthCheck()
        {
            return Ok(new { message = "API funcionando", user = User.Identity?.Name });
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var hash = HashPassword(password);
                return hash == storedHash;
            }
            catch
            {
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public class TokenStorageRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}