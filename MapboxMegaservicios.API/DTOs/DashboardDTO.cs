namespace MapboxMegaservicios.API.DTOs
{
    public class DashboardEstadisticasDTO
    {
        public int TotalEmpleados { get; set; }
        public int EmpleadosActivos { get; set; }
        public int EmpleadosEnGeocerca { get; set; }
        public int EmpleadosFueraGeocerca { get; set; }
        public int AlertasHoy { get; set; }
        public int TotalLugares { get; set; }
        public int EmpleadosSinUbicacion { get; set; }
        public List<AlertaGeocercaDTO> UltimasAlertas { get; set; } = new();
    }
}