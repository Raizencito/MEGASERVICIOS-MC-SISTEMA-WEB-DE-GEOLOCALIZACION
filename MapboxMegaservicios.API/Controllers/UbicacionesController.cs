using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.DTOs;
using MapboxMegaservicios.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Security.Claims;

namespace MapboxMegaservicios.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UbicacionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UbicacionesController> _logger;

        public UbicacionesController(ApplicationDbContext context, ILogger<UbicacionesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("registrar")]
        [Authorize]
        public async Task<ActionResult<UbicacionDTO>> RegistrarUbicacion([FromBody] RegistrarUbicacionRequest request)
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                // Verificar que el empleado existe y está activo
                var empleado = await _context.Empleados
                    .FirstOrDefaultAsync(e => e.Id == empleadoId && e.Activo);

                if (empleado == null)
                {
                    return Unauthorized(new { message = "Empleado no encontrado o inactivo" });
                }

                var punto = new Point(request.Longitud, request.Latitud) { SRID = 4326 };
                var estaEnGeocerca = await VerificarGeocercaAsync(empleadoId, request.Latitud, request.Longitud);

                var ubicacion = new Ubicacion
                {
                    EmpleadoId= empleadoId,
                    UbicacionEmp = punto,
                    FechaHora = DateTime.UtcNow,
                    EstaEnGeocerca = estaEnGeocerca
                };

                _context.Ubicaciones.Add(ubicacion);

                // Registrar alerta si cambió el estado
                await RegistrarAlertaSiEsNecesario(empleadoId, estaEnGeocerca);

                await _context.SaveChangesAsync();

                // Obtener nombre del empleado
                var nombreEmpleado = $"{empleado.Nombres} {empleado.Paterno}";
                var lugarTrabajo = await _context.LugaresTrabajo
                    .Where(l => l.Id == empleado.LugarTrabajoActualId)
                    .Select(l => l.Nombre)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("📍 Ubicación registrada - Empleado: {Nombre} ({Id}), Estado: {Estado}",
                    nombreEmpleado, empleadoId, estaEnGeocerca ? "DENTRO" : "FUERA");

                return Ok(new UbicacionDTO
                {
                    EmpleadoId = empleadoId,
                    EmpleadoNombre = nombreEmpleado,
                    Latitud = request.Latitud,
                    Longitud = request.Longitud,
                    FechaHora = ubicacion.FechaHora,
                    EstaEnGeocerca = estaEnGeocerca,
                    Estado = estaEnGeocerca ? "Dentro de geocerca" : "Fuera de geocerca",
                    LugarTrabajo = lugarTrabajo ?? "Sin asignar"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando ubicación");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("ultimas")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<List<UbicacionDTO>>> ObtenerUltimasUbicaciones()
        {
            try
            {
                // Obtener la última ubicación de cada empleado activo
                var ubicaciones = await _context.Empleados
                    .Where(e => e.Activo)
                    .Select(e => new 
                    {
                        Empleado = e,
                        Lugar = e.LugarTrabajoActual,
                        UltimaUbicacion = _context.Ubicaciones
                            .Where(u => u.EmpleadoId == e.Id && u.FechaHora > DateTime.UtcNow.AddHours(-24))
                            .OrderByDescending(u => u.FechaHora)
                            .FirstOrDefault()
                    })
                    .Where(x => x.UltimaUbicacion != null)
                    .Select(x => new UbicacionDTO
                    {
                        EmpleadoId = x.Empleado.Id,
                        EmpleadoNombre = x.Empleado.Nombres + " " + x.Empleado.Paterno,
                        Latitud = x.UltimaUbicacion!.UbicacionEmp.Y,
                        Longitud = x.UltimaUbicacion!.UbicacionEmp.X,
                        FechaHora = x.UltimaUbicacion.FechaHora,
                        EstaEnGeocerca = x.UltimaUbicacion.EstaEnGeocerca,
                        Estado = x.UltimaUbicacion.EstaEnGeocerca == true ? "Dentro de geocerca" : "Fuera de geocerca",
                        LugarTrabajo = x.Lugar != null ? x.Lugar.Nombre : "Sin asignar"
                    })
                    .ToListAsync();

                return Ok(ubicaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo últimas ubicaciones");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("empleado/{empleadoId}/historial")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<List<UbicacionDTO>>> ObtenerHistorial(
            int empleadoId,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var query = _context.Ubicaciones
                    .Where(u => u.EmpleadoId == empleadoId)
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e.LugarTrabajoActual)
                    .OrderByDescending(u => u.FechaHora);

                if (desde.HasValue)
                {
                    query = (IOrderedQueryable<Ubicacion>)query.Where(u => u.FechaHora >= desde.Value);
                }

                if (hasta.HasValue)
                {
                    query = (IOrderedQueryable<Ubicacion>)query.Where(u => u.FechaHora <= hasta.Value);
                }

                var historial = await query
                    .Select(u => new UbicacionDTO
                    {
                        EmpleadoId = u.EmpleadoId,
                        EmpleadoNombre = $"{u.Empleado.Nombres} {u.Empleado.Paterno}",
                        Latitud = u.UbicacionEmp.Y,
                        Longitud = u.UbicacionEmp.X,
                        FechaHora = u.FechaHora,
                        EstaEnGeocerca = u.EstaEnGeocerca,
                        Estado = u.EstaEnGeocerca == true ? "Dentro de geocerca" : "Fuera de geocerca",
                        LugarTrabajo = u.Empleado.LugarTrabajoActual != null ? u.Empleado.LugarTrabajoActual.Nombre : "Sin asignar"
                    })
                    .Take(100) // Limitar a 100 registros para no sobrecargar
                    .ToListAsync();

                return Ok(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de ubicaciones para empleado {EmpleadoId}", empleadoId);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("alertas")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<List<AlertaGeocercaDTO>>> ObtenerAlertas(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? empleadoId = null)
        {
            try
            {
                var query = _context.AlertasGeocerca
                    .Include(a => a.Empleado)
                    .Include(a => a.EstadoAlerta)
                    .OrderByDescending(a => a.FechaHora)
                    .AsQueryable();

                if (desde.HasValue)
                {
                    query = query.Where(a => a.FechaHora >= desde.Value);
                }

                if (hasta.HasValue)
                {
                    query = query.Where(a => a.FechaHora <= hasta.Value);
                }

                if (empleadoId.HasValue)
                {
                    query = query.Where(a => a.EmpleadoId == empleadoId.Value);
                }

                var alertas = await query
                    .Select(a => new AlertaGeocercaDTO
                    {
                        Id = a.Id,
                        EmpleadoNombre = $"{a.Empleado.Nombres} {a.Empleado.Paterno}",
                        TipoAlerta = a.EstadoAlerta.Descripcion,
                        FechaHora = a.FechaHora,
                        Observaciones = a.Observaciones ?? "Sin observaciones"
                    })
                    .Take(200) // Limitar a 200 alertas
                    .ToListAsync();

                return Ok(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo alertas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        private async Task<bool> VerificarGeocercaAsync(int empleadoId, double latitud, double longitud)
        {
            var punto = new Point(longitud, latitud) { SRID = 4326 };

            var lugarTrabajo = await _context.Empleados
                .Where(e => e.Id == empleadoId)
                .Select(e => e.LugarTrabajoActual)
                .FirstAsync();

            if (lugarTrabajo?.Geocerca == null) return false;

            return lugarTrabajo.Geocerca.Contains(punto) == true;
        }

        private async Task RegistrarAlertaSiEsNecesario(int empleadoId, bool estaEnGeocerca)
        {
            var ultimaUbicacion = await _context.Ubicaciones
                .Where(u => u.EmpleadoId == empleadoId)
                .OrderByDescending(u => u.FechaHora)
                .FirstOrDefaultAsync();

            // Si es la primera ubicación O si cambió el estado
            if (ultimaUbicacion == null || ultimaUbicacion.EstaEnGeocerca != estaEnGeocerca)
            {
                var codigoEstado = estaEnGeocerca ? "DENTRO" : "FUERA";
                var estado = await _context.EstadosAlerta
                    .FirstOrDefaultAsync(e => e.Codigo == codigoEstado);

                if (estado != null)
                {
                    var alerta = new AlertaGeocerca
                    {
                        EmpleadoId = empleadoId,
                        EstadoAlertaId= estado.Id,
                        FechaHora = DateTime.UtcNow,
                        Observaciones = estaEnGeocerca ?
                            "Empleado ingresó al área de trabajo" :
                            "Empleado salió del área de trabajo"
                    };

                    _context.AlertasGeocerca.Add(alerta);
                }
            }
        }

        private int GetEmpleadoIdFromToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userId, out int empleadoId) ? empleadoId : 0;
        }
    }

    // DTO interno (no mover a DTOs compartidos)
    public class RegistrarUbicacionRequest
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }
}