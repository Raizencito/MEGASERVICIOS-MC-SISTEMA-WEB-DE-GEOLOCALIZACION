namespace MapboxMegaservicios.API.DTOs
{
    public class ReporteAlertaDTO
    {
        public string Empleado { get; set; } = string.Empty;
        public string LugarTrabajo { get; set; } = string.Empty;
        public string TipoAlerta { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public TimeSpan Duracion { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }
}