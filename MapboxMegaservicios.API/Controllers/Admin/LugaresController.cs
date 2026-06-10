using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.DTOs;
using MapboxMegaservicios.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.ComponentModel.DataAnnotations;


namespace MapboxMegaservicios.API.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/admin/[controller]")]
    public class LugaresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LugaresController> _logger;

        public LugaresController(ApplicationDbContext context, ILogger<LugaresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<LugarTrabajoDTO>>> ObtenerTodos()
        {
            try
            {
                var lugares = await _context.LugaresTrabajo
                    .Where(l => l.Activo)
                    .Select(l => new LugarTrabajoDTO
                    {
                        Id = l.Id,
                        Nombre = l.Nombre,
                        Direccion = l.Direccion,
                        Descripcion = l.Descripcion,
                        DepartamentoId = l.DepartamentoId,
                        TotalEmpleados = l.Empleados.Count(e => e.Activo),
                        Activo = l.Activo,
                        FechaCreacion = l.FechaCreacion
                    })
                    .OrderBy(l => l.Nombre)
                    .ToListAsync();

                return Ok(lugares);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo lugares de trabajo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>Devuelve todos los lugares CON su GeoJSON para dibujar geocercas en el mapa</summary>
        [HttpGet("geocercas")]
        public async Task<ActionResult<List<LugarConGeocercaDTO>>> ObtenerTodosConGeocercas()
        {
            try
            {
                var writer = new GeoJsonWriter();
                var lugares = await _context.LugaresTrabajo
                    .Where(l => l.Activo)
                    .Include(l => l.Empleados)
                    .OrderBy(l => l.Nombre)
                    .ToListAsync();

                var resultado = lugares.Select(l =>
                {
                    string? geoJson = null;
                    double? centroLat = null;
                    double? centroLng = null;

                    if (l.Geocerca != null && !l.Geocerca.IsEmpty)
                    {
                        geoJson = writer.Write(l.Geocerca);
                        var centroid = l.Geocerca.Centroid;
                        centroLat = centroid.Y;
                        centroLng = centroid.X;
                    }

                    return new LugarConGeocercaDTO
                    {
                        Id = l.Id,
                        Nombre = l.Nombre,
                        Direccion = l.Direccion,
                        Descripcion = l.Descripcion,
                        DepartamentoId = l.DepartamentoId,
                        TotalEmpleados = l.Empleados.Count(e => e.Activo),
                        Activo = l.Activo,
                        FechaCreacion = l.FechaCreacion,
                        GeocercaGeoJSON = geoJson,
                        CentroLatitud = centroLat,
                        CentroLongitud = centroLng
                    };
                }).ToList();

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo lugares con geocercas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LugarTrabajoDTO>> ObtenerPorId(int id)
        {
            try
            {
                var lugar = await _context.LugaresTrabajo
                    .Where(l => l.Id == id && l.Activo)
                    .Select(l => new LugarTrabajoDTO
                    {
                        Id = l.Id,
                        Nombre = l.Nombre,
                        Direccion = l.Direccion,
                        Descripcion = l.Descripcion,
                        TotalEmpleados = l.Empleados.Count(e => e.Activo),
                        Activo = l.Activo,
                        FechaCreacion = l.FechaCreacion
                    })
                    .FirstOrDefaultAsync();

                if (lugar == null)
                {
                    return NotFound(new { message = "Lugar de trabajo no encontrado" });
                }

                return Ok(lugar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo lugar de trabajo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<LugarTrabajoDTO>> Crear([FromBody] CrearLugarRequest request)
        {
            try
            {
                // 1. Validar que el departamento existe
                var departamentoExiste = await _context.Departamentos
                    .AnyAsync(d => d.Id == request.DepartamentoId);

                if (!departamentoExiste)
                {
                    return BadRequest(new
                    {
                        message = "El departamento seleccionado no existe",
                        departamentoId = request.DepartamentoId
                    });
                }

                // 2. Validar nombre único
                var existe = await _context.LugaresTrabajo
                    .AnyAsync(l => l.Nombre.ToLower() == request.Nombre.ToLower() && l.Activo);

                if (existe)
                    return BadRequest(new { message = "Ya existe un lugar con ese nombre" });

                // 3. Crear polígono
                if (request.Coordenadas == null || request.Coordenadas.Count < 3)
                    return BadRequest(new { message = "Se requieren al menos 3 coordenadas" });

                var polygon = CreatePolygonFromCoordinates(request.Coordenadas);

                // 4. Crear lugar con departamento
                var lugar = new LugarTrabajo
                {
                    Nombre = request.Nombre.Trim(),
                    Direccion = request.Direccion.Trim(),
                    Descripcion = request.Descripcion?.Trim(),
                    DepartamentoId = request.DepartamentoId,
                    Geocerca = polygon,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                _context.LugaresTrabajo.Add(lugar);
                await _context.SaveChangesAsync();

                return Ok(new LugarTrabajoDTO
                {
                    Id = lugar.Id,
                    Nombre = lugar.Nombre,
                    Direccion = lugar.Direccion,
                    Descripcion = lugar.Descripcion,
                    DepartamentoId = lugar.DepartamentoId,
                    TotalEmpleados = 0,
                    Activo = lugar.Activo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando lugar de trabajo");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Actualizar(int id, [FromBody] ActualizarLugarRequest request)
        {
            try
            {
                var lugar = await _context.LugaresTrabajo.FindAsync(id);
                if (lugar == null || !lugar.Activo)
                    return NotFound(new { message = "Lugar de trabajo no encontrado" });

                if (string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new { message = "El nombre es requerido" });

                // Verificar duplicados (excluyendo el actual)
                var nombreExistente = await _context.LugaresTrabajo
                    .AnyAsync(l => l.Id != id &&
                                  l.Nombre.Trim().ToLower() == request.Nombre.Trim().ToLower() &&
                                  l.Activo);

                if (nombreExistente)
                    return BadRequest(new { message = "Ya existe un lugar de trabajo con este nombre" });

                lugar.Nombre = request.Nombre.Trim();
                lugar.Direccion = request.Direccion.Trim();
                lugar.Descripcion = request.Descripcion?.Trim();

                // Actualizar departamento si se proporcionó
                if (request.DepartamentoId.HasValue)
                {
                    var deptoExiste = await _context.Departamentos.AnyAsync(d => d.Id == request.DepartamentoId.Value);
                    if (!deptoExiste)
                        return BadRequest(new { message = "Departamento no válido" });
                    lugar.DepartamentoId = request.DepartamentoId.Value;
                }

                // Actualizar geocerca si se proporcionaron coordenadas
                if (request.Coordenadas != null && request.Coordenadas.Count >= 3)
                {
                    var polygon = CreatePolygonFromCoordinates(request.Coordenadas);
                    lugar.Geocerca = polygon;
                    _logger.LogInformation("📍 Geocerca actualizada junto con el lugar: {Id}", id);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Lugar de trabajo actualizado: {Id} - {Nombre}", id, lugar.Nombre);
                return Ok(new { message = "Lugar de trabajo actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando lugar de trabajo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Eliminar(int id)
        {
            try
            {
                var lugar = await _context.LugaresTrabajo
                    .Include(l => l.Empleados)
                    .FirstOrDefaultAsync(l => l.Id == id && l.Activo);

                if (lugar == null)
                    return NotFound(new { message = "Lugar de trabajo no encontrado" });

                // Verificar si hay empleados asignados
                if (lugar.Empleados.Any(e => e.Activo))
                {
                    return BadRequest(new
                    {
                        message = "No se puede eliminar el lugar porque tiene empleados asignados",
                        empleadosAsignados = lugar.Empleados.Count(e => e.Activo)
                    });
                }

                lugar.Activo = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Lugar de trabajo eliminado: {Id} - {Nombre}", id, lugar.Nombre);
                return Ok(new { message = "Lugar de trabajo eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando lugar de trabajo {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{id}/geocerca")]
        public async Task<ActionResult<string>> ObtenerGeocercaGeoJSON(int id)
        {
            try
            {
                var lugar = await _context.LugaresTrabajo
                    .Where(l => l.Id == id && l.Activo)
                    .Select(l => l.Geocerca)
                    .FirstOrDefaultAsync();

                if (lugar == null)
                    return NotFound(new { message = "Lugar de trabajo no encontrado" });

                var writer = new GeoJsonWriter();
                var geojson = writer.Write(lugar);

                return Content(geojson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo geocerca para lugar {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPut("{id}/geocerca")]
        public async Task<ActionResult> ActualizarGeocerca(int id, [FromBody] ActualizarGeocercaRequest request)
        {
            try
            {
                var lugar = await _context.LugaresTrabajo.FindAsync(id);
                if (lugar == null || !lugar.Activo)
                    return NotFound(new { message = "Lugar de trabajo no encontrado" });

                if (request.Coordenadas == null || request.Coordenadas.Count < 3)
                    return BadRequest(new { message = "Se requieren al menos 3 coordenadas para la geocerca" });

                var polygon = CreatePolygonFromCoordinates(request.Coordenadas);
                lugar.Geocerca = polygon;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Geocerca actualizada para lugar: {Id} - {Nombre}", id, lugar.Nombre);
                return Ok(new { message = "Geocerca actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando geocerca para lugar {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{id}/empleados")]
        public async Task<ActionResult<List<EmpleadoDTO>>> ObtenerEmpleadosAsignados(int id)
        {
            try
            {
                var lugarExiste = await _context.LugaresTrabajo
                    .AnyAsync(l => l.Id == id && l.Activo);

                if (!lugarExiste)
                    return NotFound(new { message = "Lugar de trabajo no encontrado" });

                var lugar = await _context.LugaresTrabajo
                    .Where(l => l.Id == id)
                    .Select(l => l.Nombre)
                    .FirstOrDefaultAsync();

                var empleados = await _context.Empleados
                    .Where(e => e.Activo && e.LugarTrabajoActualId == id)
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
                        LugarActual = lugar ?? "Sin asignar",
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
                _logger.LogError(ex, "Error obteniendo empleados asignados al lugar {Id}", id);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("buscar")]
        public async Task<ActionResult<List<LugarTrabajoDTO>>> BuscarLugares([FromQuery] string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino))
                    return Ok(new List<LugarTrabajoDTO>());

                var lugares = await _context.LugaresTrabajo
                    .Where(l => l.Activo &&
                           (l.Nombre.Contains(termino) ||
                            l.Direccion.Contains(termino) ||
                            l.Descripcion != null && l.Descripcion.Contains(termino)))
                    .Take(20)
                    .Select(l => new LugarTrabajoDTO
                    {
                        Id = l.Id,
                        Nombre = l.Nombre,
                        Direccion = l.Direccion,
                        Descripcion = l.Descripcion,
                        TotalEmpleados = l.Empleados.Count(e => e.Activo),
                        Activo = l.Activo,
                        FechaCreacion = l.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(lugares);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando lugares con término: {Termino}", termino);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // MÉTODOS AUXILIARES
        private Polygon CreatePolygonFromCoordinates(List<Coordinate> coordinates)
        {
            if (coordinates == null || coordinates.Count < 3)
                throw new ArgumentException("Se requieren al menos 3 coordenadas");

            // Asegurar que el polígono se cierre (primera coordenada = última)
            if (!coordinates.First().Equals2D(coordinates.Last()))
            {
                coordinates.Add(new Coordinate(coordinates[0].X, coordinates[0].Y));
            }

            return new Polygon(new LinearRing(coordinates.ToArray())) { SRID = 4326 };
        }

        private string NormalizarDireccion(string direccion)
        {
            if (string.IsNullOrWhiteSpace(direccion))
                return "";

            // 1. Minúsculas
            var normalizada = direccion.Trim().ToLower();

            // 2. Reemplazar abreviaciones comunes
            normalizada = normalizada
                .Replace("av.", "avenida")
                .Replace("av ", "avenida ")
                .Replace("calle.", "calle")
                .Replace("nro.", "número")
                .Replace("nº", "número")
                .Replace("#", "número")
                .Replace("  ", " ");  // Doble espacio a simple

            // 3. Quitar caracteres especiales (opcional)
            normalizada = System.Text.RegularExpressions.Regex.Replace(normalizada, @"[^\w\sñáéíóú]", "");

            return normalizada.Trim();
        }
    }
}