namespace MapboxMegaservicios.API.DTOs
{
    public class MarcarRegistroRequest
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    public class RegistroResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
    }
}