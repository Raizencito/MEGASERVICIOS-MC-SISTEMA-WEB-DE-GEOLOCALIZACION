// services/reportes.service.ts
import api from './api'

export interface FiltroReporte {
  departamentoId?: number
  lugarTrabajoId?: number
  empleadoId?: number
  desde?: string
  hasta?: string
  tipo?: string
}

export interface DatosReporte {
  totalEmpleados: number
  totalLugares: number
  totalHorasTrabajadas: number
  totalAlertas: number
  totalTiempoFuera: number
  porcentajeAsistencia: number
  periodo: string
  jornadas: any[]
  estadisticasPorDepartamento: any[]
}

class ReportesService {
  async obtenerAlertas(filtro: FiltroReporte): Promise<any> {
    const params = new URLSearchParams()
    if (filtro.desde) params.append('desde', filtro.desde)
    if (filtro.hasta) params.append('hasta', filtro.hasta)
    if (filtro.empleadoId) params.append('empleadoId', filtro.empleadoId.toString())
    if (filtro.departamentoId) params.append('departamentoId', filtro.departamentoId.toString())
    if (filtro.lugarTrabajoId) params.append('lugarTrabajoId', filtro.lugarTrabajoId.toString())

    const response = await api.get(`/admin/reportes/alertas?${params.toString()}`)
    return response.data
  }

  async obtenerTiemposFuera(filtro: FiltroReporte): Promise<any> {
    const params = new URLSearchParams()
    if (filtro.desde) params.append('desde', filtro.desde)
    if (filtro.hasta) params.append('hasta', filtro.hasta)
    if (filtro.empleadoId) params.append('empleadoId', filtro.empleadoId.toString())
    if (filtro.departamentoId) params.append('departamentoId', filtro.departamentoId.toString())
    if (filtro.lugarTrabajoId) params.append('lugarTrabajoId', filtro.lugarTrabajoId.toString())

    const response = await api.get(`/admin/reportes/tiempos-fuera?${params.toString()}`)
    return response.data
  }

  async obtenerAsistencia(filtro: FiltroReporte): Promise<any> {
    const params = new URLSearchParams()
    if (filtro.desde) params.append('desde', filtro.desde)
    if (filtro.hasta) params.append('hasta', filtro.hasta)
    if (filtro.departamentoId) params.append('departamentoId', filtro.departamentoId.toString())
    if (filtro.lugarTrabajoId) params.append('lugarTrabajoId', filtro.lugarTrabajoId.toString())

    const response = await api.get(`/admin/reportes/asistencia?${params.toString()}`)
    return response.data
  }

  async obtenerDepartamentos(): Promise<any[]> {
    const response = await api.get('/admin/lugares/departamentos')
    return response.data
  }

  async obtenerLugaresPorDepartamento(departamentoId: number): Promise<any[]> {
    const response = await api.get(`/admin/lugares`)
    return response.data.filter((l: any) => l.departamentoId === departamentoId)
  }

  async obtenerEmpleadosActivos(): Promise<any[]> {
    const response = await api.get('/admin/empleados')
    return response.data.filter((e: any) => e.activo)
  }
}

export default new ReportesService()
