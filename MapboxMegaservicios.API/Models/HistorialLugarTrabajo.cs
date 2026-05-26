using System;
using System.Collections.Generic;

namespace MapboxMegaservicios.API.Models
{
    public class HistorialLugarTrabajo
    {
        public int Id { get; set; }
        public int EmpleadoId{ get; set; }
        public int LugarTrabajoId { get; set; }
        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
        public string? Observaciones { get; set; }

        public Empleado? Empleado { get; set; }  // ← Cambiado
        public LugarTrabajo? LugarTrabajo { get; set; }  // ← Cambiado
    }
}