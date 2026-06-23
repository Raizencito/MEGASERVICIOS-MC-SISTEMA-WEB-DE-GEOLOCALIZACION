using Microsoft.AspNetCore.SignalR;

namespace MapboxMegaservicios.API.Hubs
{
    /// <summary>
    /// Hub de SignalR para comunicación en tiempo real de ubicaciones y alertas.
    /// Los clientes del Dashboard se conectan aquí para recibir actualizaciones
    /// instantáneas sin necesidad de polling HTTP.
    /// </summary>
    public class UbicacionHub : Hub
    {
        private readonly ILogger<UbicacionHub> _logger;

        public UbicacionHub(ILogger<UbicacionHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("🔌 Cliente SignalR conectado: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("🔌 Cliente SignalR desconectado: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
