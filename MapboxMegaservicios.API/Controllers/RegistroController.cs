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
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RegistroController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistroController> _logger;

        public RegistroController(ApplicationDbContext context, ILogger<RegistroController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("marcar-entrada")]
        public async Task<ActionResult<RegistroResult>> MarcarEntrada([FromBody] MarcarRegistroRequest request)
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                var ubicacion = await RegistrarUbicacionAsync(empleadoId, request.Latitud, request.Longitud);

                if (ubicacion.EstaEnGeocerca == true)
                {
                    _logger.LogInformation("✅ Entrada registrada - Empleado: {EmpleadoId}", empleadoId);
                    return Ok(new RegistroResult
                    {
                        Success = true,
                        Message = "Entrada registrada exitosamente",
                        Tipo = "ENTRADA",
                        FechaHora = DateTime.Now
                    });
                }
                else
                {
                    _logger.LogWarning("❌ Entrada rechazada - Fuera de geocerca - Empleado: {EmpleadoId}", empleadoId);
                    return BadRequest(new RegistroResult
                    {
                        Success = false,
                        Message = "No puedes marcar entrada fuera de tu área de trabajo",
                        Tipo = "RECHAZADA"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando entrada");
                return StatusCode(500, new RegistroResult { Success = false, Message = "Error interno del servidor" });
            }
        }

        [HttpPost("marcar-salida")]
        public async Task<ActionResult<RegistroResult>> MarcarSalida([FromBody] MarcarRegistroRequest request)
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                await RegistrarUbicacionAsync(empleadoId, request.Latitud, request.Longitud);

                _logger.LogInformation("✅ Salida registrada - Empleado: {EmpleadoId}", empleadoId);
                return Ok(new RegistroResult
                {
                    Success = true,
                    Message = "Salida registrada exitosamente",
                    Tipo = "SALIDA",
                    FechaHora = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando salida");
                return StatusCode(500, new RegistroResult { Success = false, Message = "Error interno del servidor" });
            }
        }

        [HttpGet("mi-ubicacion-actual")]
        public async Task<ActionResult<UbicacionDTO>> ObtenerMiUbicacionActual()
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                var ultimaUbicacion = await _context.Ubicaciones
                    .Where(u => u.EmpleadoId == empleadoId)
                    .OrderByDescending(u => u.FechaHora)
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e.LugarTrabajoActual)
                    .FirstOrDefaultAsync();

                if (ultimaUbicacion == null)
                {
                    return NotFound(new { message = "No hay ubicaciones registradas" });
                }

                return Ok(new UbicacionDTO
                {
                    EmpleadoId = empleadoId,
                    EmpleadoNombre = $"{ultimaUbicacion.Empleado.Nombres} {ultimaUbicacion.Empleado.Paterno}",
                    Latitud = ultimaUbicacion.UbicacionEmp.Y,
                    Longitud = ultimaUbicacion.UbicacionEmp.X,
                    FechaHora = ultimaUbicacion.FechaHora,
                    EstaEnGeocerca = ultimaUbicacion.EstaEnGeocerca,
                    Estado = ultimaUbicacion.EstaEnGeocerca == true ? "Dentro de geocerca" : "Fuera de geocerca",
                    LugarTrabajo = ultimaUbicacion.Empleado.LugarTrabajoActual?.Nombre ?? "Sin asignar"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ubicación actual");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        private async Task<UbicacionDTO> RegistrarUbicacionAsync(int empleadoId, double latitud, double longitud)
        {
            var punto = new Point(longitud, latitud) { SRID = 4326 };
            var estaEnGeocerca = await VerificarGeocercaAsync(empleadoId, latitud, longitud);

            var ubicacion = new Ubicacion
            {
                    EmpleadoId = empleadoId,
                UbicacionEmp = punto,
                FechaHora = DateTime.UtcNow,
                EstaEnGeocerca = estaEnGeocerca
            };

            _context.Ubicaciones.Add(ubicacion);
            await RegistrarAlertaSiEsNecesario(empleadoId, estaEnGeocerca);
            await _context.SaveChangesAsync();

            var empleado = await _context.Empleados
                .Where(e => e.Id == empleadoId)
                .Select(e => new { e.Nombres, e.Paterno, e.LugarTrabajoActual })
                .FirstAsync();

            return new UbicacionDTO
            {
                EmpleadoId = empleadoId,
                EmpleadoNombre = $"{empleado.Nombres} {empleado.Paterno}",
                Latitud = latitud,
                Longitud = longitud,
                FechaHora = ubicacion.FechaHora,
                EstaEnGeocerca = estaEnGeocerca,
                Estado = estaEnGeocerca ? "Dentro de geocerca" : "Fuera de geocerca",
                LugarTrabajo = empleado.LugarTrabajoActual?.Nombre ?? "Sin asignar"
            };
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
                        EstadoAlertaId = estado.Id,
                        FechaHora = DateTime.UtcNow,
                        Observaciones = estaEnGeocerca ?
                            "Empleado ingresó al área de trabajo" :
                            "Empleado salió del área de trabajo"
                    };

                    _context.AlertasGeocerca.Add(alerta);
                    _logger.LogInformation("🚨 Alerta generada - Empleado: {EmpleadoId}, Estado: {Estado}",
                        empleadoId, codigoEstado);
                }
            }
        }

        private int GetEmpleadoIdFromToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userId, out int empleadoId) ? empleadoId : 0;
        }
    }
}