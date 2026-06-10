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
            [FromQuery] int? empleadoId = null,
            [FromQuery] int? departamentoId = null,
            [FromQuery] int? lugarTrabajoId = null)
        {
            try
            {
                // Validar fechas
                if (desde > hasta)
                    return BadRequest(new { message = "La fecha 'desde' no puede ser mayor a 'hasta'" });

                if ((hasta - desde).TotalDays > 31)
                    return BadRequest(new { message = "El período máximo es de 31 días" });

                var utcDesde = DateTime.SpecifyKind(desde.Date, DateTimeKind.Utc);
                var utcHasta = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                var query = _context.AlertasGeocerca
                    .Where(a => a.FechaHora >= utcDesde && a.FechaHora <= utcHasta)
                    .Include(a => a.Empleado)
                    .Include(a => a.EstadoAlerta)
                    .AsQueryable();

                if (empleadoId.HasValue)
                {
                    query = query.Where(a => a.EmpleadoId == empleadoId.Value);
                }

                if (lugarTrabajoId.HasValue)
                {
                    query = query.Where(a => a.Empleado.LugarTrabajoActualId == lugarTrabajoId.Value);
                }
                if (departamentoId.HasValue)
                {
                    query = query.Where(a => a.Empleado.LugarTrabajoActual != null && a.Empleado.LugarTrabajoActual.DepartamentoId == departamentoId.Value);
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
        public async Task<ActionResult<ReporteAsistenciaDTO>> GenerarReporteAsistencia([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            try
            {
                if (desde > hasta) return BadRequest(new { message = "La fecha 'desde' no puede ser mayor a 'hasta'" });
                if ((hasta - desde).TotalDays > 31) return BadRequest(new { message = "El período máximo es de 31 días" });

                var utcDesde = DateTime.SpecifyKind(desde.Date, DateTimeKind.Utc);
                var utcHasta = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                // Obtener última ubicación de cada empleado en ese día
                var ultimasUbicaciones = await _context.Ubicaciones
                    .Where(u => u.FechaHora >= utcDesde && u.FechaHora <= utcHasta)
                    .GroupBy(u => u.EmpleadoId)
                    .Select(g => g.OrderByDescending(u => u.FechaHora).First())
                    .ToListAsync();

                // Cargar datos de empleados por separado (Include tras Select no funciona)
                var empleadoIds = ultimasUbicaciones.Select(u => u.EmpleadoId).ToList();
                var empleados = await _context.Empleados
                    .Where(e => empleadoIds.Contains(e.Id))
                    .Include(e => e.LugarTrabajoActual)
                    .ToDictionaryAsync(e => e.Id);

                var ubicaciones = ultimasUbicaciones.Select(u =>
                {
                    empleados.TryGetValue(u.EmpleadoId, out var emp);
                    return new UbicacionDTO
                    {
                        EmpleadoId = u.EmpleadoId,
                        EmpleadoNombre = emp != null ? $"{emp.Nombres} {emp.Paterno}" : "Desconocido",
                        Latitud = u.UbicacionEmp.Y,
                        Longitud = u.UbicacionEmp.X,
                        FechaHora = u.FechaHora,
                        EstaEnGeocerca = u.EstaEnGeocerca,
                        Estado = u.EstaEnGeocerca == true ? "Dentro de geocerca" : "Fuera de geocerca",
                        LugarTrabajo = emp?.LugarTrabajoActual?.Nombre ?? "Sin asignar"
                    };
                }).ToList();

                // Obtener alertas del día
                var alertas = await _context.AlertasGeocerca
                    .Where(a => a.FechaHora >= utcDesde && a.FechaHora <= utcHasta)
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
                    Fecha = desde, // O un rango si se prefiere
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

                _logger.LogInformation("📄 Reporte de asistencia generado para {Desde} a {Hasta}: {EnGeocerca} dentro, {FueraGeocerca} fuera",
                    utcDesde.ToString("yyyy-MM-dd"), utcHasta.ToString("yyyy-MM-dd"), reporte.EmpleadosEnGeocerca, reporte.EmpleadosFueraGeocerca);

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de asistencia");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("tiempos-fuera")]
        public async Task<ActionResult<ReporteTiemposFueraDTO>> GenerarReporteTiemposFuera(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int? empleadoId = null,
            [FromQuery] int? departamentoId = null,
            [FromQuery] int? lugarTrabajoId = null)
        {
            try
            {
                if (desde > hasta)
                    return BadRequest(new { message = "La fecha 'desde' no puede ser mayor a 'hasta'" });

                var utcDesde = DateTime.SpecifyKind(desde.Date, DateTimeKind.Utc);
                var utcHasta = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                var query = _context.Ubicaciones
                    .Where(u => u.FechaHora >= utcDesde && u.FechaHora <= utcHasta)
                    .Include(u => u.Empleado)
                    .OrderBy(u => u.EmpleadoId)
                    .ThenBy(u => u.FechaHora)
                    .AsQueryable();

                if (empleadoId.HasValue)
                {
                    query = query.Where(u => u.EmpleadoId == empleadoId.Value);
                }
                
                if (lugarTrabajoId.HasValue)
                {
                    query = query.Where(u => u.Empleado.LugarTrabajoActualId == lugarTrabajoId.Value);
                }
                if (departamentoId.HasValue)
                {
                    query = query.Where(u => u.Empleado.LugarTrabajoActual != null && u.Empleado.LugarTrabajoActual.DepartamentoId == departamentoId.Value);
                }

                var ubicaciones = await query.ToListAsync();

                // Construir diccionario de nombres (evita O(n²) con First())
                var nombreEmpleados = ubicaciones
                    .Select(u => u.Empleado)
                    .Where(e => e != null)
                    .Distinct()
                    .ToDictionary(e => e.Id, e => $"{e.Nombres} {e.Paterno}");

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
                        if (empleadoActual != 0 && nombreEmpleados.TryGetValue(empleadoActual, out var nom))
                        {
                            tiemposPorEmpleado[nom] = tiempoFueraAcumulado;
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
                if (empleadoActual != 0 && nombreEmpleados.TryGetValue(empleadoActual, out var nomFinal))
                {
                    tiemposPorEmpleado[nomFinal] = tiempoFueraAcumulado;
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
                    string EscapeCsv(string value) =>
                        $"\"{(value ?? "").Replace("\"", "\"\"")}\"";

                    var csv = "Empleado,CI,TipoAlerta,FechaHora,Observaciones\n";
                    csv += string.Join("\n", alertas.Select(a =>
                        $"{EscapeCsv(a.Empleado)},{EscapeCsv(a.Ci)},{EscapeCsv(a.TipoAlerta)},{EscapeCsv(a.FechaHora)},{EscapeCsv(a.Observaciones)}"));

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

        [HttpGet("improductividad")]
        public async Task<ActionResult<List<ReporteImproductividadDTO>>> GenerarReporteImproductividad(
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta,
            [FromQuery] int toleranciaMinutosDiarios = 30,
            [FromQuery] int? empleadoId = null,
            [FromQuery] int? departamentoId = null,
            [FromQuery] int? lugarTrabajoId = null)
        {
            try
            {
                if (desde > hasta)
                    return BadRequest(new { message = "La fecha 'desde' no puede ser mayor a 'hasta'" });

                var utcDesde = DateTime.SpecifyKind(desde.Date, DateTimeKind.Utc);
                var utcHasta = DateTime.SpecifyKind(hasta.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                // 1. Obtener empleados filtrados
                var queryEmpleados = _context.Empleados
                    .Include(e => e.LugarTrabajoActual)
                        .ThenInclude(l => l.Departamento)
                    .Where(e => e.Activo)
                    .AsQueryable();

                if (empleadoId.HasValue)
                {
                    queryEmpleados = queryEmpleados.Where(e => e.Id == empleadoId.Value);
                }
                if (lugarTrabajoId.HasValue)
                {
                    queryEmpleados = queryEmpleados.Where(e => e.LugarTrabajoActualId == lugarTrabajoId.Value);
                }
                if (departamentoId.HasValue)
                {
                    queryEmpleados = queryEmpleados.Where(e => e.LugarTrabajoActual != null && e.LugarTrabajoActual.DepartamentoId == departamentoId.Value);
                }

                var empleados = await queryEmpleados.ToListAsync();
                var empleadoIds = empleados.Select(e => e.Id).ToList();

                // 2. Obtener días hábiles en el rango (Lunes a Viernes)
                var diasHabiles = new List<DateTime>();
                for (var date = utcDesde.Date; date <= utcHasta.Date; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        diasHabiles.Add(date);
                    }
                }

                // 3. Obtener asistencias registradas (tipo ENTRADA)
                var asistencias = await _context.RegistrosAsistencia
                    .Where(r => r.FechaHora >= utcDesde && r.FechaHora <= utcHasta && r.TipoRegistro == "ENTRADA" && empleadoIds.Contains(r.EmpleadoId))
                    .Select(r => new { r.EmpleadoId, Fecha = r.FechaHora.Date })
                    .Distinct()
                    .ToListAsync();

                var asistenciasMap = asistencias
                    .GroupBy(a => a.EmpleadoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Fecha).ToHashSet());

                // 4. Obtener ubicaciones y realizar cruce espacial PostGIS (ST_Within)
                var ubicaciones = await _context.Ubicaciones
                    .Where(u => u.FechaHora >= utcDesde && u.FechaHora <= utcHasta && empleadoIds.Contains(u.EmpleadoId))
                    .Where(u => u.Empleado.LugarTrabajoActual != null && u.Empleado.LugarTrabajoActual.Geocerca != null)
                    .Select(u => new
                    {
                        u.EmpleadoId,
                        u.FechaHora,
                        // PostGIS ST_Within
                        EstaDentro = u.UbicacionEmp.Within(u.Empleado.LugarTrabajoActual.Geocerca)
                    })
                    .OrderBy(u => u.EmpleadoId)
                    .ThenBy(u => u.FechaHora)
                    .ToListAsync();

                // 5. Agrupar ubicaciones por empleado y día para calcular tiempos
                var tiemposPorEmpleadoYDia = new Dictionary<int, Dictionary<DateTime, double>>(); // EmpleadoId -> Fecha -> MinutosFuera

                var ubicacionesPorEmpleado = ubicaciones.GroupBy(u => u.EmpleadoId);
                foreach (var empGroup in ubicacionesPorEmpleado)
                {
                    var empId = empGroup.Key;
                    tiemposPorEmpleadoYDia[empId] = new Dictionary<DateTime, double>();

                    var ubicacionesPorDia = empGroup.GroupBy(u => u.FechaHora.Date);
                    foreach (var diaGroup in ubicacionesPorDia)
                    {
                        var dia = diaGroup.Key;
                        var listaUbicaciones = diaGroup.OrderBy(u => u.FechaHora).ToList();
                        double minutosFuera = 0;

                        for (int i = 1; i < listaUbicaciones.Count; i++)
                        {
                            var prev = listaUbicaciones[i - 1];
                            var curr = listaUbicaciones[i];
                            
                            // Si el punto actual está fuera de la geocerca, consideramos que estuvo fuera durante el intervalo
                            if (!curr.EstaDentro)
                            {
                                var diff = (curr.FechaHora - prev.FechaHora).TotalMinutes;
                                // Limitar saltos de tiempo absurdos por si apagó el GPS por horas
                                if (diff > 0 && diff <= 30)
                                {
                                    minutosFuera += diff;
                                }
                                else if (diff > 30)
                                {
                                    minutosFuera += 5; // Estimación estándar de 5 minutos por transmisión faltante
                                }
                            }
                        }

                        tiemposPorEmpleadoYDia[empId][dia] = minutosFuera;
                    }
                }

                // 6. Consolidar el DTO de respuesta para cada empleado
                var reporteList = new List<ReporteImproductividadDTO>();

                foreach (var emp in empleados)
                {
                    var fechasAsistidas = asistenciasMap.ContainsKey(emp.Id) ? asistenciasMap[emp.Id] : new HashSet<DateTime>();
                    var inasistencias = diasHabiles.Where(d => !fechasAsistidas.Contains(d)).ToList();

                    double totalMinutosFuera = 0;
                    double totalMinutosPenalizables = 0;
                    double totalToleranciaAplicada = 0;

                    if (tiemposPorEmpleadoYDia.ContainsKey(emp.Id))
                    {
                        foreach (var kvp in tiemposPorEmpleadoYDia[emp.Id])
                        {
                            var minutosFueraDelDia = kvp.Value;
                            var penalizableDelDia = Math.Max(0, minutosFueraDelDia - toleranciaMinutosDiarios);
                            var toleranciaDelDia = minutosFueraDelDia > 0 ? Math.Min(minutosFueraDelDia, toleranciaMinutosDiarios) : 0;

                            totalMinutosFuera += minutosFueraDelDia;
                            totalMinutosPenalizables += penalizableDelDia;
                            totalToleranciaAplicada += toleranciaDelDia;
                        }
                    }

                    int totalMinutosFueraInt = (int)Math.Round(totalMinutosFuera);
                    int totalToleranciaInt = (int)Math.Round(totalToleranciaAplicada);
                    int totalPenalizablesInt = (int)Math.Round(totalMinutosPenalizables);

                    reporteList.Add(new ReporteImproductividadDTO
                    {
                        EmpleadoId = emp.Id,
                        EmpleadoNombre = $"{emp.Nombres} {emp.Paterno} {emp.Materno}".Trim(),
                        DepartamentoNombre = emp.LugarTrabajoActual?.Departamento?.Nombre ?? "Sin asignar",
                        LugarTrabajoNombre = emp.LugarTrabajoActual?.Nombre ?? "Sin asignar",
                        DiasInasistencia = inasistencias.Count,
                        FechasInasistencias = inasistencias.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
                        MinutosFueraGeocerca = totalMinutosFueraInt,
                        TiempoTotalFueraRuta = $"{totalMinutosFueraInt / 60}h {totalMinutosFueraInt % 60}m",
                        MinutosToleranciaAplicados = totalToleranciaInt,
                        TiempoToleranciaAplicado = $"{totalToleranciaInt / 60}h {totalToleranciaInt % 60}m",
                        MinutosPenalizables = totalPenalizablesInt,
                        TiempoNetoPenalizable = $"{totalPenalizablesInt / 60}h {totalPenalizablesInt % 60}m"
                    });
                }

                return Ok(reporteList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de inasistencias y tiempos improductivos");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }

}