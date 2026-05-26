using MapboxMegaservicios.API.Models;

namespace MapboxMegaservicios.API.Models
{
    public class RegistroAsistencia
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string TipoRegistro { get; set; } = "ENTRADA";
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public int? UbicacionId { get; set; }
        public bool EsAutomatico { get; set; } = false;
        public string? Observaciones { get; set; }
        public bool Verificado { get; set; } = false;

        // Navegación
        public virtual Empleado? Empleado { get; set; }
        public virtual Ubicacion? Ubicacion { get; set; }
    }
}
