using System.Security.Cryptography;
using System.Text;
using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.DTOs;
using MapboxMegaservicios.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static MapboxMegaservicios.API.DTOs.EmpleadoDTO;

namespace MapboxMegaservicios.API.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/admin/[controller]")]
    public class EmpleadosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmpleadosController> _logger;

        public EmpleadosController(ApplicationDbContext context, ILogger<EmpleadosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<EmpleadoDTO>>> ObtenerTodos()
        {
            try
            {
                var empleados = await _context.Empleados
                    .Where(e => e.Activo)
                    .Include(e => e.LugarTrabajoActual)
                    .Include(e => e.Rol)
                    .Select(e => new EmpleadoDTO
                    {
                        Id = e.Id,
                        NombreCompleto = $"{e.Nombres} {e.Paterno} {e.Materno}".Trim(),
                        Paterno = e.Paterno,
                        Materno = e.Materno,
                        Nombres = e.Nombres,
                        Ci = e.Ci,
                        Usuario = e.Usuario,
                        Telefono = e.Telefono,
                        Rol = e.Rol.Nombre,
                        LugarActual = e.LugarTrabajoActual != null ? e.LugarTrabajoActual.Nombre : "Sin asignar",
                        IdLugarTrabajo = e.LugarTrabajoActualId,
                        IdRol = e.IdRol,
                        Activo = e.Activo,
                        FechaCreacion = e.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleados");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmpleadoDTO>> ObtenerPorId(int id)
        {
            try
            {
                var empleado = await _context.Empleados
                    .Where(e => e.Id == id && e.Activo)
                    .Include(e => e.LugarTrabajoActual)
                    .Include(e => e.Rol)
                    .Select(e => new EmpleadoDTO
                    {
                        Id = e.Id,
                        NombreCompleto = $"{e.Nombres} {e.Paterno} {e.Materno}".Trim(),
                        Paterno = e.Paterno,
                        Materno = e.Materno,
                        Nombres = e.Nombres,
                        Ci = e.Ci,
                        Usuario = e.Usuario,
                        Telefono = e.Telefono,
                        Rol = e.Rol.Nombre,
                        LugarActual = e.LugarTrabajoActual != null ? e.LugarTrabajoActual.Nombre : "Sin asignar",
                        IdLugarTrabajo = e.LugarTrabajoActualId,
                        IdRol = e.IdRol,
                        Activo = e.Activo,
                        FechaCreacion = e.FechaCreacion
                    })
                    .FirstOrDefaultAsync();

                if (empleado == null)
                {
                    return NotFound(new { message = "Empleado no encontrado" });
                }

                return Ok(empleado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<EmpleadoCreadoDTO>> Crear([FromBody] CrearEmpleadoRequest request)
        {
            try
            {
                _logger.LogInformation("📝 Intentando crear empleado: {Nombres} {Paterno}",
                    request.Nombres, request.Paterno);

                // 1. VALIDACIONES
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("❌ Validación fallida: {@Errors}",
                        ModelState.Values.SelectMany(v => v.Errors));
                    return BadRequest(new
                    {
                        success = false,
                        message = "Errores de validación",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // 2. VERIFICAR CI ÚNICO
                var ciExistente = await _context.Empleados
                    .AnyAsync(e => e.Ci == request.Ci && e.Activo);

                if (ciExistente)
                {
                    _logger.LogWarning("❌ CI duplicado: {CI}", request.Ci);
                    return BadRequest(new
                    {
                        success = false,
                        message = $"El CI {request.Ci} ya está registrado"
                    });
                }

                // 3. VERIFICAR TELÉFONO ÚNICO
                if (!string.IsNullOrEmpty(request.Telefono))
                {
                    var telefonoExistente = await _context.Empleados
                        .AnyAsync(e => e.Telefono == request.Telefono && e.Activo);

                    if (telefonoExistente)
                    {
                        _logger.LogWarning("❌ Teléfono duplicado: {Telefono}", request.Telefono);
                        return BadRequest(new
                        {
                            success = false,
                            message = $"El teléfono {request.Telefono} ya está registrado"
                        });
                    }
                }

                // 4. VERIFICAR LUGAR DE TRABAJO (si se proporciona)
                if (request.IdLugarTrabajo.HasValue)
                {
                    var lugarExiste = await _context.LugaresTrabajo
                        .AnyAsync(l => l.Id == request.IdLugarTrabajo.Value && l.Activo);

                    if (!lugarExiste)
                    {
                        _logger.LogWarning("❌ Lugar de trabajo no encontrado: {Id}", request.IdLugarTrabajo);
                        return BadRequest(new
                        {
                            success = false,
                            message = $"El lugar de trabajo ID {request.IdLugarTrabajo} no existe"
                        });
                    }
                }

                // 5. VERIFICAR ROL
                var rolExiste = await _context.Roles.AnyAsync(r => r.Id == request.IdRol);
                if (!rolExiste)
                {
                    _logger.LogWarning("❌ Rol no encontrado: {Id}", request.IdRol);
                    return BadRequest(new
                    {
                        success = false,
                        message = $"El rol ID {request.IdRol} no existe"
                    });
                }

                // 6. GENERAR CREDENCIALES
                var (usuario, password) = GenerarCredencialesAutomaticas(request);
                _logger.LogInformation("🔑 Credenciales generadas: Usuario={Usuario}", usuario);

                // 7. CREAR EMPLEADO
                var empleado = new Empleado
                {
                    Paterno = request.Paterno.Trim(),
                    Materno = request.Materno?.Trim() ?? "",
                    Nombres = request.Nombres.Trim(),
                    Ci = request.Ci.Trim(),
                    Telefono = request.Telefono?.Trim() ?? "",
                    Usuario = usuario,
                    PasswordHash = HashPassword(password),
                    IdRol = request.IdRol,
                    LugarTrabajoActualId = request.IdLugarTrabajo,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Empleados.Add(empleado);
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Empleado creado con ID: {Id}", empleado.Id);

                // 8. REGISTRAR EN HISTORIAL (si tiene lugar)
                if (request.IdLugarTrabajo.HasValue)
                {
                    var historial = new HistorialLugarTrabajo
                    {
                        EmpleadoId = empleado.Id,
                        LugarTrabajoId= request.IdLugarTrabajo.Value,
                        FechaCambio = DateTime.UtcNow,
                        Observaciones = "Asignación inicial"
                    };
                    _context.HistorialLugaresTrabajo.Add(historial);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("📋 Historial registrado para empleado {Id}", empleado.Id);
                }

                // 9. RETORNAR RESPUESTA
                return Ok(new EmpleadoCreadoDTO
                {
                    Id = empleado.Id,
                    NombreCompleto = $"{empleado.Nombres} {empleado.Paterno}",
                    Usuario = usuario,
                    Password = password,
                    Telefono = empleado.Telefono,
                    Mensaje = "¡GUARDE ESTAS CREDENCIALES! No se mostrarán nuevamente."
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "❌ Error de base de datos al crear empleado");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error de base de datos",
                    detail = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inesperado al crear empleado");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    detail = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Actualizar(int id, [FromBody] ActualizarEmpleadoRequest request)
        {
            try
            {
                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null || !empleado.Activo)
                    return NotFound(new { message = "Empleado no encontrado" });

                // Si el teléfono NO viene en el request (es null), mantener el actual
                // Si viene vacío (""), también mantener el actual
                if (request.Telefono != null && !string.IsNullOrWhiteSpace(request.Telefono))
                {
                    var telefonoLimpio = request.Telefono.Trim();

                    // Solo validar si es diferente al actual
                    if (telefonoLimpio != empleado.Telefono)
                    {
                        var telefonoExistente = await _context.Empleados
                            .AnyAsync(e => e.Telefono == telefonoLimpio &&
                                          e.Id != id &&
                                          e.Activo);

                        if (telefonoExistente)
                            return BadRequest(new { message = "El teléfono ya está registrado" });

                        empleado.Telefono = telefonoLimpio;
                    }
                }
                // Si request.Telefono es null o vacío, NO cambiamos empleado.Telefono

                // Actualizar otros campos
                empleado.Paterno = request.Paterno.Trim();
                empleado.Materno = request.Materno?.Trim() ?? "";
                empleado.Nombres = request.Nombres.Trim();
                empleado.IdRol = request.IdRol;
                empleado.Activo = request.Activo;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Empleado actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando empleado {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Eliminar(int id)
        {
            if (id == 1) // ID del admin (generalmente el primero)
            {
                return BadRequest(new { message = "No se puede modificar el usuario administrador" });
            }
            try
            {
                var empleado = await _context.Empleados.FindAsync(id);
                if (empleado == null || !empleado.Activo)
                    return NotFound(new { message = "Empleado no encontrado" });

                // Desactivar en lugar de eliminar (soft delete)
                empleado.Activo = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Empleado desactivado: {Id} - {Nombre}", id, empleado.Nombres);
                return Ok(new { message = "Empleado desactivado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando empleado {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPatch("{id}/lugar-trabajo")]
        public async Task<ActionResult> ActualizarLugarTrabajo(int id, [FromBody] SimpleLugarRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Cambiando lugar para empleado {Id}", id);

                // 1. Verificar empleado
                var empleadoExiste = await _context.Empleados
                    .AsNoTracking()
                    .AnyAsync(e => e.Id == id && e.Activo);

                if (!empleadoExiste)
                    return NotFound(new { success = false, message = "Empleado no encontrado" });

                // 2. Verificar lugar si se proporciona
                if (request.LugarTrabajoId.HasValue)
                {
                    var lugarExiste = await _context.LugaresTrabajo
                        .AsNoTracking()
                        .AnyAsync(l => l.Id == request.LugarTrabajoId.Value && l.Activo);

                    if (!lugarExiste)
                        return NotFound(new { success = false, message = "Lugar no encontrado" });
                }

                // 3. LIMPIAR el ChangeTracker (por seguridad)
                _context.ChangeTracker.Clear();

                // 4. Actualizar empleado con ExecuteSqlRaw
                await _context.Database.ExecuteSqlRawAsync(
                    request.LugarTrabajoId.HasValue
                        ? @"UPDATE ""Empleados"" SET ""IdLugarTrabajoActual"" = {0} WHERE ""Id"" = {1}"
                        : @"UPDATE ""Empleados"" SET ""IdLugarTrabajoActual"" = NULL WHERE ""Id"" = {1}",
                    request.LugarTrabajoId.HasValue ? request.LugarTrabajoId.Value : 0,
                    id);

                // 5. Registrar historial (si se proporcionó lugar)
                if (request.LugarTrabajoId.HasValue)
                {
                    // Crear historial SIN inicializar las propiedades de navegación
                    var historial = new HistorialLugarTrabajo
                    {
                        EmpleadoId = id,
                        LugarTrabajoId= request.LugarTrabajoId.Value,
                        FechaCambio = DateTime.UtcNow,
                        Observaciones = request.Observaciones ?? "Cambio de lugar"
                        // NO hacer: Empleado = new() ni LugarTrabajo = new()
                    };

                    _context.HistorialLugaresTrabajo.Add(historial);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("✅ Lugar actualizado para empleado {Id}", id);

                return Ok(new
                {
                    success = true,
                    message = request.LugarTrabajoId.HasValue
                        ? "Lugar actualizado exitosamente"
                        : "Empleado removido del lugar actual"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error actualizando lugar para empleado {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        [HttpGet("debug-context")]
        public ActionResult<string> DebugContextState()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== DEBUG: ESTADO DEL DBCONTEXT ===");

            // Ver entidades en el ChangeTracker
            var entries = _context.ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                info.AppendLine($"Entidad: {entry.Entity.GetType().Name}, Estado: {entry.State}");

                if (entry.Entity is Empleado emp)
                {
                    info.AppendLine($"  -> Empleado ID: {emp.Id}, RolID: {emp.IdRol}, Nombre: {emp.Nombres}");
                }
                else if (entry.Entity is Departamento dep)
                {
                    info.AppendLine($"  -> Departamento ID: {dep.Id}, Nombre: {dep.Nombre}");
                }
            }

            return Ok(info.ToString());
        }

        [HttpGet("{id}/historial-lugares")]
        public async Task<ActionResult<List<HistorialLugarDTO>>> ObtenerHistorialLugares(int id)
        {
            try
            {
                var historial = await _context.HistorialLugaresTrabajo
                    .Where(h => h.EmpleadoId == id)
                    .Include(h => h.LugarTrabajo)
                    .OrderByDescending(h => h.FechaCambio)
                    .Select(h => new HistorialLugarDTO
                    {
                        Id = h.Id,
                        LugarTrabajo = h.LugarTrabajo.Nombre,
                        FechaCambio = h.FechaCambio,
                        Observaciones = h.Observaciones ?? "Sin observaciones"
                    })
                    .ToListAsync();

                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de lugares para empleado {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("buscar")]
        public async Task<ActionResult<List<EmpleadoDTO>>> BuscarEmpleados([FromQuery] string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                    return Ok(new List<EmpleadoDTO>());

                var empleados = await _context.Empleados
                    .Where(e => e.Activo &&
                           (e.Nombres.Contains(termino) ||
                            e.Paterno.Contains(termino) ||
                            e.Ci.Contains(termino) ||
                            e.Usuario.Contains(termino)))
                    .Include(e => e.LugarTrabajoActual)
                    .Include(e => e.Rol)
                    .Take(20)
                    .Select(e => new EmpleadoDTO
                    {
                        Id = e.Id,
                        NombreCompleto = $"{e.Nombres} {e.Paterno} {e.Materno}".Trim(),
                        Paterno = e.Paterno,
                        Materno = e.Materno,
                        Nombres = e.Nombres,
                        Ci = e.Ci,
                        Usuario = e.Usuario,
                        Telefono = e.Telefono,
                        Rol = e.Rol.Nombre,
                        LugarActual = e.LugarTrabajoActual != null ? e.LugarTrabajoActual.Nombre : "Sin asignar",
                        IdLugarTrabajo = e.LugarTrabajoActualId,
                        IdRol = e.IdRol,
                        Activo = e.Activo
                    })
                    .ToListAsync();

                return Ok(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando empleados con término: {Termino}", termino);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPatch("{id}/estadoemp")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult> ToggleActivo(int id)  // ← Sin request
        {
            try
            {
                _logger.LogInformation("🔄 Intentando cambiar estado activo para empleado {Id}", id);

                var empleado = await _context.Empleados.FindAsync(id);

                if (empleado == null)
                {
                    _logger.LogWarning("❌ Empleado no encontrado: {Id}", id);
                    return NotFound(new
                    {
                        success = false,
                        message = $"Empleado con ID {id} no encontrado"
                    });
                }

                // ❌ BLOQUEAR PARA ADMIN (ID: 1)
                if (empleado.Usuario == "admin")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No se puede desactivar el usuario administrador"
                    });
                }

                var estadoAnterior = empleado.Activo;

                // TOGGLE: invertir estado
                empleado.Activo = !empleado.Activo;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Estado cambiado: Empleado {Id} - {Nombre} - {Anterior} → {Nuevo}",
                    id, $"{empleado.Nombres} {empleado.Paterno}", estadoAnterior, empleado.Activo);

                return Ok(new
                {
                    success = true,
                    message = empleado.Activo ? "Empleado activado" : "Empleado desactivado",
                    empleadoId = empleado.Id,
                    nombreCompleto = $"{empleado.Nombres} {empleado.Paterno}",
                    activo = empleado.Activo,
                    anterior = estadoAnterior
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error cambiando estado para empleado {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor"
                });
            }
        }

        // Método auxiliar para verificar si es admin
        private async Task<bool> EsUsuarioAdmin(int id)
        {
            var empleado = await _context.Empleados.FindAsync(id);
            return empleado?.Usuario == "admin";
        }

        [HttpGet("{id}/debug-info")]
        [AllowAnonymous]
        public async Task<ActionResult> DebugInfo(int id)
        {
            var empleado = await _context.Empleados
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empleado == null)
                return NotFound();

            return Ok(new
            {
                id = empleado.Id,
                nombre = $"{empleado.Nombres} {empleado.Paterno}",
                idRol = empleado.IdRol,
                idLugarActual = empleado.LugarTrabajoActualId,
                activo = empleado.Activo,
                rolExiste = await _context.Roles.AnyAsync(r => r.Id == empleado.IdRol)
            });
        }

        private (string usuario, string password) GenerarCredencialesAutomaticas(CrearEmpleadoRequest request)
        {
            if (!string.IsNullOrEmpty(request.Usuario) && !string.IsNullOrEmpty(request.Password))
                return (request.Usuario, request.Password);

            var primerNombre = request.Nombres.Split(' ')[0].ToLower();
            var paterno = request.Paterno.ToLower();

            var usuarioBase = $"{primerNombre}.{paterno}";
            usuarioBase = System.Text.RegularExpressions.Regex.Replace(usuarioBase, @"[^a-zA-Z0-9.]", "");

            var usuarioFinal = usuarioBase;
            var contador = 1;

            while (_context.Empleados.Any(e => e.Usuario == usuarioFinal && e.Activo))
            {
                usuarioFinal = $"{usuarioBase}{contador}";
                contador++;
            }

            var password = $"{paterno}123";

            return (usuarioFinal, password);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public class ActualizarEmpleadoRequest
    {
        public string Paterno { get; set; } = string.Empty;
        public string Materno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        
        public string? Telefono { get; set; }
        public int IdRol { get; set; }
        public bool Activo { get; set; }
    }
}