namespace MapboxMegaservicios.API.DTOs
{
    public class ReporteAlertasDTO
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int TotalAlertas { get; set; }
        public List<AlertaGeocercaDTO> Alertas { get; set; } = new();
        public Dictionary<string, int> AlertasPorTipo { get; set; } = new();
        public Dictionary<string, int> AlertasPorEmpleado { get; set; } = new();
    }

    public class ReporteAsistenciaDTO
    {
        public DateTime Fecha { get; set; }
        public int TotalEmpleados { get; set; }
        public int EmpleadosEnGeocerca { get; set; }
        public int EmpleadosFueraGeocerca { get; set; }
        public int AlertasDelDia { get; set; }
        public int EmpleadosSinUbicacion { get; set; }
        public List<DetalleAsistenciaDTO> Detalles { get; set; } = new();
    }

    public class DetalleAsistenciaDTO
    {
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string LugarTrabajo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime UltimaUbicacion { get; set; }
        public int AlertasHoy { get; set; }
    }

    public class HistorialLugarDTO
    {
        public int Id { get; set; }
        public string LugarTrabajo { get; set; } = string.Empty;
        public DateTime FechaCambio { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }

    public class ReporteTiemposFueraDTO
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public Dictionary<string, TimeSpan> TiemposPorEmpleado { get; set; } = new();
        public TimeSpan TotalTiempoFuera { get; set; }
    }
}