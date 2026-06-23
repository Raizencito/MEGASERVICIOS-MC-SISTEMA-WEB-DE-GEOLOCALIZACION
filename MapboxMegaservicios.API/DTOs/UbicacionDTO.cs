namespace MapboxMegaservicios.API.DTOs
{
    public class UbicacionDTO
    {
        public int EmpleadoId { get; set; }
        public string EmpleadoNombre { get; set; } = string.Empty;
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public DateTime FechaHora { get; set; }
        public bool? EstaEnGeocerca { get; set; }
        public string Estado { get; set; } = "Desconocido";
        public string LugarTrabajo { get; set; } = string.Empty;
        public bool IsPossibleSpoofing { get; set; }
    }

    public class RegistrarUbicacionRequest
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    public class SincronizarOfflineRequest
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public DateTime FechaHoraLocal { get; set; }
    }
}