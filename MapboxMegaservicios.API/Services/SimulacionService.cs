using MapboxMegaservicios.API.Data;
using MapboxMegaservicios.API.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapboxMegaservicios.API.Services
{
    public class SimulacionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimulacionService> _logger;
        private static bool _activo;
        private static int _intervaloSegundos = 8;
        private static readonly Random _rng = new();

        public static bool Activo => _activo;

        public static void Iniciar(int intervaloSegundos = 8)
        {
            _activo = true;
            _intervaloSegundos = Math.Max(3, intervaloSegundos);
        }

        public static void Detener() => _activo = false;

        public SimulacionService(IServiceScopeFactory scopeFactory, ILogger<SimulacionService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de simulación iniciado (inactivo)");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_activo)
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var empleados = await context.Empleados
                            .Where(e => e.Activo && e.Usuario != "admin")
                            .Include(e => e.LugarTrabajoActual)
                            .ToListAsync(stoppingToken);

                        if (empleados.Count == 0)
                        {
                            _logger.LogWarning("No hay empleados activos para simular");
                        }
                        else
                        {
                            var ubicaciones = new List<Ubicacion>();

                            foreach (var emp in empleados)
                            {
                                var state = ObtenerOInicializar(emp);
                                var (nuevaLat, nuevaLng) = CalcularNuevaPosicion(state);
                                var punto = new Point(nuevaLng, nuevaLat) { SRID = 4326 };

                                var estaEnGeocerca = emp.LugarTrabajoActual?.Geocerca?.Contains(punto);
                                if (state.EsPrimeraVez)
                                {
                                    state.EstadoAnterior = estaEnGeocerca ?? false;
                                    state.EsPrimeraVez = false;
                                }

                                var ubicacion = new Ubicacion
                                {
                                    EmpleadoId = emp.Id,
                                    UbicacionEmp = punto,
                                    FechaHora = DateTime.UtcNow,
                                    EstaEnGeocerca = estaEnGeocerca,
                                    IsPossibleSpoofing = false
                                };

                                ubicaciones.Add(ubicacion);
                            }

                            context.Ubicaciones.AddRange(ubicaciones);
                            await context.SaveChangesAsync(stoppingToken);

                            // Generar alertas para cambios de estado
                            foreach (var emp in empleados)
                            {
                                var ultimaUbicacion = ubicaciones.LastOrDefault(u => u.EmpleadoId == emp.Id);
                                if (ultimaUbicacion?.EstaEnGeocerca == null)
                                {
                                    _logger.LogInformation("⏭️ {Emp} sin geocerca, saltando alerta", $"{emp.Nombres} {emp.Paterno}");
                                    continue;
                                }
                                if (!SimulacionEstatica.Estados.TryGetValue(emp.Id, out var simState)) continue;

                                var estadoActual = ultimaUbicacion.EstaEnGeocerca.Value;
                                _logger.LogInformation("🔍 {Emp}: anterior={Ant} actual={Act}", $"{emp.Nombres} {emp.Paterno}", simState.EstadoAnterior, estadoActual);
                                if (simState.EstadoAnterior != estadoActual)
                                {
                                    simState.EstadoAnterior = estadoActual;

                                    var codigo = estadoActual ? "DENTRO" : "FUERA";
                                    var estado = await context.EstadosAlerta
                                        .FirstOrDefaultAsync(e => e.Codigo == codigo, stoppingToken);

                                    if (estado != null)
                                    {
                                        context.AlertasGeocerca.Add(new AlertaGeocerca
                                        {
                                            EmpleadoId = emp.Id,
                                            EstadoAlertaId = estado.Id,
                                            FechaHora = DateTime.UtcNow,
                                            Observaciones = estadoActual
                                                ? "Empleado ingresó al área de trabajo"
                                                : "Empleado salió del área de trabajo"
                                        });
                                        _logger.LogInformation("🚨 Alerta creada para {Emp}: {Codigo}", $"{emp.Nombres} {emp.Paterno}", codigo);
                                    }
                                }
                            }

                            await context.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation("🧪 Simulación: {Count} ubicaciones generadas", ubicaciones.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error en simulación");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(_intervaloSegundos), stoppingToken);
            }
        }

        private static SimulacionEstado ObtenerOInicializar(Empleado emp)
        {
            var key = emp.Id;
            if (SimulacionEstatica.Estados.TryGetValue(key, out var existente))
            {
                existente.Nombre = $"{emp.Nombres} {emp.Paterno}";
                return existente;
            }

            var lugar = emp.LugarTrabajoActual;
            double centroLat, centroLng;

            if (lugar?.Geocerca != null)
            {
                var coords = lugar.Geocerca.Centroid;
                centroLat = coords.Y;
                centroLng = coords.X;
            }
            else
            {
                centroLat = -16.5 + _rng.NextDouble() * 0.1;
                centroLng = -68.1 + _rng.NextDouble() * 0.1;
            }

            var estado = new SimulacionEstado
            {
                EmpleadoId = emp.Id,
                Nombre = $"{emp.Nombres} {emp.Paterno}",
                CentroLat = centroLat,
                CentroLng = centroLng,
                Lat = centroLat + (_rng.NextDouble() - 0.5) * 0.002,
                Lng = centroLng + (_rng.NextDouble() - 0.5) * 0.002,
                Angulo = _rng.NextDouble() * 2 * Math.PI,
                EsDeambulante = _rng.NextDouble() < 0.3,
                RadioMovimiento = 0.0005 + _rng.NextDouble() * 0.001,
                PasoBase = 0.00008 + _rng.NextDouble() * 0.0001,
                TicksDentroRestantes = _rng.Next(15, 40),
                TicksFueraRestantes = 0
            };

            SimulacionEstatica.Estados[key] = estado;
            return estado;
        }

        private static (double lat, double lng) CalcularNuevaPosicion(SimulacionEstado state)
        {
            state.Angulo += (_rng.NextDouble() - 0.5) * 0.6;

            double paso = state.PasoBase;

            if (state.EsDeambulante)
            {
                if (state.TicksFueraRestantes > 0)
                {
                    state.TicksFueraRestantes--;

                    var dirSalida = Math.Atan2(
                        state.Lat - state.CentroLat,
                        state.Lng - state.CentroLng
                    );

                    state.Angulo = state.Angulo * 0.3 + dirSalida * 0.7;
                    paso *= 1.5;
                }
                else
                {
                    state.TicksDentroRestantes--;

                    if (state.TicksDentroRestantes <= 0 && _rng.NextDouble() < 0.15)
                    {
                        state.TicksFueraRestantes = _rng.Next(5, 15);
                        state.TicksDentroRestantes = _rng.Next(20, 60);
                    }

                    var dirCentro = Math.Atan2(
                        state.CentroLat - state.Lat,
                        state.CentroLng - state.Lng
                    );

                    state.Angulo = state.Angulo * 0.7 + dirCentro * 0.3;
                }
            }
            else
            {
                var dirCentro = Math.Atan2(
                    state.CentroLat - state.Lat,
                    state.CentroLng - state.Lng
                );

                double dist = Math.Sqrt(
                    Math.Pow(state.Lat - state.CentroLat, 2) +
                    Math.Pow(state.Lng - state.CentroLng, 2)
                );

                if (dist > state.RadioMovimiento * 2)
                    state.Angulo = state.Angulo * 0.5 + dirCentro * 0.5;
                else
                    state.Angulo = state.Angulo * 0.85 + dirCentro * 0.15;
            }

            state.Lat += Math.Sin(state.Angulo) * paso;
            state.Lng += Math.Cos(state.Angulo) * paso;

            return (state.Lat, state.Lng);
        }
    }

    public static class SimulacionEstatica
    {
        public static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SimulacionEstado> Estados =
            new System.Collections.Concurrent.ConcurrentDictionary<int, SimulacionEstado>();
    }
}
