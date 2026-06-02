<template>
  <v-container fluid class="px-md-6 pb-6">
    <!-- Header -->
    <v-row class="mb-4 align-center">
      <v-col>
        <h1 class="text-h3 font-weight-bold" style="color: var(--v-theme-primary); letter-spacing: -1px;">
          Panorama General
        </h1>
        <p class="text-subtitle-1 text-medium-emphasis mt-1">
          Monitoreo en tiempo real del personal de MegaServicios MC
        </p>
      </v-col>
      <v-col cols="auto">
        <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" @click="fetchData" :loading="loading" class="text-none font-weight-bold" rounded="lg">
          Actualizar Datos
        </v-btn>
      </v-col>
    </v-row>

    <!-- Alertas Críticas (Spoofing) -->
    <v-fade-transition group>
      <div v-for="(alerta, index) in spoofingAlerts" :key="index" class="mb-6">
        <v-card class="bg-gradient-error glass-panel" elevation="12" rounded="xl">
          <v-card-item>
            <template v-slot:prepend>
              <v-icon icon="mdi-shield-alert-outline" size="x-large" class="mr-3" color="white"></v-icon>
            </template>
            <v-card-title class="text-white text-h6 font-weight-bold">
              ¡Detección de Fraude GPS (Spoofing)!
            </v-card-title>
            <v-card-subtitle class="text-white opacity-90 mt-1">
              Actividad anómala detectada para el empleado <strong>{{ alerta.empleadoNombre }}</strong>
            </v-card-subtitle>
          </v-card-item>
          <v-card-text class="text-white pt-2">
            El sistema ha registrado un salto de ubicación que excede la velocidad físicamente posible.
            <div class="mt-2 d-flex align-center">
              <v-icon icon="mdi-map-marker-radius" size="small" class="mr-2"></v-icon>
              <span>Lugar asignado: {{ alerta.lugarTrabajo }}</span>
              <span class="mx-3">|</span>
              <v-icon icon="mdi-clock-outline" size="small" class="mr-2"></v-icon>
              <span>{{ formatDate(alerta.fechaHora) }}</span>
            </div>
          </v-card-text>
        </v-card>
      </div>
    </v-fade-transition>

    <!-- Estadísticas Rápidas (Tarjetas) -->
    <v-row class="mb-6">
      <v-col cols="12" sm="6" md="3">
        <v-card class="bg-gradient-primary h-100" elevation="4" rounded="xl">
          <v-card-text class="d-flex align-center justify-space-between pa-6">
            <div>
              <div class="text-subtitle-1 font-weight-medium opacity-80">Total Empleados</div>
              <div class="text-h3 font-weight-black mt-2">{{ stats.total }}</div>
            </div>
            <v-icon icon="mdi-account-group" size="64" class="opacity-40"></v-icon>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card class="bg-gradient-success h-100" elevation="4" rounded="xl">
          <v-card-text class="d-flex align-center justify-space-between pa-6">
            <div>
              <div class="text-subtitle-1 font-weight-medium opacity-80">En Geocerca</div>
              <div class="text-h3 font-weight-black mt-2">{{ stats.dentro }}</div>
            </div>
            <v-icon icon="mdi-map-marker-check" size="64" class="opacity-40"></v-icon>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card class="bg-gradient-error h-100" elevation="4" rounded="xl">
          <v-card-text class="d-flex align-center justify-space-between pa-6">
            <div>
              <div class="text-subtitle-1 font-weight-medium opacity-80">Fuera de Ruta</div>
              <div class="text-h3 font-weight-black mt-2">{{ stats.fuera }}</div>
            </div>
            <v-icon icon="mdi-map-marker-alert" size="64" class="opacity-40"></v-icon>
          </v-card-text>
        </v-card>
      </v-col>
      <v-col cols="12" sm="6" md="3">
        <v-card class="bg-gradient-warning h-100" elevation="4" rounded="xl">
          <v-card-text class="d-flex align-center justify-space-between pa-6">
            <div>
              <div class="text-subtitle-1 font-weight-medium opacity-80">Alertas Hoy</div>
              <div class="text-h3 font-weight-black mt-2">{{ stats.alertas }}</div>
            </div>
            <v-icon icon="mdi-bell-ring" size="64" class="opacity-40"></v-icon>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Gráficos y Actividad Reciente -->
    <v-row>
      <!-- Gráfico -->
      <v-col cols="12" md="4">
        <v-card elevation="2" rounded="xl" class="h-100">
          <v-card-title class="pa-6 pb-2 text-h6 font-weight-bold">
            Distribución de Personal
          </v-card-title>
          <v-card-text class="pa-6 pt-0 d-flex justify-center align-center h-100" style="min-height: 300px;">
            <div v-if="loading" class="text-center w-100">
              <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
            </div>
            <Doughnut v-else-if="chartData" :data="chartData" :options="chartOptions" />
            <div v-else class="text-medium-emphasis">Sin datos para mostrar</div>
          </v-card-text>
        </v-card>
      </v-col>

      <!-- Últimas Alertas -->
      <v-col cols="12" md="8">
        <v-card elevation="2" rounded="xl" class="h-100">
          <v-card-title class="pa-6 pb-2 text-h6 font-weight-bold d-flex justify-space-between align-center">
            <span>Últimas Alertas</span>
            <v-chip color="primary" variant="flat" size="small">{{ alertas.length }} eventos</v-chip>
          </v-card-title>
          <v-card-text class="pa-0">
            <div v-if="loading" class="pa-6 text-center">
              <v-progress-circular indeterminate color="primary"></v-progress-circular>
            </div>
            <v-list v-else-if="alertas.length > 0" lines="two" class="bg-transparent">
              <template v-for="(alerta, index) in alertas" :key="index">
                <v-list-item class="px-6 py-3">
                  <template v-slot:prepend>
                    <v-avatar :color="getAlertColor(alerta.alerta)" variant="tonal" rounded="lg">
                      <v-icon :icon="getAlertIcon(alerta.alerta)"></v-icon>
                    </v-avatar>
                  </template>
                  <v-list-item-title class="font-weight-bold mb-1">
                    {{ alerta.empleado }}
                    <v-chip class="ml-2" size="x-small" :color="getAlertColor(alerta.alerta)" variant="flat">
                      {{ alerta.alerta }}
                    </v-chip>
                  </v-list-item-title>
                  <v-list-item-subtitle>
                    <v-icon icon="mdi-map-marker" size="x-small" class="mr-1"></v-icon>
                    {{ alerta.lugar || 'Ubicación desconocida' }}
                    <span class="mx-2">•</span>
                    {{ alerta.observaciones }}
                  </v-list-item-subtitle>
                  <template v-slot:append>
                    <div class="text-caption text-medium-emphasis d-flex align-center">
                      <v-icon icon="mdi-clock-outline" size="x-small" class="mr-1"></v-icon>
                      {{ formatTimeAgo(alerta.fechaHora) }}
                    </div>
                  </template>
                </v-list-item>
                <v-divider v-if="index < alertas.length - 1" inset class="ml-16 mr-6"></v-divider>
              </template>
            </v-list>
            <div v-else class="pa-10 text-center text-medium-emphasis">
              <v-icon icon="mdi-check-circle-outline" size="64" color="success" class="mb-4 opacity-50"></v-icon>
              <h3>Todo en orden</h3>
              <p>No se han registrado alertas recientes.</p>
            </div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import api from '@/services/api'
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js'
import { Doughnut } from 'vue-chartjs'

ChartJS.register(ArcElement, Tooltip, Legend)

const loading = ref(true)

const stats = ref({
  total: 0,
  dentro: 0,
  fuera: 0,
  alertas: 0
})

const alertas = ref<any[]>([])
const spoofingAlerts = ref<any[]>([])

// Chart.js Data and Options
const chartData = computed(() => {
  if (stats.value.dentro === 0 && stats.value.fuera === 0) return null;
  return {
    labels: ['En Geocerca', 'Fuera de Ruta'],
    datasets: [
      {
        backgroundColor: ['#10B981', '#EF4444'], // Success y Error del theme
        borderColor: ['#059669', '#B91C1C'],
        data: [stats.value.dentro, stats.value.fuera],
        borderWidth: 2,
        hoverOffset: 4
      }
    ]
  }
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: {
        font: {
          family: 'Inter',
          size: 14
        },
        usePointStyle: true,
        padding: 20
      }
    }
  },
  cutout: '70%'
}

// Formatters and Helpers
const formatDate = (date: string | Date) => {
  return new Date(date).toLocaleString('es-ES', { 
    dateStyle: 'medium', 
    timeStyle: 'short' 
  })
}

const formatTimeAgo = (dateStr: string | Date) => {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  
  if (diffMins < 1) return 'Hace un momento'
  if (diffMins < 60) return `Hace ${diffMins} min`
  const diffHrs = Math.floor(diffMins / 60)
  if (diffHrs < 24) return `Hace ${diffHrs} h`
  return formatDate(date)
}

const getAlertColor = (tipo: string) => {
  const t = (tipo || '').toLowerCase()
  if (t.includes('dentro')) return 'success'
  if (t.includes('fuera')) return 'error'
  return 'warning'
}

const getAlertIcon = (tipo: string) => {
  const t = (tipo || '').toLowerCase()
  if (t.includes('dentro')) return 'mdi-login'
  if (t.includes('fuera')) return 'mdi-logout'
  return 'mdi-alert'
}

const fetchData = async () => {
  loading.value = true
  try {
    const [statsResponse, spoofingResponse] = await Promise.all([
      api.get('/admin/dashboard/estadisticas'),
      api.get('/ubicaciones/spoofing').catch(() => ({ data: [] }))
    ])
    
    const data = statsResponse.data
    stats.value.total = data?.totalEmpleados ?? data?.TotalEmpleados ?? 0
    stats.value.dentro = data?.empleadosEnGeocerca ?? data?.EmpleadosEnGeocerca ?? 0
    stats.value.fuera = data?.empleadosFueraGeocerca ?? data?.EmpleadosFueraGeocerca ?? 0
    stats.value.alertas = data?.alertasHoy ?? data?.AlertasHoy ?? 0

    alertas.value = data?.ultimasAlertas || data?.UltimasAlertas || []
    spoofingAlerts.value = spoofingResponse.data || []
  } catch (error) {
    console.error('Error cargando dashboard:', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchData()
})
</script>

<style scoped>
/* Optional: specific tweaks for Dashboard */
</style>
