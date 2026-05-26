import api from './api'
import type { JornadaDTO, EmpleadoAsistenciaDTO } from '@/types'

export interface MarcarRequest {
  latitud: number
  longitud: number
}

export interface RegistroResponse {
  success: boolean
  message: string
  tipo: string
  fechaHora?: string
  jornadaId?: number
}

class AsistenciaService {
  // Marcar entrada
  async marcarEntrada(latitud: number, longitud: number): Promise<RegistroResponse> {
    const response = await api.post<RegistroResponse>('/asistencia/marcar-entrada', {
      latitud,
      longitud,
    })
    return response.data
  }

  // Marcar salida
  async marcarSalida(latitud: number, longitud: number): Promise<RegistroResponse> {
    const response = await api.post<RegistroResponse>('/asistencia/marcar-salida', {
      latitud,
      longitud,
    })
    return response.data
  }

  // Obtener mi jornada de hoy
  async obtenerMiJornadaHoy(): Promise<JornadaDTO> {
    const response = await api.get<JornadaDTO>('/asistencia/mi-jornada-hoy')
    return response.data
  }

  // Obtener mis asistencias (historial)
  async obtenerMisAsistencias(desde?: string, hasta?: string): Promise<JornadaDTO[]> {
    const params: any = {}
    if (desde) params.desde = desde
    if (hasta) params.hasta = hasta

    const response = await api.get<JornadaDTO[]>('/asistencia/mis-asistencias', { params })
    return response.data
  }

  // Obtener asistencia de todos los empleados hoy (admin)
  async obtenerAsistenciaHoy(): Promise<EmpleadoAsistenciaDTO[]> {
    const response = await api.get<EmpleadoAsistenciaDTO[]>('/asistencia/empleados/hoy')
    return response.data
  }

  // Generar datos de prueba (admin)
  async generarDatosPrueba(dias: number = 7): Promise<any> {
    const response = await api.post('/asistencia/generar-datos-prueba', null, {
      params: { dias },
    })
    return response.data
  }
}

export default new AsistenciaService()
