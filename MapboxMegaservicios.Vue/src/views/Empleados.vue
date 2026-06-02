<template>
  <div class="h-100 d-flex flex-column gap-6">
    <!-- Header -->
    <v-row align="center" no-gutters class="mb-4">
      <v-col cols="12" md="6" class="mb-4 mb-md-0">
        <h1 class="text-h4 font-weight-bold" style="color: var(--v-theme-primary); letter-spacing: -1px;">
          Directorio de Personal
        </h1>
        <p class="text-subtitle-1 text-medium-emphasis mt-1">
          Gestión de empleados y asignación de geocercas
        </p>
      </v-col>
      
      <v-col cols="12" md="6" class="d-flex justify-md-end align-center gap-4">
        <!-- Búsqueda Integrada -->
        <v-text-field
          v-model="filtroEmpleados"
          placeholder="Buscar por nombre, CI o usuario..."
          variant="solo-filled"
          density="comfortable"
          prepend-inner-icon="mdi-magnify"
          hide-details
          rounded="lg"
          flat
          bg-color="rgba(255,255,255,0.05)"
          class="flex-grow-1 flex-md-grow-0"
          style="min-width: 280px"
        ></v-text-field>

        <!-- Botón de Nuevo Empleado -->
        <v-btn 
          color="primary" 
          variant="flat" 
          prepend-icon="mdi-account-plus" 
          @click="openDialog" 
          size="large"
          class="text-none font-weight-bold" 
          rounded="lg"
        >
          Agregar
        </v-btn>
      </v-col>
    </v-row>

    <!-- Tabla Principal (Estilo Premium) -->
    <v-card class="flex-grow-1" elevation="0" rounded="xl" style="background: var(--v-theme-surface);">
      <v-table class="bg-transparent table-premium">
        <thead>
          <tr>
            <th class="text-left font-weight-bold text-medium-emphasis px-6 py-4">PERFIL</th>
            <th class="text-left font-weight-bold text-medium-emphasis py-4">DOCUMENTO</th>
            <th class="text-left font-weight-bold text-medium-emphasis py-4">CONTACTO</th>
            <th class="text-left font-weight-bold text-medium-emphasis py-4">ASIGNACIÓN</th>
            <th class="text-center font-weight-bold text-medium-emphasis py-4">ESTADO</th>
            <th class="text-right font-weight-bold text-medium-emphasis px-6 py-4">ACCIONES</th>
          </tr>
        </thead>
        <tbody>
          <tr 
            v-for="empleado in empleadosFiltrados" 
            :key="empleado.id"
            class="table-row-hover"
            :class="{ 'opacity-60': !empleado.activo }"
          >
            <td class="px-6 py-3">
              <div class="d-flex align-center">
                <v-avatar :color="empleado.activo ? 'primary' : 'grey'" size="40" variant="tonal" class="mr-4">
                  <span class="font-weight-bold">{{ empleado.nombres.charAt(0) }}{{ empleado.paterno.charAt(0) }}</span>
                </v-avatar>
                <div>
                  <div class="font-weight-bold text-body-1">{{ empleado.nombreCompleto }}</div>
                  <div class="text-caption text-medium-emphasis">@{{ empleado.usuario }}</div>
                </div>
              </div>
            </td>
            <td class="py-3 font-weight-medium">{{ empleado.ci }}</td>
            <td class="py-3">
              <div class="d-flex align-center text-medium-emphasis">
                <v-icon size="small" class="mr-2" v-if="empleado.telefono">mdi-phone</v-icon>
                {{ empleado.telefono || 'No registrado' }}
              </div>
            </td>
            <td class="py-3">
              <v-chip
                :color="empleado.lugarActual ? 'info' : 'warning'"
                variant="tonal"
                size="small"
                class="font-weight-medium cursor-pointer"
                @click="empleado.activo && openLugarDialog(empleado)"
              >
                <v-icon start size="small">mdi-map-marker</v-icon>
                {{ empleado.lugarActual || 'Sin Asignar' }}
                <v-icon end size="small" v-if="empleado.activo">mdi-pencil-circle</v-icon>
              </v-chip>
            </td>
            <td class="py-3 text-center">
              <v-chip
                :color="empleado.activo ? 'success' : 'error'"
                size="small"
                variant="flat"
                class="font-weight-bold px-3"
              >
                {{ empleado.activo ? 'ACTIVO' : 'BAJA' }}
              </v-chip>
            </td>
            <td class="px-6 py-3 text-right">
              <v-btn
                icon="mdi-pencil-outline"
                variant="text"
                color="primary"
                size="small"
                @click="editEmpleado(empleado)"
                :disabled="!empleado.activo"
                class="mr-2"
              ></v-btn>
              <v-btn
                :icon="empleado.activo ? 'mdi-account-off-outline' : 'mdi-account-check-outline'"
                variant="text"
                :color="empleado.activo ? 'error' : 'success'"
                size="small"
                @click="toggleActivo(empleado)"
              ></v-btn>
            </td>
          </tr>
        </tbody>
      </v-table>
      
      <!-- Empty State -->
      <div v-if="empleadosFiltrados.length === 0 && !loading" class="text-center pa-10">
        <v-icon size="64" color="grey" class="mb-4 opacity-50">mdi-account-search</v-icon>
        <h3 class="text-h6 text-medium-emphasis">No se encontraron empleados</h3>
      </div>
    </v-card>

    <!-- Modal Formulario Empleado -->
    <v-dialog v-model="dialog" max-width="600" persistent>
      <v-card rounded="xl" class="glass-panel" elevation="24">
        <v-card-title class="pa-6 pb-2 d-flex justify-space-between align-center">
          <span class="text-h5 font-weight-bold">{{ editMode ? 'Editar Perfil' : 'Nuevo Registro' }}</span>
          <v-btn icon="mdi-close" variant="text" @click="dialog = false"></v-btn>
        </v-card-title>
        
        <v-card-text class="pa-6 pt-0">
          <v-form @submit.prevent="saveEmpleado">
            <v-row>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.nombres"
                  label="Nombres *"
                  variant="solo-filled"
                  flat
                  bg-color="rgba(255,255,255,0.05)"
                  :error-messages="errors.nombres"
                ></v-text-field>
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.paterno"
                  label="Apellido Paterno *"
                  variant="solo-filled"
                  flat
                  bg-color="rgba(255,255,255,0.05)"
                  :error-messages="errors.paterno"
                ></v-text-field>
              </v-col>
            </v-row>
            <v-row>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.materno"
                  label="Apellido Materno"
                  variant="solo-filled"
                  flat
                  bg-color="rgba(255,255,255,0.05)"
                ></v-text-field>
              </v-col>
              <v-col cols="12" sm="6">
                <v-text-field
                  v-model="form.ci"
                  label="Cédula de Identidad *"
                  variant="solo-filled"
                  flat
                  bg-color="rgba(255,255,255,0.05)"
                  :error-messages="errors.ci"
                  :disabled="editMode"
                ></v-text-field>
              </v-col>
            </v-row>
            <v-row>
              <v-col cols="12" :sm="editMode ? 12 : 6">
                <v-text-field
                  v-model="form.telefono"
                  label="Teléfono"
                  variant="solo-filled"
                  flat
                  bg-color="rgba(255,255,255,0.05)"
                  :error-messages="errors.telefono"
                ></v-text-field>
              </v-col>
              <v-col cols="12" sm="6" v-if="!editMode">
                <v-select
                  v-model="form.idLugarTrabajo"
                  :items="lugaresOptions"
                  label="Asignar Geocerca"
                  variant="solo-filled"
                  flat
                  bg-color="rgba(255,255,255,0.05)"
                  item-title="nombre"
                  item-value="id"
                  clearable
                ></v-select>
              </v-col>
            </v-row>

            <v-alert v-if="errorMessage" type="error" class="mt-4" variant="tonal" rounded="lg">
              {{ errorMessage }}
            </v-alert>
          </v-form>
        </v-card-text>
        
        <v-card-actions class="pa-6 pt-0">
          <v-spacer></v-spacer>
          <v-btn variant="text" class="text-none font-weight-medium" @click="dialog = false">Cancelar</v-btn>
          <v-btn 
            color="primary" 
            variant="flat" 
            class="text-none font-weight-bold px-6" 
            rounded="lg"
            @click="saveEmpleado" 
            :loading="loadingAction"
          >
            {{ editMode ? 'Guardar Cambios' : 'Crear Empleado' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Modal Cambio de Geocerca -->
    <v-dialog v-model="lugarDialog" max-width="450">
      <v-card rounded="xl" class="glass-panel" elevation="24">
        <v-card-title class="pa-6 pb-2 d-flex justify-space-between align-center">
          <span class="text-h6 font-weight-bold">Cambiar Geocerca</span>
          <v-btn icon="mdi-close" variant="text" size="small" @click="lugarDialog = false"></v-btn>
        </v-card-title>
        
        <v-card-text class="pa-6 pt-0">
          <div class="mb-6 bg-surface pa-4 rounded-lg d-flex align-center">
            <v-avatar color="primary" size="32" variant="tonal" class="mr-3">
              <v-icon size="small">mdi-account</v-icon>
            </v-avatar>
            <span class="font-weight-medium">{{ selectedEmpleado?.nombreCompleto }}</span>
          </div>

          <v-select
            v-model="nuevoLugarId"
            :items="lugaresOptions"
            label="Seleccionar nueva ubicación"
            variant="solo-filled"
            flat
            bg-color="rgba(255,255,255,0.05)"
            item-title="nombre"
            item-value="id"
            clearable
          ></v-select>

          <v-textarea
            v-model="observacionesLugar"
            label="Motivo del cambio (opcional)"
            variant="solo-filled"
            flat
            bg-color="rgba(255,255,255,0.05)"
            rows="2"
            class="mt-2"
          ></v-textarea>
        </v-card-text>
        
        <v-card-actions class="pa-6 pt-0">
          <v-spacer></v-spacer>
          <v-btn 
            color="primary" 
            variant="flat" 
            block
            rounded="lg"
            class="text-none font-weight-bold"
            @click="cambiarLugar" 
            :loading="cambiandoLugar"
          >
            Confirmar Traslado
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive, computed } from 'vue'
import api from '@/services/api'
import type { Empleado, LugarTrabajo } from '@/types'
import { useNotificationStore } from '@/stores/notification'

const notif = useNotificationStore()

const empleados = ref<Empleado[]>([])
const lugares = ref<LugarTrabajo[]>([])
const dialog = ref(false)
const lugarDialog = ref(false)
const editMode = ref(false)
const loading = ref(true)
const loadingAction = ref(false)
const cambiandoLugar = ref(false)
const errorMessage = ref('')
const editingId = ref<number | null>(null)
const selectedEmpleado = ref<Empleado | null>(null)
const nuevoLugarId = ref<number | null>(null)
const observacionesLugar = ref('')
const filtroEmpleados = ref('')

const form = reactive({
  paterno: '',
  materno: '',
  nombres: '',
  ci: '',
  telefono: '',
  idLugarTrabajo: null as number | null,
})

const errors = reactive({
  paterno: '',
  materno: '',
  nombres: '',
  ci: '',
  telefono: '',
})

const lugaresOptions = computed(() => {
  return [
    { id: null, nombre: 'Sin asignar' },
    ...lugares.value.map((l) => ({ id: l.id, nombre: l.nombre }))
  ]
})

const empleadosFiltrados = computed(() => {
  if (!filtroEmpleados.value) return empleados.value
  const term = filtroEmpleados.value.toLowerCase()
  return empleados.value.filter(
    (emp) =>
      emp.nombreCompleto.toLowerCase().includes(term) ||
      emp.ci.includes(term) ||
      emp.usuario.toLowerCase().includes(term) ||
      (emp.lugarActual && emp.lugarActual.toLowerCase().includes(term)) ||
      emp.telefono?.includes(term),
  )
})

onMounted(async () => {
  loading.value = true
  await Promise.all([loadEmpleados(), loadLugares()])
  loading.value = false
})

async function loadEmpleados() {
  try {
    const response = await api.get('/admin/empleados')
    empleados.value = response.data
  } catch (error) {
    console.error('Error cargando empleados:', error)
  }
}

async function loadLugares() {
  try {
    const response = await api.get('/admin/lugares')
    lugares.value = response.data
  } catch (error) {
    console.error('Error cargando lugares:', error)
  }
}

function openDialog() {
  resetForm()
  editMode.value = false
  dialog.value = true
}

function editEmpleado(empleado: Empleado) {
  editMode.value = true
  editingId.value = empleado.id
  form.paterno = empleado.paterno
  form.materno = empleado.materno || ''
  form.nombres = empleado.nombres
  form.ci = empleado.ci
  form.telefono = empleado.telefono || ''
  dialog.value = true
}

function openLugarDialog(empleado: Empleado) {
  selectedEmpleado.value = empleado
  nuevoLugarId.value = empleado.idLugarTrabajo
  observacionesLugar.value = ''
  lugarDialog.value = true
}

function resetForm() {
  form.paterno = ''
  form.materno = ''
  form.nombres = ''
  form.ci = ''
  form.telefono = ''
  form.idLugarTrabajo = null
  editingId.value = null
  errorMessage.value = ''
  Object.keys(errors).forEach((key) => (errors[key as keyof typeof errors] = ''))
}

async function saveEmpleado() {
  let hasError = false
  if (!form.paterno.trim()) { errors.paterno = 'Requerido'; hasError = true }
  if (!form.nombres.trim()) { errors.nombres = 'Requerido'; hasError = true }
  if (!form.ci.trim()) { errors.ci = 'Requerido'; hasError = true }
  if (form.telefono.trim() && !/^[67]\d{7}$/.test(form.telefono.trim())) {
    errors.telefono = 'Debe comenzar con 6 o 7 y tener 8 dígitos'
    hasError = true
  }

  if (hasError) return

  loadingAction.value = true
  errorMessage.value = ''

  try {
    if (editMode.value && editingId.value) {
      await api.put(`/admin/empleados/${editingId.value}`, {
        paterno: form.paterno,
        materno: form.materno,
        nombres: form.nombres,
        telefono: form.telefono || null,
        activo: true,
      })
    } else {
      const payload: any = {
        paterno: form.paterno,
        materno: form.materno,
        nombres: form.nombres,
        ci: form.ci,
        telefono: form.telefono,
        idRol: 2,
      }
      if (form.idLugarTrabajo) payload.idLugarTrabajo = form.idLugarTrabajo
      await api.post('/admin/empleados', payload)
    }
    dialog.value = false
    await loadEmpleados()
  } catch (error: any) {
    const data = error.response?.data
    if (data?.errors && Array.isArray(data.errors)) {
      errorMessage.value = data.errors.join('. ')
    } else {
      errorMessage.value = data?.message || 'Error guardando empleado'
    }
  } finally {
    loadingAction.value = false
  }
}

async function cambiarLugar() {
  if (!selectedEmpleado.value) return
  cambiandoLugar.value = true
  try {
    await api.patch(`/admin/empleados/${selectedEmpleado.value.id}/lugar-trabajo`, {
      lugarTrabajoId: nuevoLugarId.value,
      observaciones: observacionesLugar.value || 'Asignación vía panel de control',
    })
    lugarDialog.value = false
    await loadEmpleados()
  } catch (error: any) {
    notif.handleApiError(error, 'Error en asignación')
  } finally {
    cambiandoLugar.value = false
  }
}

async function toggleActivo(empleado: Empleado) {
  if (!confirm(`¿${empleado.activo ? 'Desactivar' : 'Activar'} a ${empleado.nombreCompleto}?`)) return
  try {
    await api.patch(`/admin/empleados/${empleado.id}/estadoemp`)
    await loadEmpleados()
    notif.mostrarExito(empleado.activo ? 'Empleado desactivado' : 'Empleado activado')
  } catch (error: any) {
    notif.handleApiError(error, 'Error al cambiar el estado del empleado')
  }
}
</script>

<style scoped>
.table-premium th {
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  letter-spacing: 1px;
  font-size: 0.75rem;
  text-transform: uppercase;
}

.table-premium td {
  border-bottom: 1px solid rgba(255, 255, 255, 0.02);
}

.table-row-hover {
  transition: background-color 0.2s ease;
}

.table-row-hover:hover {
  background-color: rgba(255, 255, 255, 0.02);
}

.opacity-60 {
  opacity: 0.6;
}

.gap-6 {
  gap: 24px;
}
.gap-4 {
  gap: 16px;
}
</style>
