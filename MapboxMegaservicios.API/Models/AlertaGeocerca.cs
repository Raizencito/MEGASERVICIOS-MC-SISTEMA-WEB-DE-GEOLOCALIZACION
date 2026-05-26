using System;
using System.Collections;
using System.Collections.Generic;

namespace MapboxMegaservicios.API.Models
{
    public class AlertaGeocerca
    {
        public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public int EstadoAlertaId { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public string? Observaciones { get; set; }

        // Navigation properties
        public Empleado Empleado { get; set; } = new();
        public EstadoAlerta EstadoAlerta { get; set; } = new();
    }
}