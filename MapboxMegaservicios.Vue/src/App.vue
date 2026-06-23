<template>
  <router-view />
  
  <!-- Snackbar Global -->
  <v-snackbar
    v-model="notifStore.notif.mostrar"
    :color="notifStore.notif.color"
    location="bottom end"
    timeout="5000"
    vertical
  >
    <div class="text-subtitle-1 font-weight-bold pb-2">
      <v-icon start icon="mdi-bell-ring"></v-icon>
      {{ notifStore.notif.titulo }}
    </div>
    <p>{{ notifStore.notif.mensaje }}</p>
    
    <template v-slot:actions>
      <v-btn variant="text" @click="notifStore.cerrar()">Cerrar</v-btn>
    </template>
  </v-snackbar>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import api from '@/services/api'
import { useNotificationStore } from '@/stores/notification'

const notifStore = useNotificationStore()

let pollingInterval: any = null
let lastCheck = new Date().toISOString()

onMounted(() => { startPolling() })
onUnmounted(() => { stopPolling() })

function startPolling() {
  pollingInterval = setInterval(async () => {
     try {
       const response = await api.get(`/ubicaciones/alertas?desde=${lastCheck}&take=1`)
       const alertas = response.data
       
       if (alertas && alertas.length > 0) {
          const alerta = alertas[0]
          
          // Use the alert's timestamp + 1ms to avoid fetching the same alert
          const alertTime = new Date(alerta.fechaHora)
          alertTime.setMilliseconds(alertTime.getMilliseconds() + 1)
          
          // Only update if the new time is strictly greater (handles unordered arrivals loosely)
          if (alertTime.toISOString() > lastCheck) {
            lastCheck = alertTime.toISOString()
          }
          
          const esEntrada = (alerta.tipoAlerta || '').toLowerCase().includes('dentro')
          notifStore.mostrarExito(
            esEntrada ? 'Empleado en área' : 'Empleado fuera',
            `${alerta.empleadoNombre}: ${alerta.observaciones}`
          )
       }
       // If no alerts, keep lastCheck exactly as it was, to prevent missing alerts if client clock is behind.
     } catch (e) {
       // silent polling error
     }
  }, 5000)
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
