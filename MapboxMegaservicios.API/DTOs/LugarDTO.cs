using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

namespace MapboxMegaservicios.API.DTOs
{
    public class LugarTrabajoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int TotalEmpleados { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int DepartamentoId { get; set; } 
    }

    public class CrearLugarRequest
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es requerida")]
        [StringLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
        public string Direccion { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El departamento es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un departamento válido")]
        public int DepartamentoId { get; set; }  // ← ¡DEBE ESTAR AQUÍ!

        [Required(ErrorMessage = "La geocerca es requerida")]
        [MinLength(3, ErrorMessage = "Se requieren al menos 3 coordenadas para la geocerca")]
        public List<Coordinate> Coordenadas { get; set; } = new();
    }

    public class ActualizarLugarRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Direccion { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un departamento válido")]
        public int? DepartamentoId { get; set; }

        /// <summary>Coordenadas opcionales para actualizar la geocerca en la misma llamada</summary>
        public List<Coordinate>? Coordenadas { get; set; }
    }

    public class ActualizarGeocercaRequest
    {
        public List<Coordinate> Coordenadas { get; set; } = new();
    }

    /// <summary>DTO que incluye el GeoJSON de la geocerca para dibujar en el mapa</summary>
    public class LugarConGeocercaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int DepartamentoId { get; set; }
        public int TotalEmpleados { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? GeocercaGeoJSON { get; set; }
        public double? CentroLatitud { get; set; }
        public double? CentroLongitud { get; set; }
    }
}