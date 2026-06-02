using NetTopologySuite.Geometries;
using System;
using System.Collections;
using System.Collections.Generic;
namespace MapboxMegaservicios.API.Models
{
    public class Ubicacion
    {
        public int Id { get; set; }
        public int EmpleadoId{ get; set; }
        public Point UbicacionEmp { get; set; } = new Point(0, 0);
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public bool? EstaEnGeocerca { get; set; }
        public bool IsPossibleSpoofing { get; set; } = false;

        // Navigation properties
        public Empleado Empleado { get; set; } = new();
    }
}