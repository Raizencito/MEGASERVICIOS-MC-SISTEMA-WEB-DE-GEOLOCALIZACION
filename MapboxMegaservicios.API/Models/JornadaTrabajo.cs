using MapboxMegaservicios.API.Models;

namespace MapboxMegaservicios.API.Models
{
    public class JornadaTrabajo
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? HoraEntrada { get; set; }
        public DateTime? HoraSalida { get; set; }
        public decimal? TotalHoras { get; set; }
        public string Estado { get; set; } = "PENDIENTE"; // "PENDIENTE" | "COMPLETADA" | "INCOMPLETA"
        public int TiempoFueraGeocerca { get; set; } = 0; // Minutos
        public int AlertasGeneradas { get; set; } = 0;

        // Navegación
        public virtual Empleado? Empleado { get; set; }
    }
}
