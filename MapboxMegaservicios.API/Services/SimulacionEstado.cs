namespace MapboxMegaservicios.API.Services
{
    public class SimulacionEstado
    {
        public int EmpleadoId { get; set; }
        public string Nombre { get; set; } = "";
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double CentroLat { get; set; }
        public double CentroLng { get; set; }
        public double Angulo { get; set; }
        public bool EsDeambulante { get; set; }
        public int TicksFueraRestantes { get; set; }
        public int TicksDentroRestantes { get; set; }
        public bool EstadoAnterior { get; set; }
        public bool EsPrimeraVez { get; set; } = true;
        public double RadioMovimiento { get; set; } = 0.0008;
        public double PasoBase { get; set; } = 0.00012;
    }
}
