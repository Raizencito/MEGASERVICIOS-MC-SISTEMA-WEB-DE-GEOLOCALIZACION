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
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("estadisticas")]
        public async Task<ActionResult<DashboardEstadisticasDTO>> ObtenerEstadisticas()
        {
            try
            {
                var totalEmpleados = await _context.Empleados.CountAsync(e => e.Activo);
                var totalLugares = await _context.LugaresTrabajo.CountAsync(l => l.Activo);

                var inicioHoy = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                var finHoy = inicioHoy.AddDays(1);
                
                var alertasHoy = await _context.AlertasGeocerca
                    .CountAsync(a => a.FechaHora >= inicioHoy && a.FechaHora < finHoy);

                // Obtener última ubicación de cada empleado (últimas 24 horas)
                var ubicaciones = await _context.Empleados
                    .Where(e => e.Activo)
                    .Select(e => _context.Ubicaciones
                        .Where(u => u.EmpleadoId == e.Id && u.FechaHora > DateTime.UtcNow.AddHours(-24))
                        .OrderByDescending(u => u.FechaHora)
                        .FirstOrDefault())
                    .Where(u => u != null)
                    .ToListAsync();

                var enGeocerca = ubicaciones.Count(u => u.EstaEnGeocerca == true);
                var fueraGeocerca = ubicaciones.Count(u => u.EstaEnGeocerca == false);

                var ultimasAlertas = await _context.AlertasGeocerca
                    .Where(a => a.FechaHora >= inicioHoy && a.FechaHora < finHoy)
                    .Include(a => a.Empleado)
                    .Include(a => a.EstadoAlerta)
                    .OrderByDescending(a => a.FechaHora)
                    .Take(5)
                    .Select(a => new AlertaGeocercaDTO
                    {
                        EmpleadoNombre = $"{a.Empleado.Nombres} {a.Empleado.Paterno}",
                        TipoAlerta = a.EstadoAlerta.Descripcion,
                        FechaHora = a.FechaHora,
                        Observaciones = a.Observaciones ?? "Sin observaciones"
                    })
                    .ToListAsync();

                var dashboard = new DashboardEstadisticasDTO
                {
                    TotalEmpleados = totalEmpleados,
                    EmpleadosActivos = ubicaciones.Count,
                    EmpleadosEnGeocerca = enGeocerca,
                    EmpleadosFueraGeocerca = fueraGeocerca,
                    AlertasHoy = alertasHoy,
                    TotalLugares = totalLugares,
                    EmpleadosSinUbicacion = totalEmpleados - ubicaciones.Count,
                    UltimasAlertas = ultimasAlertas
                };

                _logger.LogInformation("📊 Dashboard generado: {EnGeocerca} dentro, {FueraGeocerca} fuera",
                    enGeocerca, fueraGeocerca);

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del dashboard");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("mapa-ubicaciones")]
        public async Task<ActionResult<List<UbicacionDTO>>> ObtenerUbicacionesParaMapa()
        {
            try
            {
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

                _logger.LogInformation("🗺️ Mapa generado con {Cantidad} ubicaciones", ubicaciones.Count);

                return Ok(ubicaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo ubicaciones para mapa");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("empleados-por-lugar")]
        public async Task<ActionResult<Dictionary<string, int>>> ObtenerEmpleadosPorLugar()
        {
            try
            {
                var empleadosPorLugar = await _context.Empleados
                    .Where(e => e.Activo && e.LugarTrabajoActual != null)
                    .GroupBy(e => e.LugarTrabajoActual!.Nombre)
                    .Select(g => new { Lugar = g.Key, Cantidad = g.Count() })
                    .ToDictionaryAsync(x => x.Lugar!, x => x.Cantidad);

                return Ok(empleadosPorLugar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleados por lugar");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("alertas-por-hora")]
        public async Task<ActionResult<Dictionary<int, int>>> ObtenerAlertasPorHora()
        {
            try
            {
                var inicioHoy = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                var finHoy = inicioHoy.AddDays(1);
                var alertasPorHora = await _context.AlertasGeocerca
                    .Where(a => a.FechaHora >= inicioHoy && a.FechaHora < finHoy)
                    .GroupBy(a => a.FechaHora.Hour)
                    .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                    .OrderBy(x => x.Hora)
                    .ToDictionaryAsync(x => x.Hora, x => x.Cantidad);

                return Ok(alertasPorHora);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo alertas por hora");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}