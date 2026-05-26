import api from './api'
import type { Empleado } from '@/types'

class EmpleadosService {
  async getAll(): Promise<Empleado[]> {
    const response = await api.get<Empleado[]>('/admin/empleados')
    return response.data
  }

  async getById(id: number): Promise<Empleado> {
    const response = await api.get<Empleado>(`/admin/empleados/${id}`)
    return response.data
  }

  async create(empleado: any): Promise<any> {
    const response = await api.post('/admin/empleados', empleado)
    return response.data
  }

  async update(id: number, empleado: any): Promise<any> {
    const response = await api.put(`/admin/empleados/${id}`, empleado)
    return response.data
  }

  async delete(id: number): Promise<boolean> {
    await api.delete(`/admin/empleados/${id}`)
    return true
  }
}

export default new EmpleadosService()
