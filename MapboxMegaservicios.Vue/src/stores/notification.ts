import { ref } from 'vue'
import { defineStore } from 'pinia'

export interface Notification {
  mostrar: boolean
  color: 'success' | 'error' | 'info' | 'warning'
  titulo: string
  mensaje: string
}

export const useNotificationStore = defineStore('notification', () => {
  const notif = ref<Notification>({
    mostrar: false,
    color: 'info',
    titulo: '',
    mensaje: '',
  })

  function mostrarExito(titulo: string, mensaje?: string) {
    notif.value = { mostrar: true, color: 'success', titulo, mensaje: mensaje || '' }
  }

  function mostrarError(titulo: string, mensaje?: string) {
    notif.value = { mostrar: true, color: 'error', titulo, mensaje: mensaje || '' }
  }

  function mostrarInfo(titulo: string, mensaje?: string) {
    notif.value = { mostrar: true, color: 'info', titulo, mensaje: mensaje || '' }
  }

  function mostrarAdvertencia(titulo: string, mensaje?: string) {
    notif.value = { mostrar: true, color: 'warning', titulo, mensaje: mensaje || '' }
  }

  function handleApiError(error: any, mensajePorDefecto: string) {
    const msg = error?.response?.data?.message || error?.response?.data?.error || mensajePorDefecto
    mostrarError('Error', msg)
  }

  function cerrar() {
    notif.value.mostrar = false
  }

  return { notif, mostrarExito, mostrarError, mostrarInfo, mostrarAdvertencia, handleApiError, cerrar }
})
