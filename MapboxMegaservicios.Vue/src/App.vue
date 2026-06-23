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
import { useNotificationStore } from '@/stores/notification'
import { startSignalR, stopSignalR, onSignalREvent, offSignalREvent } from '@/services/signalr'

const notifStore = useNotificationStore()

// Handler para nuevas ubicaciones recibidas por SignalR
function handleNuevaUbicacion(ubicacion: any) {
  // Mostrar notificación solo cuando cambia el estado de geocerca
  if (ubicacion.estado) {
    const esEntrada = ubicacion.estaEnGeocerca === true
    notifStore.mostrarExito(
      esEntrada ? 'Empleado en área' : 'Empleado fuera de área',
      `${ubicacion.empleadoNombre}: ${ubicacion.estado}`
    )
  }

  // Notificar si es posible spoofing
  if (ubicacion.isPossibleSpoofing) {
    notifStore.mostrarExito(
      '⚠️ Alerta de Spoofing GPS',
      `${ubicacion.empleadoNombre}: Movimiento sospechoso detectado`
    )
  }
}

onMounted(async () => {
  // Conectar SignalR y escuchar eventos
  await startSignalR()
  onSignalREvent('NuevaUbicacion', handleNuevaUbicacion)
})

onUnmounted(() => {
  offSignalREvent('NuevaUbicacion', handleNuevaUbicacion)
  stopSignalR()
})
</script>


<style>
#app {
  height: 100%;
}
</style>
