<template>
  <div>
    <v-row class="mb-6">
      <v-col cols="12" class="d-flex align-center flex-wrap gap-2">
        <h1 class="text-h5 font-weight-bold">Empleados</h1>
        <v-divider vertical class="mx-2"></v-divider>
        <span class="text-body-2 text-grey">Gestión de personal</span>

        <v-spacer></v-spacer>

        <!-- 🔍 Campo de búsqueda -->
        <v-text-field
          v-model="filtroEmpleados"
          label="Buscar empleado"
          density="comfortable"
          variant="outlined"
          prepend-inner-icon="mdi-magnify"
          hide-details
          class="me-2"
          style="max-width: 280px"
        ></v-text-field>

        <!-- Botón Nuevo -->
        <v-btn color="primary" prepend-icon="mdi-plus" @click="openDialog" class="text-none">
          Nuevo Empleado
        </v-btn>
      </v-col>
    </v-row>

    <!-- Tabla de Empleados -->
    <v-card elevation="2" rounded="lg">
      <v-card-text class="px-0">
        <v-table class="text-no-wrap">
          <thead>
            <tr class="text-uppercase" style="font-size: 0.9rem; color: #666">
              <th class="pl-6">Nombre Completo</th>
              <th>CI</th>
              <th>Teléfono</th>
              <th>Usuario</th>
              <th>Lugar de Trabajo</th>
              <th class="text-center">Estado</th>
              <th class="text-center">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="empleado in empleadosFiltrados"
              :key="empleado.id"
              :class="{ 'bg-grey-lighten-5': !empleado.activo }"
              style="border-bottom: 1px solid #eee"
            >
              <td class="pl-6">
                <span :class="{ 'text-grey': !empleado.activo }">
                  {{ empleado.nombreCompleto }}
                </span>
              </td>
              <td>{{ empleado.ci }}</td>
              <td>{{ empleado.telefono || '—' }}</td>
              <td>{{ empleado.usuario }}</td>
              <td>
                <div class="d-flex align-center">
                  <v-avatar size="20" color="primary" :class="{ 'opacity-50': !empleado.activo }">
                    <v-icon size="14" color="white">mdi-map-marker</v-icon>
                  </v-avatar>
                  <span class="ml-2">{{ empleado.lugarActual || 'Sin asignar' }}</span>
                  <v-btn
                    icon
                    size="x-small"
                    @click="openLugarDialog(empleado)"
                    color="primary"
                    variant="text"
                    :disabled="!empleado.activo"
                    class="ml-1"
                  >
                    <v-icon size="small">mdi-pencil</v-icon>
                  </v-btn>
                </div>
              </td>
              <td class="text-center">
                <v-chip
                  :color="empleado.activo ? 'success' : 'error'"
                  size="small"
                  variant="flat"
                  label
                >
                  {{ empleado.activo ? 'Activo' : 'Inactivo' }}
                </v-chip>
              </td>
              <td class="text-center">
                <v-btn
                  icon
                  size="small"
                  @click="editEmpleado(empleado)"
                  color="primary"
                  variant="text"
                  :disabled="!empleado.activo"
                  class="mx-1"
                >
                  <v-icon size="small">mdi-pencil</v-icon>
                </v-btn>
                <v-btn
                  icon
                  size="small"
                  @click="toggleActivo(empleado)"
                  :color="empleado.activo ? 'error' : 'success'"
                  variant="text"
                  class="mx-1"
                >
                  <v-icon size="small">
                    {{ empleado.activo ? 'mdi-account-off' : 'mdi-account-check' }}
                  </v-icon>
                </v-btn>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card-text>
    </v-card>

    <!-- Diálogo para Crear/Editar Empleado -->
    <v-dialog v-model="dialog" max-width="600" scrollable>
      <v-card>
        <v-card-title class="bg-primary text-white">
          {{ editMode ? 'Editar Empleado' : 'Nuevo Empleado' }}
        </v-card-title>
        <v-card-text class="pt-4">
          <v-form @submit.prevent="saveEmpleado">
            <v-row>
              <v-col cols="6">
                <v-text-field
                  v-model="form.paterno"
                  label="Apellido Paterno *"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="errors.paterno"
                ></v-text-field>
              </v-col>
              <v-col cols="6">
                <v-text-field
                  v-model="form.materno"
                  label="Apellido Materno"
                  variant="outlined"
                  density="comfortable"
                ></v-text-field>
              </v-col>
            </v-row>

            <v-row>
              <v-col cols="12">
                <v-text-field
                  v-model="form.nombres"
                  label="Nombres *"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="errors.nombres"
                ></v-text-field>
              </v-col>
            </v-row>

            <v-row>
              <v-col cols="6">
                <v-text-field
                  v-model="form.ci"
                  label="CI *"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="errors.ci"
                  :disabled="editMode"
                ></v-text-field>
              </v-col>
              <v-col cols="6">
                <v-text-field
                  v-model="form.telefono"
                  label="Teléfono"
                  variant="outlined"
                  density="comfortable"
                  :error-messages="errors.telefono"
                ></v-text-field>
              </v-col>
            </v-row>

            <!-- Selector de Lugar de Trabajo -->
            <v-row v-if="!editMode">
              <v-col cols="12">
                <v-select
                  v-model="form.idLugarTrabajo"
                  :items="lugaresOptions"
                  label="Lugar de Trabajo (opcional)"
                  variant="outlined"
                  density="comfortable"
                  clearable
                  item-title="nombre"
                  item-value="id"
                ></v-select>
              </v-col>
            </v-row>

            <v-alert v-if="errorMessage" type="error" class="mt-4" variant="tonal">
              {{ errorMessage }}
            </v-alert>
          </v-form>
        </v-card-text>
        <v-card-actions class="px-6 pb-6">
          <v-spacer></v-spacer>
          <v-btn @click="dialog = false" variant="tonal">Cancelar</v-btn>
          <v-btn color="primary" @click="saveEmpleado" :loading="loading">
            {{ editMode ? 'Actualizar' : 'Crear' }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Diálogo rápido para cambiar lugar -->
    <v-dialog v-model="lugarDialog" max-width="450">
      <v-card>
        <v-card-title class="d-flex align-center" style="font-size: 1.1rem">
          Cambiar Lugar de Trabajo
          <v-spacer></v-spacer>
          <v-btn icon @click="lugarDialog = false">
            <v-icon>mdi-close</v-icon>
          </v-btn>
        </v-card-title>
        <v-card-text>
          <div class="mb-4"><strong>Empleado:</strong> {{ selectedEmpleado?.nombreCompleto }}</div>

          <v-select
            v-model="nuevoLugarId"
            :items="lugaresOptions"
            label="Seleccionar Lugar"
            variant="outlined"
            density="comfortable"
            clearable
            item-title="nombre"
            item-value="id"
          >
            <template #item="{ props, item }">
              <v-list-item v-bind="props">
                <template #title>
                  <div>{{ item.raw.nombre }}</div>
                </template>
              </v-list-item>
            </template>
          </v-select>

          <v-textarea
            v-model="observacionesLugar"
            label="Observaciones (opcional)"
            variant="outlined"
            density="comfortable"
            rows="2"
            class="mt-4"
          ></v-textarea>
        </v-card-text>
        <v-card-actions class="px-6 pb-6">
          <v-btn @click="lugarDialog = false" variant="tonal">Cancelar</v-btn>
          <v-btn color="primary" @click="cambiarLugar" :loading="cambiandoLugar">
            Guardar Cambio
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

const empleados = ref<Empleado[]>([])
const lugares = ref<LugarTrabajo[]>([])
const dialog = ref(false)
const lugarDialog = ref(false)
const editMode = ref(false)
const loading = ref(false)
const cambiandoLugar = ref(false)
const errorMessage = ref('')
const editingId = ref<number | null>(null)
const selectedEmpleado = ref<Empleado | null>(null)
const nuevoLugarId = ref<number | null>(null)
const observacionesLugar = ref('')
const filtroEmpleados = ref('')
// Formulario principal
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

// Opciones para el select de lugares
const lugaresOptions = computed(() => {
  return [
    { id: null, nombre: 'Sin asignar' },
    ...lugares.value.map((l) => ({
      id: l.id,
      nombre: l.nombre,
      direccion: l.direccion,
    })),
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
  await Promise.all([loadEmpleados(), loadLugares()])
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
  // NO enviar idLugarTrabajo en edición (se cambia con el botón rápido)
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
  // Validación
  let hasError = false

  if (!form.paterno.trim()) {
    errors.paterno = 'Apellido paterno es requerido'
    hasError = true
  }
  if (!form.nombres.trim()) {
    errors.nombres = 'Nombres son requeridos'
    hasError = true
  }
  if (!form.ci.trim()) {
    errors.ci = 'CI es requerido'
    hasError = true
  }

  if (hasError) return

  loading.value = true
  errorMessage.value = ''

  try {
    if (editMode.value && editingId.value) {
      // Actualizar empleado (sin IdRol)
      await api.put(`/admin/empleados/${editingId.value}`, {
        paterno: form.paterno,
        materno: form.materno,
        nombres: form.nombres,
        telefono: form.telefono,
        activo: true, // Mantener activo
      })
    } else {
      // Crear nuevo empleado
      const payload: any = {
        paterno: form.paterno,
        materno: form.materno,
        nombres: form.nombres,
        ci: form.ci,
        telefono: form.telefono,
        idRol: 2, // Rol Empleado (fijo)
      }

      // Solo agregar lugar si se seleccionó
      if (form.idLugarTrabajo) {
        payload.idLugarTrabajo = form.idLugarTrabajo
      }

      await api.post('/admin/empleados', payload)
    }

    dialog.value = false
    await loadEmpleados()
  } catch (error: any) {
    errorMessage.value = error.response?.data?.message || 'Error guardando empleado'
  } finally {
    loading.value = false
  }
}

async function cambiarLugar() {
  if (!selectedEmpleado.value) return

  cambiandoLugar.value = true

  try {
    await api.patch(`/admin/empleados/${selectedEmpleado.value.id}/lugar-trabajo`, {
      lugarTrabajoId: nuevoLugarId.value,
      observaciones: observacionesLugar.value || 'Cambio de lugar desde interfaz',
    })

    lugarDialog.value = false
    await loadEmpleados()
  } catch (error: any) {
    alert(error.response?.data?.message || 'Error cambiando lugar')
  } finally {
    cambiandoLugar.value = false
  }
}

async function toggleActivo(empleado: Empleado) {
  const confirmMessage = empleado.activo
    ? `¿Desactivar a ${empleado.nombreCompleto}?`
    : `¿Activar a ${empleado.nombreCompleto}?`

  if (!confirm(confirmMessage)) return

  try {
    await api.patch(`/admin/empleados/${empleado.id}/estadoemp`)
    await loadEmpleados()
  } catch (error: any) {
    alert(error.response?.data?.message || 'Error cambiando estado')
  }
}
</script>

<style scoped>
.text-no-wrap td {
  white-space: nowrap;
}

.opacity-50 {
  opacity: 0.5;
}
</style>
