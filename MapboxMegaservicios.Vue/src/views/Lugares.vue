<template>
  <div class="h-100 d-flex flex-column gap-4">
    <!-- Header Minimalista -->
    <v-row align="center" no-gutters class="mb-2">
      <v-col>
        <h1 class="text-h4 font-weight-bold" style="color: var(--v-theme-primary); letter-spacing: -1px;">
          Gestión de Geocercas
        </h1>
        <p class="text-subtitle-1 text-medium-emphasis mt-1">
          Define y administra las ubicaciones de trabajo autorizadas
        </p>
      </v-col>
    </v-row>

    <v-row class="flex-grow-1">
      <!-- Columna Principal: Mapa -->
      <v-col cols="12" lg="8" class="d-flex flex-column">
        <v-card class="flex-grow-1 d-flex flex-column rounded-xl overflow-hidden glass-panel border-0" elevation="12">
          
          <!-- Toolbar Flotante sobre el Mapa -->
          <div class="map-toolbar pa-4 d-flex align-center justify-space-between w-100" style="position: absolute; z-index: 2; top: 0; left: 0; background: linear-gradient(180deg, rgba(15,23,42,0.9) 0%, rgba(15,23,42,0) 100%);">
            <div class="d-flex align-center gap-4">
              <v-btn-toggle
                v-model="modoMapa"
                color="primary"
                mandatory
                variant="flat"
                class="bg-surface rounded-lg elevation-4"
              >
                <v-btn value="dibujo" class="px-4 text-none font-weight-bold">
                  <v-icon start>mdi-draw-pen</v-icon> Dibujar
                </v-btn>
                <v-btn value="mover" class="px-4 text-none font-weight-bold">
                  <v-icon start>mdi-cursor-move</v-icon> Mover
                </v-btn>
              </v-btn-toggle>

              <v-select
                v-model="departamentoFiltro"
                :items="departamentos"
                item-title="nombre"
                item-value="id"
                density="compact"
                variant="solo-filled"
                bg-color="surface"
                hide-details
                flat
                class="rounded-lg elevation-4"
                style="max-width: 200px"
                @update:model-value="cambiarDepartamento"
                prepend-inner-icon="mdi-map"
              ></v-select>
            </div>

            <div class="d-flex gap-2">
              <v-chip
                v-if="puntosGeocerca.length > 0"
                :color="estadoGeocerca.type"
                variant="flat"
                class="elevation-4 font-weight-bold"
              >
                {{ puntosGeocerca.length }} Puntos Registrados
              </v-chip>
              <v-btn
                color="error"
                variant="flat"
                @click="limpiarMapa"
                :disabled="!hayPuntosEnMapa"
                class="text-none font-weight-bold elevation-4 rounded-lg"
              >
                <v-icon start>mdi-eraser</v-icon> Limpiar
              </v-btn>
            </div>
          </div>

          <!-- Contenedor del Mapa -->
          <div class="flex-grow-1 position-relative" style="min-height: 500px;">
            <MapboxMap
              v-if="mostrarMapa"
              ref="mapaRef"
              :coordenadas-iniciales="coordenadasActuales"
              :modo="modoMapa"
              :departamento-centro="centroDepartamento"
              @puntos-cambiados="onPuntosCambiados"
              class="w-100 h-100"
            />
            <div v-else class="w-100 h-100 d-flex align-center justify-center bg-surface">
              <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
            </div>
          </div>
          
          <!-- Panel Inferior: Formulario Rápido -->
          <div class="pa-6 bg-surface" style="border-top: 1px solid rgba(255,255,255,0.05);">
            <v-form @submit.prevent="guardarLugar">
              <v-row align="end">
                <v-col cols="12" sm="3">
                  <v-text-field
                    v-model="formLugar.nombre"
                    label="Nombre del Lugar *"
                    variant="underlined"
                    color="primary"
                    :readonly="lugarSeleccionado && !editandoLugar"
                    hide-details
                  ></v-text-field>
                </v-col>
                <v-col cols="12" sm="4">
                  <v-text-field
                    v-model="formLugar.direccion"
                    label="Dirección *"
                    variant="underlined"
                    color="primary"
                    :readonly="lugarSeleccionado && !editandoLugar"
                    hide-details
                  ></v-text-field>
                </v-col>
                <v-col cols="12" sm="2">
                   <v-select
                    v-model="formLugar.departamentoId"
                    :items="departamentos"
                    item-title="nombre"
                    item-value="id"
                    label="Depto *"
                    variant="underlined"
                    color="primary"
                    :readonly="lugarSeleccionado && !editandoLugar"
                    hide-details
                  ></v-select>
                </v-col>
                <v-col cols="12" sm="3" class="d-flex justify-end gap-2">
                  <template v-if="!editandoLugar && lugarSeleccionado">
                    <v-btn color="warning" variant="tonal" class="rounded-lg flex-grow-1" @click="habilitarEdicion">
                      Editar
                    </v-btn>
                    <v-btn color="grey" variant="text" icon="mdi-plus" @click="nuevoLugar"></v-btn>
                  </template>
                  <template v-else>
                    <v-btn 
                      type="submit" 
                      color="primary" 
                      variant="flat" 
                      class="rounded-lg flex-grow-1 text-none font-weight-bold" 
                      :loading="guardandoLugar" 
                      :disabled="puntosGeocerca.length < 3"
                    >
                      Guardar
                    </v-btn>
                    <v-btn v-if="editandoLugar" color="grey" variant="text" icon="mdi-close" @click="cancelarEdicion"></v-btn>
                  </template>
                </v-col>
              </v-row>
            </v-form>
          </div>
        </v-card>
      </v-col>

      <!-- Columna Lateral: Directorio -->
      <v-col cols="12" lg="4" class="d-flex flex-column gap-4">
        <v-card class="bg-surface rounded-xl flex-grow-1 d-flex flex-column" elevation="0">
          <v-card-title class="pa-6 pb-2 d-flex justify-space-between align-center">
            <span class="text-h6 font-weight-bold">Directorio de Lugares</span>
            <v-btn icon="mdi-refresh" variant="tonal" size="small" color="primary" @click="cargarLugares"></v-btn>
          </v-card-title>
          
          <v-card-text class="pa-0 flex-grow-1 overflow-auto" style="max-height: 600px;">
            <v-list class="bg-transparent" lines="two">
              <template v-for="(lugar, i) in lugares" :key="lugar.id">
                <v-list-item
                  @click.stop="seleccionarLugar(lugar)"
                  class="px-6 py-4 cursor-pointer hover-bg"
                  :class="{ 'lugar-activo': lugarSeleccionado?.id === lugar.id }"
                >
                  <template v-slot:prepend>
                    <v-avatar :color="lugarSeleccionado?.id === lugar.id ? 'primary' : 'rgba(255,255,255,0.1)'" size="40" rounded="lg" class="mr-4">
                      <v-icon :color="lugarSeleccionado?.id === lugar.id ? 'white' : 'medium-emphasis'">mdi-office-building</v-icon>
                    </v-avatar>
                  </template>
                  
                  <v-list-item-title class="font-weight-bold mb-1">{{ lugar.nombre }}</v-list-item-title>
                  <v-list-item-subtitle class="text-medium-emphasis">
                    {{ getDepartamentoNombre(lugar.departamentoId) }} &bull; {{ lugar.direccion }}
                  </v-list-item-subtitle>
                  
                  <template v-slot:append>
                    <div class="d-flex align-center">
                       <v-chip size="x-small" :color="lugar.activo ? 'success' : 'error'" variant="flat" class="mr-2 px-2">
                        {{ lugar.totalEmpleados || 0 }} EMP
                      </v-chip>
                      <v-btn
                        icon="mdi-delete-outline"
                        variant="text"
                        color="error"
                        size="small"
                        @click.stop="eliminarLugar(lugar.id)"
                      ></v-btn>
                    </div>
                  </template>
                </v-list-item>
                <v-divider v-if="i < lugares.length - 1" class="border-opacity-25 mx-6"></v-divider>
              </template>
            </v-list>
            
            <div v-if="lugares.length === 0" class="text-center pa-10 opacity-50">
              <v-icon size="48" class="mb-4">mdi-map-marker-off</v-icon>
              <p>No hay lugares registrados</p>
            </div>
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
import { useNotificationStore } from '@/stores/notification'

const notif = useNotificationStore()

const mapaRef = ref<InstanceType<typeof MapboxMap>>()
const mostrarMapa = ref(false)

const lugares = ref<LugarTrabajo[]>([])
const lugarSeleccionado = ref<LugarTrabajo | null>(null)
const editandoLugar = ref(false)
const guardandoLugar = ref(false)
const puntosGeocerca = ref<Array<{ lng: number; lat: number }>>([])
const coordenadasActuales = ref<Array<{ lng: number; lat: number }>>([])
const modoMapa = ref('mover')
const departamentoFiltro = ref<number | null>(null)
const centroDepartamento = ref<{ lng: number; lat: number } | null>(null)

const formLugar = ref({
  nombre: '',
  direccion: '',
  descripcion: '',
  departamentoId: 1,
})

const departamentos = ref([
  { id: 1, nombre: 'La Paz', centro: { lng: -68.119, lat: -16.489 } },
  { id: 2, nombre: 'Cochabamba', centro: { lng: -66.157, lat: -17.393 } },
  { id: 3, nombre: 'Santa Cruz', centro: { lng: -63.181, lat: -17.784 } },
  { id: 4, nombre: 'Oruro', centro: { lng: -67.107, lat: -17.966 } },
  { id: 5, nombre: 'Potosí', centro: { lng: -65.753, lat: -19.583 } },
  { id: 6, nombre: 'Chuquisaca', centro: { lng: -65.259, lat: -19.047 } },
  { id: 7, nombre: 'Tarija', centro: { lng: -64.731, lat: -21.532 } },
  { id: 8, nombre: 'Beni', centro: { lng: -65.755, lat: -14.834 } },
  { id: 9, nombre: 'Pando', centro: { lng: -67.183, lat: -11.026 } },
])

const hayPuntosEnMapa = computed(() => puntosGeocerca.value.length > 0)

const estadoGeocerca = computed(() => {
  if (puntosGeocerca.value.length === 0) return { type: 'warning' }
  if (puntosGeocerca.value.length < 3) return { type: 'error' }
  return { type: 'success' }
})

onMounted(async () => {
  await cargarLugares()
  nextTick(() => { mostrarMapa.value = true })
})

function getDepartamentoNombre(id: number) {
  return departamentos.value.find((d) => d.id === id)?.nombre || 'N/A'
}

function cambiarDepartamento(id: number) {
  const depto = departamentos.value.find((d) => d.id === id)
  if (depto && depto.centro) centroDepartamento.value = depto.centro
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
  if (editandoLugar.value && lugarSeleccionado.value?.id === lugar.id) return
  if (editandoLugar.value && !confirm('¿Cancelar edición actual?')) return

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
      if (mapaRef.value && coordenadasActuales.value.length > 0) {
        mapaRef.value.cargarGeocercaParaEdicion(coordenadasActuales.value)
      }
    } else {
      coordenadasActuales.value = []
    }
  } catch (error) {
    coordenadasActuales.value = []
  }
}

function nuevoLugar() {
  if (editandoLugar.value && !confirm('¿Cancelar actual?')) return
  cancelarEdicion()
  lugarSeleccionado.value = null
  formLugar.value = { nombre: '', direccion: '', descripcion: '', departamentoId: departamentoFiltro.value || 1 }
  puntosGeocerca.value = []
  coordenadasActuales.value = []
  if (mapaRef.value) mapaRef.value.iniciarEdicionGeocerca()
}

function habilitarEdicion() {
  if (lugarSeleccionado.value) {
    editandoLugar.value = true
    if (mapaRef.value) mapaRef.value.iniciarEdicionGeocerca()
  }
}

function cancelarEdicion() {
  editandoLugar.value = false
  if (lugarSeleccionado.value) {
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
  if (!confirm('¿Eliminar lugar de forma permanente?')) return
  try {
    await api.delete(`/admin/lugares/${id}`)
    await cargarLugares()
    if (lugarSeleccionado.value?.id === id) {
      lugarSeleccionado.value = null
      editandoLugar.value = false
      puntosGeocerca.value = []
    }
    if (mapaRef.value) {
      mapaRef.value.limpiarMapa()
      await mapaRef.value.actualizarMapa()
    }
  } catch (error: any) {
    notif.handleApiError(error, 'Error eliminando lugar')
  }
}

function limpiarMapa() {
  if (mapaRef.value) mapaRef.value.limpiarMapa()
  puntosGeocerca.value = []
}

async function guardarLugar() {
  if (puntosGeocerca.value.length < 3) {
    notif.mostrarAdvertencia('Dibuja al menos 3 puntos en el mapa')
    return
  }
  guardandoLugar.value = true
  try {
    const coordenadas = puntosGeocerca.value.map((p) => ({ x: p.lng, y: p.lat }))
    if (editandoLugar.value && lugarSeleccionado.value) {
      await api.put(`/admin/lugares/${lugarSeleccionado.value.id}`, {
        ...formLugar.value, coordenadas,
      })
    } else {
      const response = await api.post('/admin/lugares', {
        ...formLugar.value, coordenadas,
      })
      lugarSeleccionado.value = response.data
    }
    await cargarLugares()
    if (mapaRef.value) {
      mapaRef.value.limpiarMapa()
      await mapaRef.value.actualizarMapa()
    }
    puntosGeocerca.value = []
    coordenadasActuales.value = []
    editandoLugar.value = false
    if (!lugarSeleccionado.value) nuevoLugar()
  } catch (error: any) {
    notif.handleApiError(error, 'Error guardando lugar')
  } finally {
    guardandoLugar.value = false
  }
}

function onPuntosCambiados(puntos: Array<{ lng: number; lat: number }>) {
  puntosGeocerca.value = puntos
}
</script>

<style scoped>
.gap-4 { gap: 16px; }
.gap-2 { gap: 8px; }
.hover-bg { transition: background-color 0.2s ease; }
.hover-bg:hover { background-color: rgba(255,255,255,0.03); }
.lugar-activo {
  background: linear-gradient(90deg, rgba(99, 102, 241, 0.15) 0%, rgba(99, 102, 241, 0) 100%);
  border-left: 4px solid var(--v-theme-primary);
}
</style>
