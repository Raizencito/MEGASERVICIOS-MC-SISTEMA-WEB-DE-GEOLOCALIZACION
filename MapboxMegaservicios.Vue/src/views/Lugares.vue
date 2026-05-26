<template>
  <div>
    <h1 class="mb-6">Lugares de Trabajo</h1>

    <v-row>
      <!-- Columna Izquierda: Mapa y Formulario -->
      <v-col cols="12" lg="8">
        <!-- Tarjeta del Mapa con controles mejorados -->
        <v-card class="mb-4">
          <v-card-title class="d-flex align-center">
            <v-icon start>mdi-map</v-icon>
            Mapa de Geocercas
            <v-spacer></v-spacer>

            <!-- Filtro por Departamento -->
            <v-select
              v-model="departamentoFiltro"
              :items="departamentos"
              item-title="nombre"
              item-value="id"
              label="Departamento"
              density="compact"
              variant="outlined"
              class="mr-4"
              style="max-width: 200px"
              @update:model-value="cambiarDepartamento"
            >
              <template v-slot:prepend-inner>
                <v-icon>mdi-filter</v-icon>
              </template>
            </v-select>

            <!-- Estado del lugar -->
            <v-chip
              :color="lugarSeleccionado ? 'primary' : 'grey'"
              variant="outlined"
              prepend-icon="mdi-map-marker"
            >
              {{ lugarSeleccionado ? lugarSeleccionado.nombre : 'Nuevo Lugar' }}
            </v-chip>
          </v-card-title>

          <!-- Controles del Mapa en Toolbar (ÚNICO punto de control) -->
          <v-toolbar density="compact" color="grey-lighten-3" class="px-2">
            <v-btn-toggle
              v-model="modoMapa"
              color="primary"
              mandatory
              variant="outlined"
              density="compact"
            >
              <v-btn value="dibujo" size="small">
                <v-icon start size="small">mdi-pencil</v-icon>
                Dibujar
              </v-btn>
              <v-btn value="mover" size="small">
                <v-icon start size="small">mdi-cursor-move</v-icon>
                Mover
              </v-btn>
              <v-btn value="ver" size="small">
                <v-icon start size="small">mdi-eye</v-icon>
                Ver
              </v-btn>
            </v-btn-toggle>

            <v-spacer></v-spacer>

            <v-chip size="small" :color="estadoGeocerca.type" variant="flat" class="mr-2">
              {{ puntosGeocerca.length }} puntos
            </v-chip>

            <v-btn
              color="error"
              @click="limpiarMapa"
              :disabled="!hayPuntosEnMapa"
              variant="tonal"
              size="small"
              class="mr-2"
            >
              <v-icon start size="small">mdi-delete</v-icon>
              Limpiar
            </v-btn>

            <v-btn color="success" @click="probarPuntos" variant="tonal" size="small">
              <v-icon start size="small">mdi-plus</v-icon>
              Prueba
            </v-btn>
          </v-toolbar>

          <v-card-text>
            <!-- ✅ COMPONENTE MAPA HIJO -->
            <div v-if="mostrarMapa">
              <MapboxMap
                ref="mapaRef"
                :coordenadas-iniciales="coordenadasActuales"
                :modo="modoMapa"
                :departamento-centro="centroDepartamento"
                @geocerca-guardada="onGeocercaGuardada"
                @puntos-cambiados="onPuntosCambiados"
              />
            </div>
            <div v-else class="text-center pa-8">
              <v-progress-circular indeterminate color="primary"></v-progress-circular>
              <p class="mt-4">Cargando mapa...</p>
            </div>
          </v-card-text>
        </v-card>

        <!-- Formulario del Lugar - Ahora siempre visible pero con estado claro -->
        <v-card
          :class="{
            'formulario-edicion': editandoLugar,
            'formulario-nuevo': !editandoLugar && !lugarSeleccionado,
          }"
        >
          <v-card-title class="d-flex align-center">
            <v-icon start :color="editandoLugar ? 'warning' : 'primary'">
              {{ editandoLugar ? 'mdi-pencil' : lugarSeleccionado ? 'mdi-map-marker' : 'mdi-plus' }}
            </v-icon>
            {{
              editandoLugar
                ? 'Editando Lugar'
                : lugarSeleccionado
                  ? 'Lugar Seleccionado'
                  : 'Nuevo Lugar'
            }}

            <v-spacer></v-spacer>

            <v-chip
              v-if="editandoLugar"
              color="warning"
              variant="outlined"
              prepend-icon="mdi-alert"
            >
              Modo Edición
            </v-chip>
          </v-card-title>

          <v-divider></v-divider>

          <v-card-text>
            <v-form @submit.prevent="guardarLugar">
              <v-row>
                <v-col cols="12" md="6">
                  <v-text-field
                    v-model="formLugar.nombre"
                    label="Nombre *"
                    :rules="[(v) => !!v || 'Nombre es requerido']"
                    required
                    variant="outlined"
                    density="comfortable"
                    :readonly="lugarSeleccionado && !editandoLugar"
                  ></v-text-field>
                </v-col>

                <v-col cols="12" md="6">
                  <v-select
                    v-model="formLugar.departamentoId"
                    :items="departamentos"
                    item-title="nombre"
                    item-value="id"
                    label="Departamento *"
                    :rules="[(v) => !!v || 'Departamento es requerido']"
                    required
                    variant="outlined"
                    density="comfortable"
                    :readonly="lugarSeleccionado && !editandoLugar"
                  ></v-select>
                </v-col>
              </v-row>

              <v-row>
                <v-col cols="12">
                  <v-text-field
                    v-model="formLugar.direccion"
                    label="Dirección *"
                    :rules="[(v) => !!v || 'Dirección es requerida']"
                    required
                    variant="outlined"
                    density="comfortable"
                    :readonly="lugarSeleccionado && !editandoLugar"
                  ></v-text-field>
                </v-col>
              </v-row>

              <v-row>
                <v-col cols="12">
                  <v-textarea
                    v-model="formLugar.descripcion"
                    label="Descripción"
                    rows="2"
                    hint="Descripción opcional del lugar"
                    variant="outlined"
                    density="comfortable"
                    :readonly="lugarSeleccionado && !editandoLugar"
                  ></v-textarea>
                </v-col>
              </v-row>

              <!-- Estado de la Geocerca con más información -->
              <v-alert :type="estadoGeocerca.type" variant="tonal" class="mb-4">
                <div class="d-flex align-center">
                  <v-icon :color="estadoGeocerca.type" class="mr-3">
                    {{ estadoGeocerca.icon }}
                  </v-icon>
                  <div>
                    <div class="font-weight-medium">{{ estadoGeocerca.titulo }}</div>
                    <div class="text-caption">{{ estadoGeocerca.mensaje }}</div>
                  </div>
                  <v-spacer></v-spacer>
                  <v-chip size="small" :color="estadoGeocerca.type" variant="flat">
                    {{ puntosGeocerca.length }} puntos
                  </v-chip>
                </div>
              </v-alert>

              <!-- Botones de acción -->
              <div class="d-flex gap-2">
                <template v-if="!editandoLugar && lugarSeleccionado">
                  <v-btn
                    color="warning"
                    @click="habilitarEdicion"
                    variant="outlined"
                    prepend-icon="mdi-pencil"
                  >
                    Editar Lugar
                  </v-btn>

                  <v-btn
                    color="primary"
                    @click="guardarSoloGeocerca"
                    :disabled="puntosGeocerca.length < 3"
                    variant="tonal"
                    prepend-icon="mdi-map-marker"
                  >
                    Actualizar Geocerca
                  </v-btn>
                </template>

                <template v-else>
                  <v-btn
                    type="submit"
                    color="primary"
                    :loading="guardandoLugar"
                    :disabled="puntosGeocerca.length < 3"
                    prepend-icon="mdi-content-save"
                  >
                    {{ editandoLugar ? 'Actualizar Lugar' : 'Crear Lugar' }}
                  </v-btn>

                  <v-btn
                    v-if="editandoLugar"
                    @click="cancelarEdicion"
                    variant="outlined"
                    color="grey"
                    prepend-icon="mdi-close"
                  >
                    Cancelar
                  </v-btn>
                </template>

                <v-spacer></v-spacer>

                <v-btn color="grey" @click="nuevoLugar" variant="text" prepend-icon="mdi-plus">
                  Nuevo
                </v-btn>
              </div>
            </v-form>
          </v-card-text>
        </v-card>
      </v-col>

      <!-- Columna Derecha: Lista de Lugares -->
      <v-col cols="12" lg="4">
        <!-- Lista de Lugares -->
        <v-card class="mb-4">
          <v-card-title class="d-flex align-center">
            <v-icon start>mdi-office-building</v-icon>
            Lugares Registrados
            <v-spacer></v-spacer>
            <v-btn icon @click="cargarLugares" size="small" variant="text">
              <v-icon>mdi-refresh</v-icon>
            </v-btn>
          </v-card-title>

          <v-card-text>
            <v-list v-if="lugares.length > 0">
              <v-list-item
                v-for="lugar in lugares"
                :key="lugar.id"
                :title="lugar.nombre"
                :subtitle="`${lugar.direccion} · ${getDepartamentoNombre(lugar.departamentoId)}`"
                @click="seleccionarLugar(lugar)"
                :class="{
                  'lugar-activo': lugarSeleccionado?.id === lugar.id,
                  'lugar-editando': editandoLugar && lugarSeleccionado?.id === lugar.id,
                }"
                lines="two"
              >
                <template v-slot:prepend>
                  <v-badge :color="lugar.activo ? 'success' : 'error'" dot inline class="mr-3">
                    <v-avatar
                      :color="lugarSeleccionado?.id === lugar.id ? 'primary' : 'grey-lighten-1'"
                      size="36"
                    >
                      <v-icon size="small" color="white"> mdi-map-marker </v-icon>
                    </v-avatar>
                  </v-badge>
                </template>

                <template v-slot:append>
                  <div class="d-flex">
                    <v-btn
                      icon
                      size="small"
                      @click.stop="editarLugar(lugar)"
                      color="primary"
                      variant="text"
                      density="compact"
                    >
                      <v-icon size="small">mdi-pencil</v-icon>
                    </v-btn>
                    <v-btn
                      icon
                      size="small"
                      @click.stop="eliminarLugar(lugar.id)"
                      color="error"
                      variant="text"
                      density="compact"
                      class="ml-1"
                    >
                      <v-icon size="small">mdi-delete</v-icon>
                    </v-btn>
                  </div>
                </template>
              </v-list-item>
            </v-list>

            <v-alert v-else type="info" variant="tonal">
              <div class="text-center">
                <v-icon size="large" class="mb-2">mdi-map-marker-off</v-icon>
                <div>No hay lugares registrados</div>
              </div>
            </v-alert>
          </v-card-text>

          <v-card-actions>
            <v-btn
              block
              color="primary"
              variant="tonal"
              @click="nuevoLugar"
              prepend-icon="mdi-plus"
            >
              Nuevo Lugar
            </v-btn>
          </v-card-actions>
        </v-card>

        <!-- Estadísticas -->
        <v-card>
          <v-card-title>
            <v-icon start>mdi-chart-box</v-icon>
            Estadísticas
          </v-card-title>

          <v-card-text>
            <v-list density="comfortable">
              <v-list-item class="px-0">
                <v-list-item-title class="text-body-2">Total Lugares</v-list-item-title>
                <v-list-item-subtitle class="text-right text-h6 font-weight-bold">
                  {{ lugares.length }}
                </v-list-item-subtitle>
              </v-list-item>

              <v-list-item class="px-0">
                <v-list-item-title class="text-body-2">Lugares Activos</v-list-item-title>
                <v-list-item-subtitle class="text-right">
                  <v-chip size="small" color="success" variant="tonal">
                    {{ lugaresActivos }}
                  </v-chip>
                </v-list-item-subtitle>
              </v-list-item>

              <v-list-item class="px-0">
                <v-list-item-title class="text-body-2">Total Empleados</v-list-item-title>
                <v-list-item-subtitle class="text-right text-h6 font-weight-bold text-primary">
                  {{ totalEmpleados }}
                </v-list-item-subtitle>
              </v-list-item>

              <v-list-item class="px-0">
                <v-list-item-title class="text-body-2">Puntos en Geocerca</v-list-item-title>
                <v-list-item-subtitle class="text-right">
                  <v-chip size="small" :color="estadoGeocerca.type" variant="flat">
                    {{ puntosGeocerca.length }}
                  </v-chip>
                </v-list-item-subtitle>
              </v-list-item>
            </v-list>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import MapboxMap from '@/components/MapboxMap.vue'
import api from '@/services/api'
import type { LugarTrabajo } from '@/types'

// Referencias
const mapaRef = ref<InstanceType<typeof MapboxMap>>()
const mostrarMapa = ref(false)

// Estados
const lugares = ref<LugarTrabajo[]>([])
const lugarSeleccionado = ref<LugarTrabajo | null>(null)
const editandoLugar = ref(false)
const guardandoLugar = ref(false)
const puntosGeocerca = ref<Array<{ lng: number; lat: number }>>([])
const coordenadasActuales = ref<Array<{ lng: number; lat: number }>>([])
const modoMapa = ref('dibujo')
const departamentoFiltro = ref<number | null>(null)
const centroDepartamento = ref<{ lng: number; lat: number } | null>(null)

// Formulario
const formLugar = ref({
  nombre: '',
  direccion: '',
  descripcion: '',
  departamentoId: 1, // La Paz por defecto
})

// Departamentos con centros definidos
const departamentos = ref([
  { id: 1, nombre: 'La Paz', codigo: 'LP', centro: { lng: -68.119, lat: -16.489 } },
  { id: 2, nombre: 'Cochabamba', codigo: 'CB', centro: { lng: -66.157, lat: -17.393 } },
  { id: 3, nombre: 'Santa Cruz', codigo: 'SC', centro: { lng: -63.181, lat: -17.784 } },
  { id: 4, nombre: 'Oruro', codigo: 'OR', centro: { lng: -67.107, lat: -17.966 } },
  { id: 5, nombre: 'Potosí', codigo: 'PT', centro: { lng: -65.753, lat: -19.583 } },
  { id: 6, nombre: 'Chuquisaca', codigo: 'CH', centro: { lng: -65.259, lat: -19.047 } },
  { id: 7, nombre: 'Tarija', codigo: 'TJ', centro: { lng: -64.731, lat: -21.532 } },
  { id: 8, nombre: 'Beni', codigo: 'BN', centro: { lng: -65.755, lat: -14.834 } },
  { id: 9, nombre: 'Pando', codigo: 'PD', centro: { lng: -67.183, lat: -11.026 } },
])

// Computed
const totalEmpleados = computed(() => {
  return lugares.value.reduce((total, lugar) => total + (lugar.totalEmpleados || 0), 0)
})

const lugaresActivos = computed(() => {
  return lugares.value.filter((l) => l.activo).length
})

const hayPuntosEnMapa = computed(() => {
  return puntosGeocerca.value.length > 0
})

const estadoGeocerca = computed<{
  type: 'info' | 'warning' | 'success' | 'error'
  icon: string
  titulo: string
  mensaje: string
}>(() => {
  if (puntosGeocerca.value.length === 0) {
    return {
      type: 'warning',
      icon: 'mdi-alert',
      titulo: 'Geocerca vacía',
      mensaje: 'No hay puntos en el mapa. Haz clic para agregar o dibuja una geocerca.',
    }
  } else if (puntosGeocerca.value.length < 3) {
    return {
      type: 'warning',
      icon: 'mdi-alert-circle',
      titulo: 'Geocerca incompleta',
      mensaje: `Faltan ${3 - puntosGeocerca.value.length} puntos para formar una geocerca (mínimo 3)`,
    }
  } else {
    return {
      type: 'success',
      icon: 'mdi-check-circle',
      titulo: 'Geocerca lista',
      mensaje: `Puedes guardar la geocerca con ${puntosGeocerca.value.length} puntos`,
    }
  }
})

// Lifecycle
onMounted(async () => {
  await cargarLugares()
  nextTick(() => {
    mostrarMapa.value = true
  })
})

// Métodos
function getDepartamentoNombre(id: number) {
  const depto = departamentos.value.find((d) => d.id === id)
  return depto ? depto.nombre : 'N/A'
}

function cambiarDepartamento(id: number) {
  const depto = departamentos.value.find((d) => d.id === id)
  if (depto && depto.centro) {
    centroDepartamento.value = depto.centro
  }
}

async function cargarLugares() {
  try {
    const response = await api.get('/admin/lugares')
    lugares.value = response.data
  } catch (error) {
    console.error('Error cargando lugares:', error)
  }
}

function seleccionarLugar(lugar: LugarTrabajo) {
  if (editandoLugar.value && lugarSeleccionado.value?.id === lugar.id) {
    return // Ya está seleccionado en modo edición
  }

  if (editandoLugar.value) {
    if (!confirm('Estás editando un lugar. ¿Deseas cancelar la edición y seleccionar otro?')) {
      return
    }
    cancelarEdicion()
  }

  lugarSeleccionado.value = lugar
  editandoLugar.value = false
  formLugar.value = {
    nombre: lugar.nombre,
    direccion: lugar.direccion,
    descripcion: lugar.descripcion || '',
    departamentoId: lugar.departamentoId || 1,
  }
  cargarGeocercaLugar(lugar.id)
}

async function cargarGeocercaLugar(lugarId: number) {
  try {
    const response = await api.get(`/admin/lugares/${lugarId}/geocerca`)
    const geojson = response.data

    if (geojson.geometry?.coordinates?.[0]) {
      coordenadasActuales.value = geojson.geometry.coordinates[0]
        .slice(0, -1)
        .map((coord: [number, number]) => ({ lng: coord[0], lat: coord[1] }))
      
      // Cargar en el mapa para edición
      if (mapaRef.value && coordenadasActuales.value.length > 0) {
        mapaRef.value.cargarGeocercaParaEdicion(coordenadasActuales.value)
      }
    } else {
      coordenadasActuales.value = []
    }
  } catch (error) {
    console.error('Error cargando geocerca:', error)
    coordenadasActuales.value = []
  }
}

function nuevoLugar() {
  if (editandoLugar.value) {
    if (!confirm('Estás editando un lugar. ¿Deseas cancelar y crear uno nuevo?')) {
      return
    }
  }

  cancelarEdicion()
  lugarSeleccionado.value = null
  formLugar.value = {
    nombre: '',
    direccion: '',
    descripcion: '',
    departamentoId: departamentoFiltro.value || 1,
  }
  puntosGeocerca.value = []
  coordenadasActuales.value = []
  
  // Activar modo de edición en el mapa
  if (mapaRef.value) {
    mapaRef.value.iniciarEdicionGeocerca()
  }
}

function habilitarEdicion() {
  if (lugarSeleccionado.value) {
    editandoLugar.value = true
    
    // Activar modo de edición en el mapa
    if (mapaRef.value) {
      mapaRef.value.iniciarEdicionGeocerca()
    }
  }
}

function editarLugar(lugar: LugarTrabajo) {
  if (editandoLugar.value && lugarSeleccionado.value?.id !== lugar.id) {
    if (!confirm('Estás editando otro lugar. ¿Deseas cancelar y editar este?')) {
      return
    }
  }

  lugarSeleccionado.value = lugar
  editandoLugar.value = true
  formLugar.value = {
    nombre: lugar.nombre,
    direccion: lugar.direccion,
    descripcion: lugar.descripcion || '',
    departamentoId: lugar.departamentoId || 1,
  }
  cargarGeocercaLugar(lugar.id)
  
  // Activar modo de edición en el mapa
  if (mapaRef.value) {
    mapaRef.value.iniciarEdicionGeocerca()
  }
}

function cancelarEdicion() {
  editandoLugar.value = false
  if (lugarSeleccionado.value) {
    // Restaurar datos originales del lugar seleccionado
    formLugar.value = {
      nombre: lugarSeleccionado.value.nombre,
      direccion: lugarSeleccionado.value.direccion,
      descripcion: lugarSeleccionado.value.descripcion || '',
      departamentoId: lugarSeleccionado.value.departamentoId || 1,
    }
    cargarGeocercaLugar(lugarSeleccionado.value.id)
  }
}

async function eliminarLugar(id: number) {
  if (!confirm('¿Eliminar este lugar? Los empleados perderán su asignación.')) return

  try {
    await api.delete(`/admin/lugares/${id}`)
    await cargarLugares()
    if (lugarSeleccionado.value?.id === id) {
      lugarSeleccionado.value = null
      editandoLugar.value = false
      formLugar.value = { nombre: '', direccion: '', descripcion: '', departamentoId: departamentoFiltro.value || 1 }
      puntosGeocerca.value = []
      coordenadasActuales.value = []
    }
    // ✅ Refrescar geocercas del mapa
    if (mapaRef.value) {
      mapaRef.value.limpiarMapa()
      await mapaRef.value.actualizarMapa()
    }
  } catch (error: any) {
    alert(error.response?.data?.message || 'Error eliminando lugar')
  }
}

function limpiarMapa() {
  if (mapaRef.value) {
    mapaRef.value.limpiarMapa()
  }
  puntosGeocerca.value = []
}

function probarPuntos() {
  if (mapaRef.value) {
    // Agregar puntos de prueba (ejemplo: Plaza Murillo, La Paz)
    const puntosPrueba = [
      { lng: -68.1193, lat: -16.4958 },
      { lng: -68.1185, lat: -16.4958 },
      { lng: -68.1185, lat: -16.495 },
      { lng: -68.1193, lat: -16.495 },
    ]

    puntosPrueba.forEach((punto) => {
      mapaRef.value?.agregarPuntoManual(punto.lng, punto.lat)
    })
  }
}

async function guardarLugar() {
  if (puntosGeocerca.value.length < 3) {
    alert('Se necesitan al menos 3 puntos para la geocerca')
    return
  }

  guardandoLugar.value = true

  try {
    const coordenadas = puntosGeocerca.value.map((p) => ({ x: p.lng, y: p.lat }))

    let response
    if (editandoLugar.value && lugarSeleccionado.value) {
      // Actualizar — ahora incluye departamentoId y coordenadas opcionales
      response = await api.put(`/admin/lugares/${lugarSeleccionado.value.id}`, {
        nombre: formLugar.value.nombre,
        direccion: formLugar.value.direccion,
        descripcion: formLugar.value.descripcion,
        departamentoId: formLugar.value.departamentoId,
        coordenadas: coordenadas,
      })
    } else {
      // Crear nuevo
      response = await api.post('/admin/lugares', {
        ...formLugar.value,
        coordenadas: coordenadas,
      })
      lugarSeleccionado.value = response.data
    }

    await cargarLugares()

    // ✅ Refrescar geocercas del mapa
    if (mapaRef.value) {
      mapaRef.value.limpiarMapa()
      await mapaRef.value.actualizarMapa()
    }

    alert(editandoLugar.value ? '✅ Lugar actualizado' : '✅ Lugar creado')

    // Limpiar puntos después de crear/actualizar
    puntosGeocerca.value = []
    coordenadasActuales.value = []

    // Si es nuevo, mantener el formulario para otro lugar
    if (!editandoLugar.value) {
      formLugar.value = {
        nombre: '',
        direccion: '',
        descripcion: '',
        departamentoId: departamentoFiltro.value || 1,
      }
    } else {
      editandoLugar.value = false
    }
  } catch (error: any) {
    alert(error.response?.data?.message || 'Error guardando lugar')
  } finally {
    guardandoLugar.value = false
  }
}

async function guardarSoloGeocerca() {
  if (!lugarSeleccionado.value || puntosGeocerca.value.length < 3) {
    alert('Selecciona un lugar y define una geocerca primero')
    return
  }

  try {
    await api.put(`/admin/lugares/${lugarSeleccionado.value.id}/geocerca`, {
      coordenadas: puntosGeocerca.value.map((p) => ({ x: p.lng, y: p.lat })),
    })

    // ✅ Refrescar geocercas del mapa
    if (mapaRef.value) {
      mapaRef.value.limpiarMapa()
      await mapaRef.value.actualizarMapa()
    }

    alert('✅ Geocerca actualizada')
  } catch (error: any) {
    alert(error.response?.data?.message || 'Error guardando geocerca')
  }
}

// Eventos del componente hijo (MapboxMap)
function onGeocercaGuardada(geojson: any) {
  console.log('Geocerca guardada desde componente hijo:', geojson)
}

function onPuntosCambiados(puntos: Array<{ lng: number; lat: number }>) {
  puntosGeocerca.value = puntos
}
</script>

<style scoped>
.lugar-activo {
  background-color: rgba(25, 118, 210, 0.08);
  border-left: 4px solid #1976d2;
}

.lugar-editando {
  background-color: rgba(255, 152, 0, 0.08);
  border-left: 4px solid #ff9800;
}

.gap-2 {
  gap: 8px;
}

.formulario-edicion {
  border: 2px solid #ff9800;
  box-shadow: 0 4px 12px rgba(255, 152, 0, 0.15);
}

.formulario-nuevo {
  border: 2px solid #4caf50;
  box-shadow: 0 4px 12px rgba(76, 175, 80, 0.15);
}

/* Animación para cambios de estado */
.v-list-item {
  transition: all 0.3s ease;
}

.v-list-item:hover {
  transform: translateX(4px);
}
</style>
