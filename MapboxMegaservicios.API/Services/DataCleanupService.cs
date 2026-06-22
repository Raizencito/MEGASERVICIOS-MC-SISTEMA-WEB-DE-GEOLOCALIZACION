using MapboxMegaservicios.API.Data;
using Microsoft.EntityFrameworkCore;

namespace MapboxMegaservicios.API.Services
{
    public class DataCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataCleanupService> _logger;
        private readonly int _daysToKeep = 30; // Guardar historial de ubicaciones puras por 30 días

        public DataCleanupService(IServiceProvider serviceProvider, ILogger<DataCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataCleanupService is starting.");

            // Ejecutar la primera vez inmediatamente (útil para pruebas)
            await DoWork(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Esperar 24 horas antes de volver a ejecutar
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                await DoWork(stoppingToken);
            }
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataCleanupService is working.");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var thresholdDate = DateTime.UtcNow.AddDays(-_daysToKeep);

                    // Eliminar registros de Ubicaciones más antiguos que el umbral
                    // NOTA: No borramos AlertasGeocerca, estas se mantienen para el historial
                    var deletedCount = await dbContext.Ubicaciones
                        .Where(u => u.FechaHora < thresholdDate)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation("DataCleanupService eliminó {DeletedCount} registros antiguos de Ubicaciones.", deletedCount);
                    }
                    else
                    {
                        _logger.LogInformation("DataCleanupService no encontró registros antiguos para eliminar.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ocurrido ejecutando DataCleanupService.");
            }
        }
    }
}
