namespace MapboxMegaservicios.API.DTOs
{
    public class AlertaGeocercaDTO
    {
        public int Id { get; set; }
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string TipoAlerta { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }
}