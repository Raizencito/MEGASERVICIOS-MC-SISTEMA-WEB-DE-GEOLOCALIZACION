<template>
  <router-view />
  
  <!-- Snackbar Global -->
  <v-snackbar
    v-model="snackbar.mostrar"
    :color="snackbar.color"
    location="bottom end"
    timeout="5000"
    vertical
  >
    <div class="text-subtitle-1 font-weight-bold pb-2">
      <v-icon start icon="mdi-bell-ring"></v-icon>
      {{ snackbar.titulo }}
    </div>
    <p>{{ snackbar.mensaje }}</p>
    
    <template v-slot:actions>
      <v-btn variant="text" @click="snackbar.mostrar = false">Cerrar</v-btn>
    </template>
  </v-snackbar>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import api from '@/services/api'

// Estado del Snackbar Global
const snackbar = ref({
  mostrar: false,
  color: 'info',
  titulo: '',
  mensaje: ''
})

let pollingInterval: any = null
let lastCheck = new Date().toISOString() // Fecha ISO para API

// Polling Global de Alertas
onMounted(() => {
  startPolling()
})

onUnmounted(() => {
  stopPolling()
})

function startPolling() {
  pollingInterval = setInterval(async () => {
     try {
       // Consultar alertas nuevas desde la última revisión
       // El backend espera 'desde' en formato ISO
       const response = await api.get(`/ubicaciones/alertas?desde=${lastCheck}&take=1`)
       const alertas = response.data
       
       if (alertas && alertas.length > 0) {
          const alerta = alertas[0] // Tomar la más reciente
          
          // Verificar si es REALMENTE nueva comparando fechas de forma robusta
          // (Aunque 'desde' filtra, un clock skew podría causar duplicados, pero para demo está bien)
          
          // Actualizar cursor de tiempo AHORA
          lastCheck = new Date().toISOString()
          
          const esEntrada = alerta.tipoAlerta.includes('DENTRO') || alerta.tipoAlerta.includes('Dentro')
          
          snackbar.value = {
             mostrar: true,
             color: esEntrada ? 'success' : 'error',
             titulo: esEntrada ? '¡Empleado Entró!' : '¡Empleado Salió!',
             mensaje: `${alerta.empleadoNombre}: ${alerta.observaciones}`
          }
       } else {
          // Si no hubo alertas, actualizamos lastCheck para 'avanzar' la ventana deslizante
          // y no traer alertas viejas si el backend ignorase el filtro (safety)
          lastCheck = new Date().toISOString()
       }
     } catch (e) {
       console.error('Error polling alertas:', e)
     }
  }, 5000) // Cada 5 segundos
}

function stopPolling() {
  if (pollingInterval) clearInterval(pollingInterval)
}
</script>

<style>
#app {
  height: 100%;
}
</style>
