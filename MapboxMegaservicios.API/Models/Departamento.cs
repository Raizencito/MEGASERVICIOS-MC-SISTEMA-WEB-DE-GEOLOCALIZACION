using MapboxMegaservicios.API.Models;
using System;
using System.Collections;
using System.Collections.Generic;
public class Departamento
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public ICollection<LugarTrabajo> LugaresTrabajo { get; set; } = new List<LugarTrabajo>();
}