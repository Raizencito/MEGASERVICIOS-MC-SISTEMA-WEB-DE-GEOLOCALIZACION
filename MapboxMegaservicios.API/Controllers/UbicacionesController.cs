using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.DTOs;
using MapboxMegaservicios.API.Hubs;
using MapboxMegaservicios.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<UbicacionHub> _hubContext;

        public UbicacionesController(
            ApplicationDbContext context,
            ILogger<UbicacionesController> logger,
            IHubContext<UbicacionHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
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

                // Validación anti-spoofing
                var ultimaUbicacion = await _context.Ubicaciones
                    .Where(u => u.EmpleadoId == empleadoId)
                    .OrderByDescending(u => u.FechaHora)
                    .FirstOrDefaultAsync();

                bool isPossibleSpoofing = false;
                if (ultimaUbicacion != null)
                {
                    double lat1 = ultimaUbicacion.UbicacionEmp.Y;
                    double lon1 = ultimaUbicacion.UbicacionEmp.X;
                    double lat2 = request.Latitud;
                    double lon2 = request.Longitud;

                    double R = 6371e3; // Radio de la Tierra en metros
                    double phi1 = lat1 * Math.PI / 180;
                    double phi2 = lat2 * Math.PI / 180;
                    double deltaPhi = (lat2 - lat1) * Math.PI / 180;
                    double deltaLambda = (lon2 - lon1) * Math.PI / 180;

                    double a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                               Math.Cos(phi1) * Math.Cos(phi2) *
                               Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
                    double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                    double distanceMeters = R * c;
                    double secondsElapsed = (DateTime.UtcNow - ultimaUbicacion.FechaHora).TotalSeconds;

                    if (secondsElapsed > 0)
                    {
                        double speedMps = distanceMeters / secondsElapsed;
                        double speedKmh = speedMps * 3.6;

                        // Si la velocidad supera 100 km/h y la distancia es mayor a 100 metros (para evitar ruido GPS)
                        if (distanceMeters > 100 && speedKmh > 100.0)
                        {
                            isPossibleSpoofing = true;
                        }
                    }
                }

                var ubicacion = new Ubicacion
                {
                    EmpleadoId = empleadoId,
                    UbicacionEmp = punto,
                    FechaHora = DateTime.UtcNow,
                    EstaEnGeocerca = estaEnGeocerca,
                    IsPossibleSpoofing = isPossibleSpoofing
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

                _logger.LogInformation("📍 Ubicación registrada - Empleado: {Nombre} ({Id}), Estado: {Estado}, Spoofing: {Spoofing}",
                    nombreEmpleado, empleadoId, estaEnGeocerca ? "DENTRO" : "FUERA", isPossibleSpoofing);

                var resultDto = new UbicacionDTO
                {
                    EmpleadoId = empleadoId,
                    EmpleadoNombre = nombreEmpleado,
                    Latitud = request.Latitud,
                    Longitud = request.Longitud,
                    FechaHora = ubicacion.FechaHora,
                    EstaEnGeocerca = estaEnGeocerca,
                    Estado = estaEnGeocerca ? "Dentro de geocerca" : "Fuera de geocerca",
                    LugarTrabajo = lugarTrabajo ?? "Sin asignar",
                    IsPossibleSpoofing = isPossibleSpoofing
                };

                // Emitir a todos los clientes SignalR conectados
                await _hubContext.Clients.All.SendAsync("NuevaUbicacion", resultDto);

                return Ok(resultDto);
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

        [HttpGet("spoofing")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<List<UbicacionDTO>>> ObtenerUbicacionesSpoofing()
        {
            try
            {
                var ubicaciones = await _context.Ubicaciones
                    .Where(u => u.IsPossibleSpoofing)
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e.LugarTrabajoActual)
                    .OrderByDescending(u => u.FechaHora)
                    .Select(u => new UbicacionDTO
                    {
                        EmpleadoId = u.EmpleadoId,
                        EmpleadoNombre = $"{u.Empleado.Nombres} {u.Empleado.Paterno}",
                        Latitud = u.UbicacionEmp.Y,
                        Longitud = u.UbicacionEmp.X,
                        FechaHora = u.FechaHora,
                        EstaEnGeocerca = u.EstaEnGeocerca,
                        Estado = "Sospecha de Spoofing GPS",
                        LugarTrabajo = u.Empleado.LugarTrabajoActual != null ? u.Empleado.LugarTrabajoActual.Nombre : "Sin asignar",
                        IsPossibleSpoofing = true
                    })
                    .Take(50)
                    .ToListAsync();

                return Ok(ubicaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ubicaciones de spoofing");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("simular")]
        // [Authorize(Policy = "AdminOnly")] // Podemos requerir admin si queremos, o dejarlo libre temporalmente
        public async Task<ActionResult<UbicacionDTO>> SimularUbicacion([FromBody] SimularUbicacionRequest request)
        {
            try
            {
                var empleado = await _context.Empleados
                    .Include(e => e.LugarTrabajoActual)
                    .FirstOrDefaultAsync(e => e.Id == request.EmpleadoId && e.Activo);

                if (empleado == null)
                    return NotFound(new { message = "Empleado no encontrado o inactivo" });

                var punto = new Point(request.Longitud, request.Latitud) { SRID = 4326 };
                var estaEnGeocerca = await VerificarGeocercaAsync(request.EmpleadoId, request.Latitud, request.Longitud);

                var ubicacion = new Ubicacion
                {
                    EmpleadoId = request.EmpleadoId,
                    UbicacionEmp = punto,
                    FechaHora = DateTime.UtcNow,
                    EstaEnGeocerca = estaEnGeocerca,
                    IsPossibleSpoofing = false
                };

                _context.Ubicaciones.Add(ubicacion);

                // Registrar alerta si cambió el estado
                await RegistrarAlertaSiEsNecesario(request.EmpleadoId, estaEnGeocerca);

                await _context.SaveChangesAsync();

                var nombreEmpleado = $"{empleado.Nombres} {empleado.Paterno}";

                _logger.LogInformation("🎮 Simulación registrada - Empleado: {Nombre} ({Id}), Estado: {Estado}",
                    nombreEmpleado, request.EmpleadoId, estaEnGeocerca ? "DENTRO" : "FUERA");

                var resultDto = new UbicacionDTO
                {
                    EmpleadoId = request.EmpleadoId,
                    EmpleadoNombre = nombreEmpleado,
                    Latitud = request.Latitud,
                    Longitud = request.Longitud,
                    FechaHora = ubicacion.FechaHora,
                    EstaEnGeocerca = estaEnGeocerca,
                    Estado = estaEnGeocerca ? "Dentro de geocerca" : "Fuera de geocerca",
                    LugarTrabajo = empleado.LugarTrabajoActual?.Nombre ?? "Sin asignar",
                    IsPossibleSpoofing = false
                };

                // Emitir a todos los clientes SignalR conectados
                await _hubContext.Clients.All.SendAsync("NuevaUbicacion", resultDto);

                return Ok(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulando ubicación");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("sincronizar-offline")]
        [Authorize]
        public async Task<ActionResult> SincronizarOffline([FromBody] List<SincronizarOfflineRequest> ubicacionesOffline)
        {
            try
            {
                var empleadoId = GetEmpleadoIdFromToken();
                if (empleadoId == 0) return Unauthorized();

                var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == empleadoId && e.Activo);
                if (empleado == null) return Unauthorized(new { message = "Empleado no encontrado o inactivo" });

                var nombreEmpleado = $"{empleado.Nombres} {empleado.Paterno}";
                var lugarTrabajo = await _context.LugaresTrabajo
                    .Where(l => l.Id == empleado.LugarTrabajoActualId)
                    .Select(l => l.Nombre)
                    .FirstOrDefaultAsync();

                int guardadas = 0;
                var ubicacionesDto = new List<UbicacionDTO>();

                foreach (var req in ubicacionesOffline)
                {
                    var punto = new Point(req.Longitud, req.Latitud) { SRID = 4326 };
                    var estaEnGeocerca = await VerificarGeocercaAsync(empleadoId, req.Latitud, req.Longitud);

                    var ubicacion = new Ubicacion
                    {
                        EmpleadoId = empleadoId,
                        UbicacionEmp = punto,
                        FechaHora = req.FechaHoraLocal.ToUniversalTime(),
                        EstaEnGeocerca = estaEnGeocerca,
                        IsPossibleSpoofing = false // Podríamos implementar la validación offline luego
                    };

                    _context.Ubicaciones.Add(ubicacion);
                    await RegistrarAlertaSiEsNecesario(empleadoId, estaEnGeocerca);

                    ubicacionesDto.Add(new UbicacionDTO
                    {
                        EmpleadoId = empleadoId,
                        EmpleadoNombre = nombreEmpleado,
                        Latitud = req.Latitud,
                        Longitud = req.Longitud,
                        FechaHora = ubicacion.FechaHora,
                        EstaEnGeocerca = estaEnGeocerca,
                        Estado = estaEnGeocerca ? "Dentro de geocerca" : "Fuera de geocerca",
                        LugarTrabajo = lugarTrabajo ?? "Sin asignar",
                        IsPossibleSpoofing = false
                    });

                    guardadas++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("🔄 Sincronización offline completada - Empleado: {Nombre} ({Id}). Puntos: {Count}",
                    nombreEmpleado, empleadoId, guardadas);

                // Opcional: Emitir a SignalR para que el frontend web actualice,
                // aunque para ráfagas grandes quizás convenga emitir solo la última ubicación
                if (ubicacionesDto.Any())
                {
                    var ultima = ubicacionesDto.OrderByDescending(u => u.FechaHora).First();
                    await _hubContext.Clients.All.SendAsync("NuevaUbicacion", ultima);
                }

                return Ok(new { message = "Sincronización exitosa", guardadas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando ubicaciones offline");
                return StatusCode(500, new { message = "Error interno del servidor" });
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

    public class SimularUbicacionRequest
    {
        public int EmpleadoId { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    public class SincronizarOfflineRequest
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public DateTime FechaHoraLocal { get; set; }
    }
}