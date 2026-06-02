using System;
using System.Collections.Generic;

namespace MapboxMegaservicios.API.DTOs
{
    public class ReporteImproductividadDTO
    {
        public int EmpleadoId { get; set; }
        public string EmpleadoNombre { get; set; } = string.Empty;
        public string DepartamentoNombre { get; set; } = string.Empty;
        public string LugarTrabajoNombre { get; set; } = string.Empty;
        public int DiasInasistencia { get; set; }
        public List<string> FechasInasistencias { get; set; } = new();
        public int MinutosFueraGeocerca { get; set; }
        public string TiempoTotalFueraRuta { get; set; } = string.Empty; // Formato "Xh Ym"
        public int MinutosToleranciaAplicados { get; set; }
        public string TiempoToleranciaAplicado { get; set; } = string.Empty; // Formato "Xh Ym"
        public int MinutosPenalizables { get; set; }
        public string TiempoNetoPenalizable { get; set; } = string.Empty; // Formato "Xh Ym"
    }
}
