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
  async generarReportePDF(filtro: FiltroReporte): Promise<Blob> {
    const response = await api.post('/reportes/generar-pdf', filtro, {
      responseType: 'blob',
    })
    return response.data
  }

  async generarReporteExcel(filtro: FiltroReporte): Promise<Blob> {
    const response = await api.post('/reportes/generar-excel', filtro, {
      responseType: 'blob',
    })
    return response.data
  }

  async obtenerDatosReporte(filtro: FiltroReporte): Promise<DatosReporte> {
    const response = await api.post('/reportes/datos-reporte', filtro)
    return response.data
  }

  async obtenerDepartamentos(): Promise<any[]> {
    const response = await api.get('/departamentos')
    return response.data
  }

  async obtenerLugaresPorDepartamento(departamentoId: number): Promise<any[]> {
    const response = await api.get(`/lugares/departamento/${departamentoId}`)
    return response.data
  }

  async obtenerEmpleadosActivos(): Promise<any[]> {
    const response = await api.get('/empleados/activos')
    return response.data
  }

  // Método para descargar reporte con nombre automático
  async descargarReportePDF(filtro: FiltroReporte, nombreBase: string = 'reporte') {
    const blob = await this.generarReportePDF(filtro)
    const fecha = new Date().toISOString().split('T')[0]
    const nombreArchivo = `${nombreBase}-${fecha}.pdf`

    this.descargarArchivo(blob, nombreArchivo)
  }

  private descargarArchivo(blob: Blob, nombreArchivo: string) {
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = nombreArchivo
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  }
}

export default new ReportesService()
