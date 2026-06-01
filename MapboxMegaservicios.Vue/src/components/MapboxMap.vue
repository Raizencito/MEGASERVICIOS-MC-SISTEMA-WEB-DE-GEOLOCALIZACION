[file name]: MapboxMap.vue [file content begin]
<template>
  <div>
    <!-- Contenedor del Mapa -->
    <div ref="mapContainer" class="map-container"></div>

    <!-- Leyenda del mapa -->
    <v-alert type="info" variant="outlined" class="mt-2" density="compact">
      <div class="d-flex align-center gap-4 flex-wrap">
        <div class="d-flex align-center gap-2">
          <div class="legend-color polygon-color"></div>
          <span>Geocercas</span>
        </div>
        <div class="d-flex align-center gap-2">
          <div class="legend-color empleado-dentro"></div>
          <span>En geocerca</span>
        </div>
        <div class="d-flex align-center gap-2">
          <div class="legend-color empleado-fuera"></div>
          <span>Fuera de geocerca</span>
        </div>
        <div class="d-flex align-center gap-2">
          <div class="legend-color empleado-sin-ubicacion"></div>
          <span>Sin ubicación</span>
        </div>
      </div>
    </v-alert>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed, nextTick } from 'vue'
import mapboxgl from 'mapbox-gl'
import * as turf from '@turf/turf'
import 'mapbox-gl/dist/mapbox-gl.css'
import type { LugarTrabajo, Ubicacion } from '@/types'
import api from '@/services/api'

// Props
const props = defineProps<{
  coordenadasIniciales?: Array<{ lng: number; lat: number }>
  lugarId?: number
  modo?: string
  departamentoCentro?: { lng: number; lat: number } | null
  mostrarLugares?: boolean // Nuevo: si mostrar geocercas existentes
  mostrarEmpleados?: boolean // Nuevo: si mostrar empleados
}>()

// Emits
const emit = defineEmits<{
  geocercaGuardada: [geojson: any]
  puntosCambiados: [puntos: Array<{ lng: number; lat: number }>]
  lugarSeleccionado: [lugar: LugarTrabajo]
  empleadoSeleccionado: [empleado: any]
}>()

// Referencias
let marcadoresPines: mapboxgl.Marker[] = []
const mapContainer = ref<HTMLElement>()
const map = ref<mapboxgl.Map>()
const puntos = ref<Array<{ lng: number; lat: number }>>([])
const modo = ref('dibujo')
const guardando = ref(false)
const editandoGeocerca = ref(false)
const mapaCargado = ref(false) 
const empleadoSimuladoId = ref<number | null>(null)
let pollingInterval: any = null // Intervalo para polling
let simulationMarker: mapboxgl.Marker | null = null // Marcador arrastrable

// Datos del mapa
const lugares = ref<LugarTrabajo[]>([])
const empleados = ref<any[]>([])

const ubicaciones = ref<Ubicacion[]>([])
// Estado previo para detectar cambios
// const ubicacionesPrevias = ref<Map<number, boolean | null>>(new Map()) // Eliminado: Polling global en App.vue maneja alertas

// Capas visibles
const capasVisibles = ref(['geocercas', 'empleados', 'lugar-actual'])
const filtroEstadoEmpleado = ref<string>('')

// Configuración Mapbox
mapboxgl.accessToken = import.meta.env.VITE_MAPBOX_TOKEN

// Estados para filtro
const estadosEmpleado = [
  { title: 'Dentro de geocerca', value: 'dentro' },
  { title: 'Fuera de geocerca', value: 'fuera' },
  { title: 'Sin ubicación', value: 'sin-ubicacion' },
  { title: 'Todos', value: '' },
]

// Computed
const hayGeocerca = computed(() => puntos.value.length >= 3)
const estadoGeocerca = computed(() => {
  if (puntos.value.length < 3) return 'warning'
  if (puntos.value.length >= 3) return 'success'
  return 'default'
})

const empleadosActivosEnMapa = computed(() => {
  return ubicaciones.value.filter((ubicacion) => {
    if (filtroEstadoEmpleado.value === '') return true
    if (filtroEstadoEmpleado.value === 'dentro') return ubicacion.estaEnGeocerca
    if (filtroEstadoEmpleado.value === 'fuera')
      return !ubicacion.estaEnGeocerca && ubicacion.latitud
    if (filtroEstadoEmpleado.value === 'sin-ubicacion') return !ubicacion.latitud
    return true
  })
})

// Watch para cambiar modo (interno o desde props)
watch(
  () => props.modo,
  (nuevoModo) => {
    if (nuevoModo) {
      modo.value = nuevoModo
    }
  },
)

// Watch para cambio de modo interno
watch(modo, (nuevoModo) => {
  console.log(`🔄 Cambio de modo: ${nuevoModo}`)
  
  if (nuevoModo === 'dibujo') {
    editandoGeocerca.value = true
    if (map.value) map.value.getCanvas().style.cursor = 'crosshair'
  } else if (modo.value === 'simular') {
    editandoGeocerca.value = false
    if (map.value) map.value.getCanvas().style.cursor = 'crosshair' // También crosshair para apuntar
  } else {
    editandoGeocerca.value = false
    if (map.value) map.value.getCanvas().style.cursor = ''
  }
}, { immediate: true })

// Watch para empleado simulado: poner marcador arrastrable
watch(empleadoSimuladoId, (newId) => {
  if (simulationMarker) simulationMarker.remove()
  
  if (newId && modo.value === 'simular') {
    const empleado = ubicaciones.value.find(u => u.empleadoId === newId)
    if (empleado && empleado.latitud && empleado.longitud) {
      // Crear marcador arrastrable
      const el = document.createElement('div')
      el.className = 'simulation-marker'
      el.style.cssText = `width: 30px; height: 30px; background-color: purple; border: 3px solid white; border-radius: 50%; cursor: move; box-shadow: 0 0 10px rgba(0,0,0,0.5); z-index: 1000;`
      
      simulationMarker = new mapboxgl.Marker({ element: el, draggable: true })
        .setLngLat([empleado.longitud, empleado.latitud])
        .addTo(map.value!)
        
      simulationMarker.on('dragend', () => {
         const lngLat = simulationMarker!.getLngLat()
         simularMovimiento(lngLat)
      })
    }
  }
})

// Watch para capas visibles
watch(capasVisibles, () => {
  if (mapaCargado.value) {
    actualizarCapas()
  }
})

// Watch para dibujado automático de empleados cuando lleguen datos
watch(ubicaciones, (nuevasUbicaciones) => {
  if (nuevasUbicaciones.length > 0 && mapaCargado.value && capasVisibles.value.includes('empleados')) {
     console.log('🔄 Datos de empleados actualizados. Redibujando...')
     nextTick(() => {
        actualizarMarcadoresEmpleados()
     })
  }
})

// Watch para filtro de empleados
watch(filtroEstadoEmpleado, () => {
  if (mapaCargado.value) {
    actualizarMarcadoresEmpleados()
  }
})

// Watch para dibujar automáticamente cuando lleguen los lugares
watch(lugares, (nuevosLugares) => {
  if (nuevosLugares.length > 0 && mapaCargado.value) {
    console.log('🔄 Detectados nuevos lugares. Dibujando automáticamente...')
    nextTick(() => {
      dibujarGeocercasExistentes()
    })
  }
})

// Watch para mapa cargado
watch(mapaCargado, (cargado) => {
  if (cargado && lugares.value.length > 0) {
    console.log('🔄 Mapa recién cargado con lugares listos. Dibujando...')
    dibujarGeocercasExistentes()
  }
})

// Inicializar mapa
onMounted(() => {
  pollingInterval = setInterval(async () => {
    if (!editandoGeocerca.value) { // No actualizar si estamos editando geocerca intenso
       await cargarUbicacionesEmpleados()
       if (props.mostrarEmpleados !== false) actualizarMarcadoresEmpleados()
    }
  }, 5000)

  if (!mapContainer.value) return

  // Crear mapa
  map.value = new mapboxgl.Map({
    container: mapContainer.value,
    style: 'mapbox://styles/mapbox/streets-v12',
    center: [-68.119, -16.489], // La Paz, Bolivia
    zoom: 14,
    attributionControl: false,
  })

  // Agregar controles
  map.value.addControl(new mapboxgl.NavigationControl(), 'top-right')
  map.value.addControl(new mapboxgl.ScaleControl(), 'bottom-left')
  map.value.addControl(new mapboxgl.FullscreenControl(), 'top-right')

  // Esperar a que el mapa cargue completamente
  map.value.on('load', async () => {
    console.log('✅ Mapa cargado completamente')
    mapaCargado.value = true

    // Cargar datos una vez que el mapa esté listo
    await cargarDatos()

    // Cargar geocerca existente si hay coordenadas iniciales
    if (props.coordenadasIniciales && props.coordenadasIniciales.length > 0) {
      puntos.value = props.coordenadasIniciales
      dibujarGeocercaEditada()
      editandoGeocerca.value = true
    }
  })

  // Eventos de zoom
  map.value.on('zoom', () => {
    if (mapaCargado.value) {
      actualizarCapas()
    }
  })
  map.value.on('zoomend', () => {
    if (mapaCargado.value) {
      actualizarCapas()
    }
  })

  // Evento click unificado
  map.value.on('click', (e) => {
    if (!mapaCargado.value) return

    if (modo.value === 'dibujo' && editandoGeocerca.value) {
      agregarPunto(e)
    } else if (modo.value === 'simular') {
      simularMovimiento(e.lngLat)
    }
  })

  // Evento para cerrar todos los popups al hacer clic en el mapa
  map.value.on('click', () => {
    const popups = document.querySelectorAll('.mapboxgl-popup')
    popups.forEach((popup) => popup.remove())
  })
})

// Limpiar al desmontar
onUnmounted(() => {
  if (pollingInterval) clearInterval(pollingInterval)
  if (map.value) {
    map.value.remove()
  }
})

// Lista simple de empleados para el selector
const listaEmpleados = computed(() => {
  return ubicaciones.value.map(u => ({ 
    id: u.empleadoId, 
    nombre: u.empleadoNombre 
  }))
})

// Función para simular movimiento
async function simularMovimiento(lngLat: { lng: number; lat: number }) {
  if (!empleadoSimuladoId.value) {
     alert('⚠️ Primero selecciona un empleado del selector amarillo')
     return
  }
  
  try {
     const nombre = listaEmpleados.value.find((e: any) => e.id === empleadoSimuladoId.value)?.nombre
     
     // Feedback visual inmediato
     if (map.value) {
        new mapboxgl.Popup({ closeButton: false, closeOnClick: true })
            .setLngLat(lngLat)
            .setHTML(`<div style="color:black; font-weight:bold">📍 Moviendo a ${nombre}...</div>`)
            .addTo(map.value)
     }

     console.log('🎮 Simulando movimiento...', { id: empleadoSimuladoId.value, ...lngLat })

     await api.post('/ubicaciones/simular', {
        empleadoId: empleadoSimuladoId.value,
        latitud: lngLat.lat,
        longitud: lngLat.lng
     })
     
     // Recargar ubicaciones para ver el resultado real
     await cargarUbicacionesEmpleados()
     
     if (props.mostrarEmpleados !== false) {
        actualizarMarcadoresEmpleados()
     }
     
     console.log('✅ Simulación completada')
  
  } catch(e: any) {
     console.error('❌ Error simulando:', e)
     alert('Error al simular ubicación: ' + (e.response?.data?.message || e.message))
  }
}

// Cargar todos los datos
async function cargarDatos() {
  try {
    console.log('📡 Cargando datos del mapa...')

    // Cargar datos en paralelo
    await Promise.all([cargarLugares(), cargarUbicacionesEmpleados()])

    console.log('✅ Datos cargados exitosamente')

    // Dibujar elementos en el mapa (mostrar por defecto si el prop no está definido)
    const deberaMostrarLugares = props.mostrarLugares !== false
    const deberaMostrarEmpleados = props.mostrarEmpleados !== false

    console.log(`🎨 Mostrar lugares: ${deberaMostrarLugares}, Mostrar empleados: ${deberaMostrarEmpleados}`)

    if (deberaMostrarLugares) {
      dibujarGeocercasExistentes()
    }

    if (deberaMostrarEmpleados) {
      dibujarEmpleados()
    }
  } catch (error) {
    console.error('❌ Error cargando datos:', error)
  }
}

// Cargar lugares de trabajo CON geocercas
async function cargarLugares() {
  try {
    console.log('📡 Cargando lugares con geocercas...')
    const response = await api.get('/admin/lugares/geocercas')
    lugares.value = response.data
    console.log(`✅ Cargados ${lugares.value.length} lugares con geocercas`)
  } catch (error) {
    console.error('❌ Error cargando lugares con geocercas:', error)
    // Fallback: cargar lugares básicos si el endpoint nuevo falla
    try {
      const response = await api.get('/admin/lugares')
      lugares.value = response.data
      console.log(`✅ Cargados ${lugares.value.length} lugares básicos`)
    } catch (error2) {
      console.error('❌ Error cargando lugares básicos:', error2)
    }
  }
}

// Cargar ubicaciones de empleados
async function cargarUbicacionesEmpleados() {
  try {
    console.log('📡 Cargando ubicaciones de empleados...')
    const response = await api.get('/ubicaciones/ultimas')
    const nuevasUbicaciones = response.data
    
    // DETECCIÓN DE CAMBIOS LOCAL ELIMINADA (Ahora es Global en App.vue)

    ubicaciones.value = nuevasUbicaciones
    console.log(`✅ Cargadas ${ubicaciones.value.length} ubicaciones de empleados`)
  } catch (error) {
    console.error('❌ Error cargando ubicaciones de empleados:', error)
  }
}

// Dibujar geocercas existentes
function dibujarGeocercasExistentes() {
  if (!map.value || !mapaCargado.value || !lugares.value.length) {
    console.warn('⚠️ Mapa no listo o sin lugares para dibujar')
    return
  }

  // 1. Procesar Features
  const features: any[] = []
  const centroidFeatures: any[] = []
  
  lugares.value.forEach((lugar) => {
    if (!lugar.geocercaGeoJSON) return

    try {
      const parsed = JSON.parse(lugar.geocercaGeoJSON)
      let geometry = parsed
      
      // Manejar si viene como Feature o Geometry directa
      if (parsed.type === 'Feature' && parsed.geometry) {
        geometry = parsed.geometry
      }

      if (geometry && geometry.coordinates) {
        const feature = {
          type: 'Feature',
          id: lugar.id,
          properties: {
             id: lugar.id,
             nombre: lugar.nombre,
             empleados: lugar.totalEmpleados,
             direccion: lugar.direccion,
             departamento: lugar.departamentoId
          },
          geometry: geometry
        }
        features.push(feature)

        // Usar Turf.js para calcular el centroide
        const center = turf.centroid(feature as any)
        center.properties = { ...feature.properties }
        center.id = lugar.id
        centroidFeatures.push(center)
      }
    } catch (e) {
      console.error(`Error procesando lugar ${lugar.nombre}:`, e)
    }
  })

  // 2. Limpiar capas previas
  const sourceId = 'geocercas'
  if (map.value.getSource(sourceId)) {
    if (map.value.getLayer('geocercas-fill')) map.value.removeLayer('geocercas-fill')
    if (map.value.getLayer('geocercas-border')) map.value.removeLayer('geocercas-border')
    map.value.removeSource(sourceId)
  }
  
  // Limpiar pines HTML previos
  marcadoresPines.forEach(m => m.remove())
  marcadoresPines = []

  // 3. Agregar Source y Layers
  try {
     map.value.addSource(sourceId, {
       type: 'geojson',
       data: {
         type: 'FeatureCollection',
         features: features
       }
     })

     // Capa Relleno (Azul)
     map.value.addLayer({
       id: 'geocercas-fill',
       type: 'fill',
       source: sourceId,
       layout: {
         'visibility': map.value.getZoom() >= 13 ? 'visible' : 'none'
       },
       paint: { 
         'fill-color': '#3b82f6', 
         'fill-opacity': 0.3,
         'fill-outline-color': '#1d4ed8'
       }
     })

     // Capa Borde (Azul Oscuro)
     map.value.addLayer({
       id: 'geocercas-border',
       type: 'line',
       source: sourceId,
       layout: {
         'visibility': map.value.getZoom() >= 13 ? 'visible' : 'none'
       },
       paint: { 
         'line-color': '#1d4ed8', 
         'line-width': 2 
       }
     })

     // Renderizar los pines HTML en lugar de Mapbox circles
     centroidFeatures.forEach((center) => {
       const el = document.createElement('div')
       el.className = 'custom-geocerca-pin'
       el.innerHTML = `<i class="mdi mdi-map-marker" style="font-size: 38px; color: #dc2626; text-shadow: 1px 2px 4px rgba(0,0,0,0.5); cursor: pointer;"></i>`

       const props = center.properties

       const popup = new mapboxgl.Popup({ closeButton: false, offset: 20, className: 'hover-popup' })
         .setHTML(`
           <div class="pa-2">
             <div class="text-subtitle-1 font-weight-bold mb-1" style="font-family: inherit;">🏢 ${props.nombre}</div>
             <div class="text-body-2" style="font-family: inherit;"><i class="mdi mdi-account-group mr-1" style="font-size: 16px;"></i> Empleados asignados: <strong>${props.empleados || 0}</strong></div>
           </div>
         `)

       const marker = new mapboxgl.Marker({ element: el, anchor: 'bottom' })
         .setLngLat(center.geometry.coordinates as [number, number])
         .addTo(map.value!)

       // Eventos del marcador HTML
       el.addEventListener('mouseenter', () => popup.addTo(map.value!))
       el.addEventListener('mouseleave', () => popup.remove())
       el.addEventListener('click', (e) => {
         e.stopPropagation()
         if (map.value) {
           map.value.flyTo({ center: center.geometry.coordinates as [number, number], zoom: 14.5 })
         }
       })

       marcadoresPines.push(marker)
     })

     console.log('✅ Capas de geocercas agregadas correctamente')
     
     console.log('✨ Geocercas dibujadas EXITOSAMENTE')
     
  } catch (e) {
     console.error('❌ Error fatal al agregar capas al mapa:', e)
  }
}

// Configurar interactividad para las geocercas
function setupInteractividad() {
  if (!map.value) return

  let hoveredPolygonId: string | number | null = null

  // Click en geocerca
  map.value.on('click', 'geocercas-fill', (e) => {
    if (!e.features?.[0]) return

    const feature = e.features[0]
    const lugar = lugares.value.find((l) => l.id === feature.properties?.id)

    if (lugar) {
      emit('lugarSeleccionado', lugar)

      // Mostrar popup
      new mapboxgl.Popup()
        .setLngLat(e.lngLat)
        .setHTML(
          `
          <div class="popup-content">
            <h4>${lugar.nombre}</h4>
            <p><strong>Dirección:</strong> ${lugar.direccion}</p>
            <p><strong>Empleados:</strong> ${lugar.totalEmpleados}</p>
            <p><strong>Departamento:</strong> ${getDepartamentoNombre(lugar.departamentoId)}</p>
            <p><strong>Estado:</strong> ${lugar.activo ? 'Activo' : 'Inactivo'}</p>
          </div>
        `,
        )
        .addTo(map.value!)
    }
  })

  // Efecto hover con Popup
  map.value.on('mouseenter', 'geocercas-fill', (e: any) => {
    if (!map.value || !e.features?.[0]) return

    map.value.getCanvas().style.cursor = 'pointer'
    
    // Mostrar popup temporal
    const feature = e.features[0]
    const nombre = feature.properties?.nombre || 'Geocerca'
    
    new mapboxgl.Popup({ closeButton: false, className: 'hover-popup' })
      .setLngLat(e.lngLat)
      .setHTML(`<strong>🏢 ${nombre}</strong>`)
      .addTo(map.value)

    if (e.features[0].id !== undefined) {
      hoveredPolygonId = e.features[0].id
      map.value.setFeatureState(
        { source: 'geocercas', id: hoveredPolygonId },
        { hover: true },
      )
    }
  })

  map.value.on('mouseleave', 'geocercas-fill', () => {
    if (!map.value) return

    map.value.getCanvas().style.cursor = ''
    
    // Remover popups de hover
    const popups = document.querySelectorAll('.hover-popup')
    popups.forEach(p => p.remove())

    if (hoveredPolygonId !== null) {
      map.value.setFeatureState(
        { source: 'geocercas', id: hoveredPolygonId },
        { hover: false },
      )
      hoveredPolygonId = null
    }
  })
}

// Dibujar empleados como marcadores
function dibujarEmpleados() {
  if (!map.value || !mapaCargado.value) {
    console.log('⚠️ Mapa no está listo para dibujar empleados')
    return
  }

  // Limpiar marcadores anteriores
  const markers = document.querySelectorAll('.empleado-marker')
  markers.forEach((marker) => marker.remove())

  console.log('👤 Dibujando empleados en el mapa...')

  // Filtrar empleados según el filtro
  const empleadosMostrar = empleadosActivosEnMapa.value.filter(
    (ubicacion) => ubicacion.latitud && ubicacion.longitud,
  )

  if (empleadosMostrar.length === 0) {
    console.log('ℹ️ No hay empleados para mostrar en el mapa')
    return
  }

  empleadosMostrar.forEach((ubicacion) => {
    if (!ubicacion.latitud || !ubicacion.longitud) return

    const markerColor = getColorForEmpleado(ubicacion)

    // MERGED FIX: Crear contenedor con dimensiones validas
    const el = document.createElement('div')
    el.className = 'empleado-marker-wrapper'
    // IMPORTANTE: El wrapper debe tener tamaño para capturar eventos de mouse
    el.style.cssText = `
      width: 28px; 
      height: 28px; 
      display: flex;
      justify-content: center;
      align-items: center;
      cursor: pointer;
      z-index: 10;
    `
    
    // Crear elemento visual interno
    const innerEl = document.createElement('div')
    innerEl.className = 'empleado-marker-inner'
    innerEl.style.cssText = `
      width: 24px;
      height: 24px;
      background-color: ${markerColor};
      border-radius: 50%;
      border: 3px solid white;
      box-shadow: 0 4px 8px rgba(0,0,0,0.4);
      transition: transform 0.2s cubic-bezier(0.175, 0.885, 0.32, 1.275);
    `
    el.appendChild(innerEl)

    // Agregar tooltip (al wrapper)
    el.title = `${ubicacion.empleadoNombre}\nTrabajo: ${ubicacion.lugarTrabajo || 'Sin asignar'}\n${ubicacion.estaEnGeocerca ? 'En geocerca' : 'Fuera de geocerca'}`

    // Efecto hover en el elemento interno
    el.addEventListener('mouseenter', () => {
      innerEl.style.transform = 'scale(1.2)'
    })
    el.addEventListener('mouseleave', () => {
      innerEl.style.transform = 'scale(1)'
    })

    // Crear marcador
    const marker = new mapboxgl.Marker({ element: el })
      .setLngLat([ubicacion.longitud, ubicacion.latitud])
      .addTo(map.value!)

    // Agregar popup al hacer clic (al wrapper)
    el.addEventListener('click', () => {
      // Cerrar otros popups
      const popups = document.querySelectorAll('.mapboxgl-popup')
      popups.forEach((popup) => popup.remove())

      const popupContent = `
        <div class="empleado-popup">
          <h4>${ubicacion.empleadoNombre}</h4>
          <p><strong>Estado:</strong> ${ubicacion.estaEnGeocerca ? '✅ En geocerca' : '⚠️ Fuera de geocerca'}</p>
          <p><strong>Lugar:</strong> ${ubicacion.lugarTrabajo || 'No asignado'}</p>
          <p><strong>Última ubicación:</strong> ${ubicacion.fechaHora ? new Date(ubicacion.fechaHora).toLocaleString() : 'N/A'}</p>
          <p><strong>Coordenadas:</strong> ${ubicacion.latitud.toFixed(6)}, ${ubicacion.longitud.toFixed(6)}</p>
        </div>
      `

      new mapboxgl.Popup({ closeButton: true, offset: 25 })
        .setLngLat([ubicacion.longitud, ubicacion.latitud])
        .setHTML(popupContent)
        .addTo(map.value!)

      emit('empleadoSeleccionado', ubicacion)
    })
  })

  console.log(`✅ Dibujados ${empleadosMostrar.length} empleados en el mapa`)
}

// Actualizar marcadores de empleados
function actualizarMarcadoresEmpleados() {
  if (!mapaCargado.value) return

  // Limpiar marcadores existentes
  const markers = document.querySelectorAll('.empleado-marker-wrapper')
  markers.forEach((marker) => marker.remove())

  // Luego, dibujar los nuevos
  if (capasVisibles.value.includes('empleados')) {
    dibujarEmpleados()
  }
}



// Agregar punto al hacer clic (solo en modo edición)
function agregarPunto(e: mapboxgl.MapMouseEvent) {
  if (modo.value !== 'dibujo' || !map.value || !editandoGeocerca.value || !mapaCargado.value) return

  const { lng, lat } = e.lngLat

  // Agregar punto a la lista
  puntos.value.push({ lng, lat })

  // Dibujar punto en el mapa
  new mapboxgl.Marker({ color: '#FF5252', draggable: true })
    .setLngLat([lng, lat])
    .addTo(map.value)
    .on('dragend', () => actualizarPuntosDesdeMarcadores())

  // Si hay 3+ puntos, dibujar/actualizar polígono
  if (puntos.value.length >= 3) {
    dibujarGeocercaEditada()
  }

  emit('puntosCambiados', puntos.value)
}

// Helper para Turf.js Convex Hull
function getConvexCoordinates(pts: Array<{ lng: number; lat: number }>): number[][] {
  if (pts.length < 3) return []
  try {
    const points = turf.featureCollection(pts.map(p => turf.point([p.lng, p.lat])))
    const hull = turf.convex(points)
    if (hull && hull.geometry.coordinates && hull.geometry.coordinates.length > 0) {
      return hull.geometry.coordinates[0] as number[][]
    }
  } catch (e) {
    console.warn('Error calculando Turf.js Convex Hull:', e)
  }
  // Fallback si falla
  return [
    ...pts.map((p) => [p.lng, p.lat]),
    [pts[0].lng, pts[0].lat],
  ]
}

// Dibujar geocerca en edición
function dibujarGeocercaEditada() {
  if (!map.value || !mapaCargado.value || puntos.value.length < 3) return

  // Limpiar geocerca anterior si existe
  if (map.value.getSource('geocerca-editada')) {
    map.value.removeLayer('geocerca-editada-fill')
    map.value.removeLayer('geocerca-editada-line')
    map.value.removeSource('geocerca-editada')
  }

  // Crear polígono cerrado (con Turf.js Convex Hull si es posible)
  const coordenadasPoligono = getConvexCoordinates(puntos.value)

  try {
    // Agregar fuente y capas
    map.value.addSource('geocerca-editada', {
      type: 'geojson',
      data: {
        type: 'Feature',
        properties: {},
        geometry: {
          type: 'Polygon',
          coordinates: [coordenadasPoligono],
        },
      },
    })

    // Capa de relleno (más visible para edición)
    map.value.addLayer({
      id: 'geocerca-editada-fill',
      type: 'fill',
      source: 'geocerca-editada',
      paint: {
        'fill-color': '#1976D2',
        'fill-opacity': 0.4,
      },
    })

    // Capa de borde (más destacada)
    map.value.addLayer({
      id: 'geocerca-editada-line',
      type: 'line',
      source: 'geocerca-editada',
      paint: {
        'line-color': '#1976D2',
        'line-width': 4,
        'line-dasharray': [2, 2],
      },
    })
  } catch (error) {
    console.error('❌ Error dibujando geocerca editada:', error)
  }
}

// Actualizar capas según visibilidad
function actualizarCapas() {
  if (!map.value || !mapaCargado.value) return

  const isGeocercasVisible = capasVisibles.value.includes('geocercas')
  const currentZoom = map.value.getZoom()
  const showPolygons = currentZoom >= 13 && isGeocercasVisible

  const showPins = currentZoom < 13 && isGeocercasVisible

  // Mostrar/ocultar geocercas
  if (map.value.getLayer('geocercas-fill')) {
    map.value.setLayoutProperty('geocercas-fill', 'visibility', showPolygons ? 'visible' : 'none')
    map.value.setLayoutProperty('geocercas-border', 'visibility', showPolygons ? 'visible' : 'none')
  }
  
  // Mostrar/ocultar pines HTML
  marcadoresPines.forEach(m => {
    m.getElement().style.display = showPins ? 'block' : 'none'
  })

  // Mostrar/ocultar empleados
  if (capasVisibles.value.includes('empleados')) {
    dibujarEmpleados()
  } else {
    const markers = document.querySelectorAll('.empleado-marker-wrapper')
    markers.forEach((marker) => marker.remove())
  }
}

// Limpiar mapa
function limpiarMapa() {
  puntos.value = []

  if (map.value && mapaCargado.value) {
    // Limpiar geocerca en edición
    if (map.value.getSource('geocerca-editada')) {
      map.value.removeLayer('geocerca-editada-fill')
      map.value.removeLayer('geocerca-editada-line')
      map.value.removeSource('geocerca-editada')
    }

    // Limpiar puntos de edición
    const markers = document.querySelectorAll('.mapboxgl-marker:not(.empleado-marker-wrapper):not(.custom-geocerca-pin)')
    markers.forEach((marker) => marker.remove())
  }

  if (modo.value !== 'dibujo') {
    editandoGeocerca.value = false
  }
  emit('puntosCambiados', [])
}

// Guardar geocerca
async function guardarGeocerca() {
  if (puntos.value.length < 3) {
    alert('Se necesitan al menos 3 puntos para formar una geocerca')
    return
  }

  guardando.value = true

  try {
    // Crear GeoJSON del polígono
    const coordenadasPoligono = getConvexCoordinates(puntos.value)

    const geojson: GeoJSON.Feature<GeoJSON.Polygon> = {
      type: 'Feature',
      properties: {},
      geometry: {
        type: 'Polygon',
        coordinates: [coordenadasPoligono],
      },
    }

    // Calcular área
    const area = turf.area(geojson)

    console.log('💾 Geocerca guardada:', {
      puntos: puntos.value.length,
      area: `${area.toFixed(2)} m²`,
      geojson,
    })

    // Emitir evento al padre
    emit('geocercaGuardada', geojson)

    alert(
      `✅ Geocerca guardada exitosamente\nPuntos: ${puntos.value.length}\nÁrea: ${area.toFixed(2)} m²`,
    )

    // Limpiar después de guardar
    limpiarMapa()
  } catch (error) {
    console.error('❌ Error guardando geocerca:', error)
    alert('❌ Error guardando geocerca')
  } finally {
    guardando.value = false
  }
}

// Métodos auxiliares
function getColorForLugar(id: number): string {
  const colors = [
    '#FF6B6B',
    '#4ECDC4',
    '#FFD166',
    '#06D6A0',
    '#118AB2',
    '#EF476F',
    '#7209B7',
    '#F72585',
    '#3A86FF',
    '#FB5607',
  ]
  return colors[id % colors.length]
}

function getColorForEmpleado(ubicacion: Ubicacion): string {
  if (!ubicacion.latitud || !ubicacion.longitud) return '#9E9E9E' // Gris para sin ubicación
  if (ubicacion.estaEnGeocerca) return '#4CAF50' // Verde para en geocerca
  return '#F44336' // Rojo para fuera de geocerca
}

function getDepartamentoNombre(id: number): string {
  const departamentos = [
    { id: 1, nombre: 'La Paz' },
    { id: 2, nombre: 'Cochabamba' },
    { id: 3, nombre: 'Santa Cruz' },
    { id: 4, nombre: 'Oruro' },
    { id: 5, nombre: 'Potosí' },
    { id: 6, nombre: 'Chuquisaca' },
    { id: 7, nombre: 'Tarija' },
    { id: 8, nombre: 'Beni' },
    { id: 9, nombre: 'Pando' },
  ]

  const depto = departamentos.find((d) => d.id === id)
  return depto ? depto.nombre : `Departamento ${id}`
}

function actualizarPuntosDesdeMarcadores() {
  // Esta función actualizaría los puntos si los marcadores son draggable
  console.log('🔄 Actualizando puntos desde marcadores...')
  // Por ahora, simplemente volvemos a dibujar la geocerca
  if (puntos.value.length >= 3) {
    dibujarGeocercaEditada()
  }
}

// Método para iniciar edición de nueva geocerca
function iniciarEdicionGeocerca() {
  limpiarMapa()
  editandoGeocerca.value = true
  modo.value = 'dibujo'
}

// Método para cargar geocerca existente para edición
function cargarGeocercaParaEdicion(coordenadas: Array<{ lng: number; lat: number }>) {
  limpiarMapa()
  puntos.value = coordenadas
  editandoGeocerca.value = true
  nextTick(() => {
    if (puntos.value.length >= 3 && mapaCargado.value) {
      dibujarGeocercaEditada()
    }
  })
}

// Exponer métodos al componente padre
defineExpose({
  guardarGeocerca,
  limpiarMapa,
  agregarPuntoManual,
  getPuntos: () => puntos.value,
  iniciarEdicionGeocerca,
  cargarGeocercaParaEdicion,
  actualizarEmpleados: cargarUbicacionesEmpleados,
  actualizarLugares: cargarLugares,
  actualizarMapa: cargarDatos, // Nuevo: recargar todo
})

// Método para agregar punto manualmente
function agregarPuntoManual(lng: number, lat: number) {
  if (!map.value || !editandoGeocerca.value || !mapaCargado.value) return

  const punto = { lng, lat }
  puntos.value.push(punto)

  // Dibujar marcador
  new mapboxgl.Marker({ color: '#FF5252', draggable: true })
    .setLngLat([lng, lat])
    .addTo(map.value)
    .on('dragend', () => actualizarPuntosDesdeMarcadores())

  if (puntos.value.length >= 3) {
    dibujarGeocercaEditada()
  }

  emit('puntosCambiados', puntos.value)
}
</script>

<style scoped>
.map-container {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  background-color: #f5f5f5;
  position: relative;
  width: 100%;
  height: 500px;
}

.custom-geocerca-pin {
  transition: transform 0.2s;
}

.custom-geocerca-pin:hover {
  transform: scale(1.2);
}

.hover-popup .mapboxgl-popup-content {
  border-radius: 8px;
  padding: 8px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

.legend-color {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  display: inline-block;
}

.polygon-color {
  background-color: #1976d2;
}

.empleado-dentro {
  background-color: #4caf50;
}

.empleado-fuera {
  background-color: #f44336;
}

.empleado-sin-ubicacion {
  background-color: #9e9e9e;
}

.gap-2 {
  gap: 8px;
}

.gap-4 {
  gap: 16px;
}

/* Estilos para popups */
:deep(.mapboxgl-popup-content) {
  border-radius: 8px;
  padding: 16px;
  max-width: 300px;
  font-family: 'Roboto', sans-serif;
}

:deep(.empleado-popup h4) {
  margin: 0 0 8px 0;
  color: #1976d2;
  font-size: 16px;
  font-weight: 600;
}

:deep(.empleado-popup p) {
  margin: 4px 0;
  font-size: 14px;
  line-height: 1.4;
}

:deep(.popup-content h4) {
  margin: 0 0 8px 0;
  color: #1976d2;
  font-size: 16px;
  font-weight: 600;
}

:deep(.popup-content p) {
  margin: 4px 0;
  font-size: 14px;
  line-height: 1.4;
}
</style>

<script lang="ts">
declare global {
  interface Window {
    __mapboxInteractivityConfigurada?: boolean
  }
}
</script>
