<template>
  <div>
    <h1 class="mb-6">Dashboard</h1>

    <!-- Estadísticas -->
    <v-row class="mb-6">
      <v-col cols="12" sm="6" md="3" v-for="stat in stats" :key="stat.title">
        <v-card :color="stat.color" dark>
          <v-card-text class="text-center">
            <div class="text-h4">{{ stat.value }}</div>
            <div>{{ stat.title }}</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Últimas alertas -->
    <v-card class="mb-6">
      <v-card-title>
        <v-icon start>mdi-alert</v-icon>
        Últimas Alertas
      </v-card-title>
      <v-card-text>
        <v-table v-if="alertas.length > 0">
          <thead>
            <tr>
              <th>Empleado</th>
              <th>Lugar</th>
              <th>Tipo</th>
              <th>Fecha/Hora</th>
              <th>Observaciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="alerta in alertas" :key="alerta.id">
              <td>{{ alerta.empleado }}</td>
              <td>{{ alerta.lugar || 'Sin asignar' }}</td>
              <td>
                <v-chip :color="alerta.alerta === 'DENTRO' || alerta.alerta.includes('Dentro') ? 'success' : 'error'" size="small">
                  {{ alerta.alerta }}
                </v-chip>
              </td>
              <td>{{ formatDate(alerta.fechaHora) }}</td>
              <td>{{ alerta.observaciones }}</td>
            </tr>
          </tbody>
        </v-table>
        <div v-else-if="loading">
          <v-progress-circular indeterminate color="primary"></v-progress-circular>
          <span class="ml-2">Cargando alertas...</span>
        </div>
        <v-alert v-else type="info"> No hay alertas recientes </v-alert>
      </v-card-text>
    </v-card>

    <!-- Mapa rápido -->
    <v-card>
      <v-card-title>
        <v-icon start>mdi-map</v-icon>
        Mapa de Ubicaciones
      </v-card-title>
      <v-card-text>
        <div class="text-center pa-6">
          <v-icon size="100" color="primary">mdi-map-marker-radius</v-icon>
          <p class="mt-4">Mapa interactivo aquí (integrar Mapbox después)</p>
          <v-btn color="primary" to="/lugares">
            <v-icon start>mdi-map</v-icon>
            Ver Mapa Completo
          </v-btn>
        </div>
      </v-card-text>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '@/services/api'

const loading = ref(true)
const stats = ref([
  { title: 'Empleados Totales', value: 0, color: 'primary' },
  { title: 'En Geocerca', value: 0, color: 'success' },
  { title: 'Fuera de Geocerca', value: 0, color: 'error' },
  { title: 'Alertas Hoy', value: 0, color: 'warning' },
])

const alertas = ref<any[]>([])

const formatDate = (date: string | Date) => {
  return new Date(date).toLocaleString('es-ES')
}

onMounted(async () => {
  loading.value = true
  try {
    const response = await api.get('/admin/dashboard/estadisticas')
    const data = response.data

    // Usar valores por defecto si no existen (Soporte dual Camel/Pascal para robustez)
    stats.value[0].value = data?.totalEmpleados ?? data?.TotalEmpleados ?? 0
    stats.value[1].value = data?.empleadosEnGeocerca ?? data?.EmpleadosEnGeocerca ?? 0
    stats.value[2].value = data?.empleadosFueraGeocerca ?? data?.EmpleadosFueraGeocerca ?? 0
    stats.value[3].value = data?.alertasHoy ?? data?.AlertasHoy ?? 0

    alertas.value = data?.ultimasAlertas || data?.UltimasAlertas || []
  } catch (error) {
    console.error('Error cargando dashboard:', error)
  } finally {
    loading.value = false
  }
})
</script>
