
using System.Collections.Generic;

namespace MapboxMegaservicios.API.Models
{
    public class Empleado
    {
        public int Id { get; set; }
        public string Paterno { get; set; } = string.Empty;
        public string Materno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        public int? LugarTrabajoActualId { get; set; }
        public int IdRol { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navigation properties (hacerlas nullable)
        public LugarTrabajo? LugarTrabajoActual { get; set; }
        public Rol? Rol { get; set; }
        public ICollection<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
        public ICollection<AlertaGeocerca> Alertas { get; set; } = new List<AlertaGeocerca>();

        public ICollection<HistorialLugarTrabajo> HistorialLugaresTrabajo { get; set; } = new List<HistorialLugarTrabajo>();
    }
}