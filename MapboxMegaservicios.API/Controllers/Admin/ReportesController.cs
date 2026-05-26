using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MapboxMegaservicios.API.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/admin/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportesController> _logger;

        public ReportesController(ApplicationDbContext context, ILogger<ReportesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("alertas")]
        public async Task<ActionResult<ReporteAlertasDTO>> GenerarReporteAlertas(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? empleadoId = null)
        {
            try
            {
                // Validar fechas
                if (desde > hasta)
                    return BadRequest(new { message = "La fecha 'desde' no puede ser mayor a 'hasta'" });

                if ((hasta - desde).TotalDays > 31)
                    return BadRequest(new { message = "El período máximo es de 31 días" });

                var query = _context.AlertasGeocerca
                    .Where(a => a.FechaHora >= desde && a.FechaHora <= hasta)
                    .Include(a => a.Empleado)
                    .Include(a => a.EstadoAlerta)
                    .AsQueryable();

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
                    .OrderByDescending(a => a.FechaHora)
                    .ToListAsync();

                var reporte = new ReporteAlertasDTO
                {
                    Desde = desde,
                    Hasta = hasta,
                    TotalAlertas = alertas.Count,
                    Alertas = alertas,
                    AlertasPorTipo = alertas
                        .GroupBy(a => a.TipoAlerta)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    AlertasPorEmpleado = alertas
                        .GroupBy(a => a.EmpleadoNombre)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                _logger.LogInformation("📄 Reporte de alertas generado: {Desde} a {Hasta}, Total: {Total}",
                    desde.ToString("yyyy-MM-dd"), hasta.ToString("yyyy-MM-dd"), alertas.Count);

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de alertas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("asistencia")]
        public async Task<ActionResult<ReporteAsistenciaDTO>> GenerarReporteAsistencia([FromQuery] DateTime fecha)
        {
            try
            {
                var desde = fecha.Date;
                var hasta = fecha.Date.AddDays(1).AddSeconds(-1);

                // Obtener última ubicación de cada empleado en ese día
                var ubicaciones = await _context.Ubicaciones
                    .Where(u => u.FechaHora >= desde && u.FechaHora <= hasta)
                    .GroupBy(u => u.EmpleadoId)
                    .Select(g => g.OrderByDescending(u => u.FechaHora).First())
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e.LugarTrabajoActual)
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
                    .ToListAsync();

                // Obtener alertas del día
                var alertas = await _context.AlertasGeocerca
                    .Where(a => a.FechaHora >= desde && a.FechaHora <= hasta)
                    .Include(a => a.Empleado)
                    .Include(a => a.EstadoAlerta)
                    .Select(a => new
                    {
                        EmpleadoNombre = $"{a.Empleado.Nombres} {a.Empleado.Paterno}",
                        TipoAlerta = a.EstadoAlerta.Descripcion,
                        a.FechaHora
                    })
                    .ToListAsync();

                var reporte = new ReporteAsistenciaDTO
                {
                    Fecha = fecha,
                    TotalEmpleados = await _context.Empleados.CountAsync(e => e.Activo),
                    EmpleadosEnGeocerca = ubicaciones.Count(u => u.EstaEnGeocerca == true),
                    EmpleadosFueraGeocerca = ubicaciones.Count(u => u.EstaEnGeocerca == false),
                    AlertasDelDia = alertas.Count,
                    EmpleadosSinUbicacion = await _context.Empleados.CountAsync(e => e.Activo) - ubicaciones.Count,
                    Detalles = ubicaciones.Select(u => new DetalleAsistenciaDTO
                    {
                        EmpleadoNombre = u.EmpleadoNombre,
                        LugarTrabajo = u.LugarTrabajo,
                        Estado = u.Estado,
                        UltimaUbicacion = u.FechaHora,
                        AlertasHoy = alertas.Count(a => a.EmpleadoNombre == u.EmpleadoNombre)
                    }).ToList()
                };

                _logger.LogInformation("📄 Reporte de asistencia generado para {Fecha}: {EnGeocerca} dentro, {FueraGeocerca} fuera",
                    fecha.ToString("yyyy-MM-dd"), reporte.EmpleadosEnGeocerca, reporte.EmpleadosFueraGeocerca);

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de asistencia para fecha {Fecha}", fecha);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("tiempos-fuera")]
        public async Task<ActionResult<ReporteTiemposFueraDTO>> GenerarReporteTiemposFuera(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? empleadoId = null)
        {
            try
            {
                if (desde > hasta)
                    return BadRequest(new { message = "La fecha 'desde' no puede ser mayor a 'hasta'" });

                var query = _context.Ubicaciones
                    .Where(u => u.FechaHora >= desde && u.FechaHora <= hasta)
                    .Include(u => u.Empleado)
                    .OrderBy(u => u.EmpleadoId)
                    .ThenBy(u => u.FechaHora)
                    .AsQueryable();

                if (empleadoId.HasValue)
                {
                    query = query.Where(u => u.EmpleadoId == empleadoId.Value);
                }

                var ubicaciones = await query.ToListAsync();

                // Calcular tiempos fuera de geocerca
                var tiemposPorEmpleado = new Dictionary<string, TimeSpan>();
                var empleadoActual = 0;
                var horaEntrada = DateTime.MinValue;
                var tiempoFueraAcumulado = TimeSpan.Zero;

                foreach (var ubicacion in ubicaciones)
                {
                    if (ubicacion.EmpleadoId != empleadoActual)
                    {
                        // Nuevo empleado, resetear contadores
                        if (empleadoActual != 0)
                        {
                            var nombreEmpleado = $"{ubicaciones.First(u => u.EmpleadoId == empleadoActual).Empleado.Nombres} " +
                                               $"{ubicaciones.First(u => u.EmpleadoId == empleadoActual).Empleado.Paterno}";
                            tiemposPorEmpleado[nombreEmpleado] = tiempoFueraAcumulado;
                        }

                        empleadoActual = ubicacion.EmpleadoId;
                        horaEntrada = ubicacion.FechaHora;
                        tiempoFueraAcumulado = TimeSpan.Zero;
                    }

                    // Si está fuera de geocerca, acumular tiempo
                    if (ubicacion.EstaEnGeocerca == false && horaEntrada != DateTime.MinValue)
                    {
                        tiempoFueraAcumulado += ubicacion.FechaHora - horaEntrada;
                    }

                    horaEntrada = ubicacion.FechaHora;
                }

                // Agregar el último empleado
                if (empleadoActual != 0)
                {
                    var nombreEmpleado = $"{ubicaciones.First(u => u.EmpleadoId == empleadoActual).Empleado.Nombres} " +
                                       $"{ubicaciones.First(u => u.EmpleadoId == empleadoActual).Empleado.Paterno}";
                    tiemposPorEmpleado[nombreEmpleado] = tiempoFueraAcumulado;
                }

                var reporte = new ReporteTiemposFueraDTO
                {
                    Desde = desde,
                    Hasta = hasta,
                    TiemposPorEmpleado = tiemposPorEmpleado,
                    TotalTiempoFuera = tiemposPorEmpleado.Values.Aggregate(TimeSpan.Zero, (total, tiempo) => total + tiempo)
                };

                _logger.LogInformation("⏱️ Reporte de tiempos fuera generado: {Desde} a {Hasta}",
                    desde.ToString("yyyy-MM-dd"), hasta.ToString("yyyy-MM-dd"));

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de tiempos fuera");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("exportar-alertas")]
        public async Task<IActionResult> ExportarAlertas(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] string formato = "json")
        {
            try
            {
                var alertas = await _context.AlertasGeocerca
                    .Where(a => a.FechaHora >= desde && a.FechaHora <= hasta)
                    .Include(a => a.Empleado)
                    .Include(a => a.EstadoAlerta)
                    .Select(a => new
                    {
                        Empleado = $"{a.Empleado.Nombres} {a.Empleado.Paterno}",
                        a.Empleado.Ci,
                        TipoAlerta = a.EstadoAlerta.Descripcion,
                        FechaHora = a.FechaHora.ToString("yyyy-MM-dd HH:mm:ss"),
                        a.Observaciones
                    })
                    .ToListAsync();

                if (formato.ToLower() == "csv")
                {
                    var csv = "Empleado,CI,TipoAlerta,FechaHora,Observaciones\n";
                    csv += string.Join("\n", alertas.Select(a =>
                        $"\"{a.Empleado}\",\"{a.Ci}\",\"{a.TipoAlerta}\",\"{a.FechaHora}\",\"{a.Observaciones}\""));

                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv",
                        $"alertas_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.csv");
                }

                // Por defecto retorna JSON
                return Ok(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando alertas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
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
    }

    // DTOs adicionales para reportes
    public class ReporteTiemposFueraDTO
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public Dictionary<string, TimeSpan> TiemposPorEmpleado { get; set; } = new();
        public TimeSpan TotalTiempoFuera { get; set; }
    }
}