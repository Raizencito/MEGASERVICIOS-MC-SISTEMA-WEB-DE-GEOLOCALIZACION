using System.Security.Claims;
using MapboxMegaservicios.API.Models;
using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.DTOs;
using MapboxMegaservicios.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MapboxMegaservicios.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AsistenciaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AsistenciaController> _logger;

        public AsistenciaController(ApplicationDbContext context, ILogger<AsistenciaController> logger)
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

                // Verificar que está dentro de su geocerca
                var estaEnGeocerca = await VerificarGeocercaAsync(empleadoId, request.Latitud, request.Longitud);

                // Registrar ubicación SIEMPRE, incluso si está fuera (para que el pin aparezca en el mapa)
                var ubicacion = await RegistrarUbicacionAsync(empleadoId, request.Latitud, request.Longitud, estaEnGeocerca);

                if (!estaEnGeocerca)
                {
                    _logger.LogWarning("❌ Entrada rechazada - Fuera de geocerca - Empleado: {EmpleadoId}", empleadoId);
                    return BadRequest(new RegistroResult
                    {
                        Success = false,
                        Message = "Solo puedes marcar entrada dentro de tu área de trabajo asignada",
                        Tipo = "RECHAZADA"
                    });
                }

                // Verificar si ya tiene entrada hoy
                var hoy = DateTime.UtcNow.Date;
                var entradaHoy = await _context.RegistrosAsistencia
                    .AnyAsync(r => r.EmpleadoId == empleadoId &&
                                  r.TipoRegistro == "ENTRADA" &&
                                  r.FechaHora.Date == hoy);

                if (entradaHoy)
                {
                    return BadRequest(new RegistroResult
                    {
                        Success = false,
                        Message = "Ya tienes una entrada registrada hoy",
                        Tipo = "DUPLICADA"
                    });
                }

                // Crear registro de asistencia
                var registro = new RegistroAsistencia
                {
                    EmpleadoId = empleadoId,
                    TipoRegistro = "ENTRADA",
                    FechaHora = DateTime.UtcNow,
                    UbicacionId = ubicacion.Id,
                    EsAutomatico = false,
                    Observaciones = "Entrada manual",
                    Verificado = true
                };

                _context.RegistrosAsistencia.Add(registro);

                // Crear o actualizar jornada
                var jornada = await _context.JornadasTrabajo
                    .FirstOrDefaultAsync(j => j.EmpleadoId == empleadoId && j.Fecha == hoy);

                if (jornada == null)
                {
                    jornada = new JornadaTrabajo
                    {
                        EmpleadoId = empleadoId,
                        Fecha = hoy,
                        HoraEntrada = DateTime.UtcNow,
                        Estado = "PENDIENTE"
                    };
                    _context.JornadasTrabajo.Add(jornada);
                }
                else
                {
                    jornada.HoraEntrada = DateTime.UtcNow;
                    jornada.Estado = "PENDIENTE";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Entrada registrada - Empleado: {EmpleadoId} - Hora: {Hora}",
                    empleadoId, DateTime.UtcNow.ToString("HH:mm"));

                return Ok(new RegistroResult
                {
                    Success = true,
                    Message = "Entrada registrada exitosamente",
                    Tipo = "ENTRADA",
                    FechaHora = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando entrada");
                return StatusCode(500, new RegistroResult
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpPost("marcar-salida")]
        public async Task<ActionResult<RegistroResult>> MarcarSalida([FromBody] MarcarRegistroRequest request)
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                // Verificar que está dentro de su geocerca (SALIDA también dentro)
                var estaEnGeocerca = await VerificarGeocercaAsync(empleadoId, request.Latitud, request.Longitud);

                // Registrar ubicación SIEMPRE (para que el pin aparezca en el mapa)
                var ubicacion = await RegistrarUbicacionAsync(empleadoId, request.Latitud, request.Longitud, estaEnGeocerca);

                if (!estaEnGeocerca)
                {
                    _logger.LogWarning("❌ Salida rechazada - Fuera de geocerca - Empleado: {EmpleadoId}", empleadoId);
                    return BadRequest(new RegistroResult
                    {
                        Success = false,
                        Message = "Solo puedes marcar salida dentro de tu área de trabajo asignada",
                        Tipo = "RECHAZADA"
                    });
                }

                // Verificar si tiene entrada hoy
                var hoy = DateTime.UtcNow.Date;
                var entradaHoy = await _context.RegistrosAsistencia
                    .Where(r => r.EmpleadoId == empleadoId &&
                               r.TipoRegistro == "ENTRADA" &&
                               r.FechaHora.Date == hoy)
                    .OrderByDescending(r => r.FechaHora)
                    .FirstOrDefaultAsync();

                if (entradaHoy == null)
                {
                    return BadRequest(new RegistroResult
                    {
                        Success = false,
                        Message = "No tienes una entrada registrada hoy",
                        Tipo = "SIN_ENTRADA"
                    });
                }

                // Verificar si ya tiene salida hoy
                var salidaHoy = await _context.RegistrosAsistencia
                    .AnyAsync(r => r.EmpleadoId == empleadoId &&
                                  r.TipoRegistro == "SALIDA" &&
                                  r.FechaHora.Date == hoy);

                if (salidaHoy)
                {
                    return BadRequest(new RegistroResult
                    {
                        Success = false,
                        Message = "Ya tienes una salida registrada hoy",
                        Tipo = "DUPLICADA"
                    });
                }

                // Crear registro de salida
                var registro = new RegistroAsistencia
                {
                    EmpleadoId = empleadoId,
                    TipoRegistro = "SALIDA",
                    FechaHora = DateTime.UtcNow,
                    UbicacionId = ubicacion.Id,
                    EsAutomatico = false,
                    Observaciones = "Salida manual",
                    Verificado = true
                };

                _context.RegistrosAsistencia.Add(registro);

                // Actualizar jornada
                var jornada = await _context.JornadasTrabajo
                    .FirstOrDefaultAsync(j => j.EmpleadoId == empleadoId && j.Fecha == hoy);

                if (jornada != null)
                {
                    jornada.HoraSalida = DateTime.UtcNow;

                    // Calcular horas trabajadas (si tiene entrada y salida)
                    if (jornada.HoraEntrada.HasValue && jornada.HoraSalida.HasValue)
                    {
                        var horasTrabajadas = (decimal)(jornada.HoraSalida.Value - jornada.HoraEntrada.Value).TotalHours;
                        jornada.TotalHoras = Math.Round(horasTrabajadas, 2);
                        jornada.Estado = horasTrabajadas >= 8 ? "COMPLETADA" : "INCOMPLETA";
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Salida registrada - Empleado: {EmpleadoId} - Hora: {Hora}",
                    empleadoId, DateTime.UtcNow.ToString("HH:mm"));

                return Ok(new RegistroResult
                {
                    Success = true,
                    Message = "Salida registrada exitosamente",
                    Tipo = "SALIDA",
                    FechaHora = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando salida");
                return StatusCode(500, new RegistroResult
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        [HttpGet("mi-jornada-hoy")]
        public async Task<ActionResult<JornadaDTO>> ObtenerMiJornadaHoy()
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                var hoy = DateTime.UtcNow.Date;

                var jornada = await _context.JornadasTrabajo
                    .Include(j => j.Empleado)
                    .FirstOrDefaultAsync(j => j.EmpleadoId == empleadoId && j.Fecha == hoy);

                if (jornada == null)
                {
                    return Ok(new JornadaDTO
                    {
                        Fecha = hoy,
                        Estado = "SIN_REGISTRO",
                        Mensaje = "No has registrado entrada hoy"
                    });
                }

                // Obtener registros del día
                var registros = await _context.RegistrosAsistencia
                    .Where(r => r.EmpleadoId == empleadoId && r.FechaHora.Date == hoy)
                    .OrderBy(r => r.FechaHora)
                    .Select(r => new RegistroAsistenciaDTO
                    {
                        Id = r.Id,
                        TipoRegistro = r.TipoRegistro,
                        FechaHora = r.FechaHora,
                        Observaciones = r.Observaciones,
                        Verificado = r.Verificado,
                        UbicacionCoords = r.Ubicacion != null ?
                            $"{r.Ubicacion.UbicacionEmp.Y}, {r.Ubicacion.UbicacionEmp.X}" : null
                    })
                    .ToListAsync();

                return Ok(new JornadaDTO
                {
                    Id = jornada.Id,
                    Fecha = jornada.Fecha,
                    HoraEntrada = jornada.HoraEntrada,
                    HoraSalida = jornada.HoraSalida,
                    TotalHoras = jornada.TotalHoras,
                    Estado = jornada.Estado,
                    TiempoFueraGeocerca = jornada.TiempoFueraGeocerca,
                    AlertasGeneradas = jornada.AlertasGeneradas,
                    Registros = registros
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo jornada actual");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("mis-asistencias")]
        public async Task<ActionResult<List<JornadaDTO>>> ObtenerMisAsistencias(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                var query = _context.JornadasTrabajo
                    .Where(j => j.EmpleadoId == empleadoId)
                    .OrderByDescending(j => j.Fecha)
                    .AsQueryable();

                if (desde.HasValue)
                    query = query.Where(j => j.Fecha >= desde.Value);

                if (hasta.HasValue)
                    query = query.Where(j => j.Fecha <= hasta.Value);

                var jornadas = await query
                    .Take(30) // Últimos 30 días
                    .Select(j => new JornadaDTO
                    {
                        Id = j.Id,
                        Fecha = j.Fecha,
                        HoraEntrada = j.HoraEntrada,
                        HoraSalida = j.HoraSalida,
                        TotalHoras = j.TotalHoras,
                        Estado = j.Estado,
                        TiempoFueraGeocerca = j.TiempoFueraGeocerca,
                        AlertasGeneradas = j.AlertasGeneradas
                    })
                    .ToListAsync();

                return Ok(jornadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo historial de asistencias");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // MÉTODOS AUXILIARES (copiar de tus controllers existentes)
        private async Task<bool> VerificarGeocercaAsync(int empleadoId, double latitud, double longitud)
        {
            var punto = new NetTopologySuite.Geometries.Point(longitud, latitud) { SRID = 4326 };

            var geocerca = await _context.Empleados
                .AsNoTracking()
                .Where(e => e.Id == empleadoId)
                .Select(e => e.LugarTrabajoActual != null ? e.LugarTrabajoActual.Geocerca : null)
                .FirstOrDefaultAsync();

            if (geocerca == null) return false;

            return geocerca.Contains(punto) == true;
        }

        private async Task<Ubicacion> RegistrarUbicacionAsync(int empleadoId, double latitud, double longitud, bool estaEnGeocerca)
        {
            var punto = new NetTopologySuite.Geometries.Point(longitud, latitud) { SRID = 4326 };

            var ubicacion = new Ubicacion
            {
                EmpleadoId= empleadoId,
                UbicacionEmp = punto,
                FechaHora = DateTime.UtcNow,
                EstaEnGeocerca = estaEnGeocerca
            };

            _context.Ubicaciones.Add(ubicacion);
            await _context.SaveChangesAsync();

            return ubicacion;
        }

        private int GetEmpleadoIdFromToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userId, out int empleadoId) ? empleadoId : 0;
        }
    }

    // DTOs necesarios (agregar a DTOs/)
    public class MarcarRegistroRequest
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    public class RegistroResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public DateTime? FechaHora { get; set; }
    }

    public class JornadaDTO
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? HoraEntrada { get; set; }
        public DateTime? HoraSalida { get; set; }
        public decimal? TotalHoras { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int TiempoFueraGeocerca { get; set; }
        public int AlertasGeneradas { get; set; }
        public List<RegistroAsistenciaDTO> Registros { get; set; } = new();
        public string? Mensaje { get; set; }
    }

    public class RegistroAsistenciaDTO
    {
        public int Id { get; set; }
        public string TipoRegistro { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string? Observaciones { get; set; }
        public bool Verificado { get; set; }
        public string? UbicacionCoords { get; set; }
    }
}