export interface LugarTrabajo {
  id: number
  nombre: string
  direccion: string
  descripcion: string
  totalEmpleados: number
  activo: boolean
  fechaCreacion: string
  departamentoId: number
  geocercaGeoJSON?: string // Nuevo: GeoJSON de la geocerca
  centroLatitud?: number // Nuevo: centro de la geocerca
  centroLongitud?: number // Nuevo: centro de la geocerca
}

export interface Ubicacion {
  empleadoId: number
  empleadoNombre: string
  latitud?: number
  longitud?: number
  fechaHora?: string
  estaEnGeocerca?: boolean
  estado: string
  lugarTrabajo: string
  ultimoEstado: string // Nuevo: descripción del último estado
}

export interface DashboardStats {
  totalEmpleados?: number
  empleadosActivos?: number
  empleadosEnGeocerca?: number
  empleadosFueraGeocerca?: number
  alertasHoy?: number
  totalLugares?: number
  empleadosSinUbicacion?: number
  ultimasAlertas?: any[]
}

export interface Empleado {
  id: number
  nombreCompleto?: string
  paterno?: string
  materno?: string
  nombres?: string
  ci?: string
  usuario?: string
  telefono?: string
  rol?: string
  lugarActual?: string
  idLugarTrabajo?: number | null
  idRol?: number
  activo?: boolean
  fechaCreacion?: string
}

export interface Coordenada {
  x: number // longitud
  y: number // latitud
}

export interface CrearLugarRequest {
  nombre: string
  direccion: string
  descripcion?: string
  departamentoId: number
  coordenadas: Coordenada[]
}

export interface ActualizarGeocercaRequest {
  coordenadas: Coordenada[]
}

export interface JornadaDTO {
  id: number
  fecha: string
  horaEntrada?: string
  horaSalida?: string
  totalHoras?: number
  estado: string
  tiempoTrabajado?: string
  tiempoFueraGeocerca: number
  alertasGeneradas: number
  registros: RegistroAsistenciaDTO[]
  empleadoNombre?: string
  mensaje?: string
}

export interface RegistroAsistenciaDTO {
  id: number
  tipoRegistro: string
  fechaHora: string
  observaciones?: string
  verificado: boolean
  ubicacionCoords?: string
}

export interface EmpleadoAsistenciaDTO {
  empleadoId: number
  empleadoNombre: string
  ci: string
  lugarTrabajo: string
  estado: string
  horaEntrada?: string
  horaSalida?: string
  jornadaId?: number
  totalHoras?: number
}

// Nuevos tipos para el mapa
export interface LugarConGeocerca {
  id: number
  nombre: string
  direccion: string
  descripcion?: string
  totalEmpleados: number
  activo: boolean
  fechaCreacion: string
  departamentoId: number
  geocercaGeoJSON: string
  centroLatitud: number
  centroLongitud: number
}

export interface UbicacionEmpleadoMapa {
  empleadoId: number
  empleadoNombre: string
  latitud: number
  longitud: number
  estaEnGeocerca?: boolean
  fechaHora: string
  lugarTrabajoId?: number
}

export interface MapaCompleto {
  geocercas: string[]
  lugares: LugarMapa[]
  empleados: UbicacionEmpleadoMapa[]
}

export interface LugarMapa {
  id: number
  nombre: string
  empleadosCount: number
  departamento: string
}
