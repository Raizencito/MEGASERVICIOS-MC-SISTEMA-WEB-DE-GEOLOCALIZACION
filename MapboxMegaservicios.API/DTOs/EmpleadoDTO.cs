using System;
using System.ComponentModel.DataAnnotations;
namespace MapboxMegaservicios.API.DTOs
{
    public class EmpleadoDTO
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Paterno { get; set; } = string.Empty;
        public string Materno { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string? LugarActual { get; set; }
        public DateTime? UltimaUbicacion { get; set; }
        public string? UltimoEstado { get; set; }
        public bool Activo { get; set; }
        

        // IDs para edición
        
        public int IdRol { get; set; }
        public int? IdLugarTrabajo { get; set; }

        // Información adicional para detalles
        public int TotalAlertas { get; set; }
        public TimeSpan TiempoFueraHoy { get; set; }
        public DateTime FechaCreacion { get; set; }


        public class CrearEmpleadoRequest
        {
            [Required(ErrorMessage = "El apellido paterno es requerido")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido paterno debe tener entre 2 y 50 caracteres")]
            public string Paterno { get; set; } = string.Empty;

            [StringLength(50, ErrorMessage = "El apellido materno no puede exceder 50 caracteres")]
            public string Materno { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los nombres son requeridos")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 100 caracteres")]
            public string Nombres { get; set; } = string.Empty;

            [Required(ErrorMessage = "El CI es requerido")]
            [RegularExpression(@"^\d{5,10}$", ErrorMessage = "El CI debe contener solo números (5-10 dígitos)")]
            public string Ci { get; set; } = string.Empty;

            [RegularExpression(@"^(|[67]\d{7})$", ErrorMessage = "El teléfono debe comenzar con 6 o 7 y tener 8 dígitos")]
            public string Telefono { get; set; } = string.Empty;

            public string? Usuario { get; set; }
            public string? Password { get; set; }

            [Required(ErrorMessage = "El rol es requerido")]
            [Range(1, int.MaxValue, ErrorMessage = "El ID del rol es inválido")]
            public int IdRol { get; set; }

            public int? IdLugarTrabajo { get; set; }
        }

        public class EmpleadoCreadoDTO
        {
            public int Id { get; set; }
            public string NombreCompleto { get; set; } = string.Empty;
            public string Usuario { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Telefono { get; set; } = string.Empty;
            public string Mensaje { get; set; } = string.Empty;
        }

        public class SimpleLugarRequest
        {
            public int? LugarTrabajoId { get; set; }
            public string? Observaciones { get; set; }
        }

        public class ToggleActivoRequest
        {
            public bool? Activo { get; set; } // Nullable para permitir toggle
        }
    }
}