using MapboxMegaservicios.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapboxMegaservicios.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulacionController : ControllerBase
    {
        private readonly ILogger<SimulacionController> _logger;

        public SimulacionController(ILogger<SimulacionController> logger)
        {
            _logger = logger;
        }

        [HttpPost("iniciar")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Iniciar([FromQuery] int intervalo = 8)
        {
            SimulacionService.Iniciar(intervalo);
            _logger.LogInformation("Simulación iniciada (intervalo: {Intervalo}s)", intervalo);
            return Ok(new { message = "Simulación iniciada", intervalo, activo = true });
        }

        [HttpPost("detener")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Detener()
        {
            SimulacionService.Detener();
            _logger.LogInformation("Simulación detenida");
            return Ok(new { message = "Simulación detenida", activo = false });
        }

        [HttpGet("estado")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Estado()
        {
            var empleadosSimulados = SimulacionEstatica.Estados.Keys.Count;
            return Ok(new
            {
                activo = SimulacionService.Activo,
                empleadosSimulados,
                empleados = SimulacionEstatica.Estados.Values
                    .Select(e => new { e.EmpleadoId, e.Nombre, e.Lat, e.Lng })
                    .ToList()
            });
        }
    }
}
