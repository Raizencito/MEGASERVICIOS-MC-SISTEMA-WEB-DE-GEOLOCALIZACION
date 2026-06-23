import { HubConnectionBuilder, HubConnection, LogLevel, HttpTransportType } from '@microsoft/signalr'

let connection: HubConnection | null = null
const listeners: Map<string, Set<(...args: any[]) => void>> = new Map()

/**
 * Obtiene o crea la conexión SignalR al Hub de ubicaciones.
 * La conexión se reutiliza (singleton) para toda la app.
 */
export function getSignalRConnection(): HubConnection {
  if (connection) return connection

  const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5001/api'
  // La URL del Hub está en la raíz del servidor, no bajo /api
  const hubUrl = apiUrl.replace('/api', '') + '/hubs/ubicacion'

  connection = new HubConnectionBuilder()
    .withUrl(hubUrl, {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // Reintentar con backoff
    .configureLogging(LogLevel.Information)
    .build()

  connection.onreconnecting(() => {
    console.log('🔄 SignalR reconectando...')
  })

  connection.onreconnected(() => {
    console.log('✅ SignalR reconectado')
  })

  connection.onclose(() => {
    console.log('❌ SignalR desconectado')
  })

  return connection
}

/**
 * Inicia la conexión SignalR si no está activa.
 */
export async function startSignalR(): Promise<void> {
  const conn = getSignalRConnection()
  if (conn.state === 'Disconnected') {
    try {
      await conn.start()
      console.log('✅ SignalR conectado al Hub de ubicaciones')
    } catch (err) {
      console.error('❌ Error conectando SignalR:', err)
      // Reintentar después de 5 segundos
      setTimeout(() => startSignalR(), 5000)
    }
  }
}

/**
 * Detiene la conexión SignalR.
 */
export async function stopSignalR(): Promise<void> {
  if (connection) {
    await connection.stop()
    connection = null
    listeners.clear()
  }
}

/**
 * Registra un callback para un evento del Hub.
 * Permite múltiples listeners por evento (App.vue y MapboxMap.vue pueden escuchar simultáneamente).
 */
export function onSignalREvent(eventName: string, callback: (...args: any[]) => void): void {
  const conn = getSignalRConnection()
  
  if (!listeners.has(eventName)) {
    listeners.set(eventName, new Set())
    // Registrar el handler "real" una sola vez en SignalR
    conn.on(eventName, (...args: any[]) => {
      const cbs = listeners.get(eventName)
      if (cbs) {
        cbs.forEach(cb => cb(...args))
      }
    })
  }
  
  listeners.get(eventName)!.add(callback)
}

/**
 * Desregistra un callback de un evento.
 */
export function offSignalREvent(eventName: string, callback: (...args: any[]) => void): void {
  const cbs = listeners.get(eventName)
  if (cbs) {
    cbs.delete(callback)
  }
}
