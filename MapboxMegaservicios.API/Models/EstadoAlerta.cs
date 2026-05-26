using System;
using System.Collections;
using System.Collections.Generic;
using MapboxMegaservicios.API.Models;

public class EstadoAlerta
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public ICollection<AlertaGeocerca> Alertas { get; set; } = new List<AlertaGeocerca>();
        
}