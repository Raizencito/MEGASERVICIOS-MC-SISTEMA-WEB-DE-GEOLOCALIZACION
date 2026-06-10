using NetTopologySuite.Geometries;
using System.Collections.Generic;
namespace MapboxMegaservicios.API.Models
{
    public class LugarTrabajo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public Polygon? Geocerca { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public int DepartamentoId { get; set; }
        public Departamento? Departamento { get; set; } 

        // Navigation properties
        public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
        public ICollection<HistorialLugarTrabajo> Historial { get; set; } = new List<HistorialLugarTrabajo>();
    }
}