<template>
  <div class="login-wrapper">
    <!-- Orbes de luz de fondo para el efecto de profundidad (Glow/Mesh Gradient) -->
    <div class="glow-orb orb-1"></div>
    <div class="glow-orb orb-2"></div>
    
    <v-container class="fill-height" fluid>
      <v-row align="center" justify="center">
        <v-col cols="12" sm="8" md="6" lg="4" class="d-flex justify-center">
          <v-card class="login-card pa-6 rounded-xl text-center" :style="{ width: '100%', maxWidth: '420px' }">
            <!-- Header / Logo -->
            <div class="d-flex flex-column align-center mt-4 mb-6">
              <div class="logo-container mb-4">
                <img
                  src="@/assets/logo-megaservicios.png"
                  alt="Mega Servicios"
                  class="login-logo"
                />
              </div>
              <h1 class="text-h5 font-weight-black text-white tracking-wide">
                SISTEMA DE GEOLOCALIZACIÓN
              </h1>
              <p class="text-caption text-medium-emphasis mt-1">
                Monitoreo y Control de Personal de Campo
              </p>
            </div>

            <!-- Formulario -->
            <v-card-text class="px-0">
              <v-form @submit.prevent="login" class="text-left">
                <v-text-field
                  v-model="form.usuario"
                  label="Nombre de Usuario"
                  prepend-inner-icon="mdi-account-outline"
                  variant="outlined"
                  required
                  :error-messages="errors.usuario"
                  class="mb-3 custom-input"
                  density="comfortable"
                  color="primary"
                  hide-details="auto"
                ></v-text-field>

                <v-text-field
                  v-model="form.password"
                  label="Contraseña"
                  prepend-inner-icon="mdi-lock-outline"
                  type="password"
                  variant="outlined"
                  required
                  :error-messages="errors.password"
                  class="mb-5 custom-input"
                  density="comfortable"
                  color="primary"
                  hide-details="auto"
                ></v-text-field>

                <!-- Mensaje de Error con estilo -->
                <transition name="slide-up">
                  <v-alert v-if="errorMessage" type="error" class="mb-4 text-caption rounded-lg" variant="tonal" density="compact">
                    {{ errorMessage }}
                  </v-alert>
                </transition>

                <v-btn
                  type="submit"
                  block
                  size="large"
                  :loading="loading"
                  :disabled="loading"
                  class="login-btn text-white font-weight-bold text-none mt-2"
                >
                  {{ loading ? 'Validando credenciales...' : 'Ingresar al Panel' }}
                  <template v-slot:loader>
                    <v-progress-circular indeterminate color="white" size="22" width="2"></v-progress-circular>
                  </template>
                </v-btn>
              </v-form>
            </v-card-text>

            <!-- Credenciales de demostración de manera elegante -->
            <v-card-actions class="justify-center px-0 pt-4">
              <div class="credentials-alert py-2 px-4 w-100 d-flex align-center justify-center gap-2">
                <v-icon size="16" color="primary">mdi-shield-key-outline</v-icon>
                <span class="text-caption text-medium-emphasis">
                  Demo: <strong class="text-white">admin</strong> / <strong class="text-white">admin123</strong>
                </span>
              </div>
            </v-card-actions>
          </v-card>
        </v-col>
      </v-row>
    </v-container>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import authService from '@/services/auth'

const router = useRouter()
const loading = ref(false)
const errorMessage = ref('')

const form = reactive({
  usuario: 'admin',
  password: 'admin123',
})

const errors = reactive({
  usuario: '',
  password: '',
})

async function login() {
  // Limpiar errores previos
  errors.usuario = ''
  errors.password = ''
  errorMessage.value = ''

  if (!form.usuario.trim()) {
    errors.usuario = 'Usuario es requerido'
    return
  }
  if (!form.password.trim()) {
    errors.password = 'Contraseña es requerida'
    return
  }

  loading.value = true

  try {
    const success = await authService.login({
      usuario: form.usuario,
      password: form.password,
    })

    if (success) {
      router.push('/')
    } else {
      errorMessage.value = 'Usuario o contraseña incorrectos.'
    }
  } catch (error: any) {
    errorMessage.value = error.response?.data?.message || 'Error de conexión con el servidor.'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
/* Contenedor principal con fondo oscuro profundo */
.login-wrapper {
  position: relative;
  min-height: 100vh;
  width: 100vw;
  background-color: #0B0F19;
  overflow: hidden;
  display: flex;
  align-items: center;
}

/* Orbes con efecto Glassmorphism de fondo */
.glow-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(100px);
  opacity: 0.4;
  pointer-events: none;
  z-index: 0;
  transition: all 0.5s ease;
}

.orb-1 {
  width: 400px;
  height: 400px;
  background: radial-gradient(circle, #6366F1 0%, rgba(99, 102, 241, 0) 70%);
  top: -100px;
  right: -50px;
}

.orb-2 {
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, #ff6b00 0%, rgba(255, 107, 0, 0) 70%);
  bottom: -150px;
  left: -100px;
}

/* Contenedor de la vista */
.fill-height {
  z-index: 1;
  position: relative;
}

/* Tarjeta de Login Glassmorphic */
.login-card {
  background: rgba(30, 41, 59, 0.45) !important;
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border: 1px solid rgba(255, 255, 255, 0.08) !important;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.4) !important;
}

/* Contenedor del Logo */
.logo-container {
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: 16px;
  padding: 8px;
}

.login-logo {
  max-width: 250px;
  height: auto;
  filter: drop-shadow(0 4px 10px rgba(0, 0, 0, 0.3));
}

.tracking-wide {
  letter-spacing: 1.5px !important;
  color: #F8FAFC !important;
  font-size: 1.1rem !important;
}

/* Personalización de los inputs de Vuetify */
.custom-input :deep(.v-field) {
  background: rgba(15, 23, 42, 0.4) !important;
  border-radius: 12px !important;
  border: 1px solid rgba(255, 255, 255, 0.08) !important;
  color: #F8FAFC !important;
  transition: all 0.3s ease;
}

.custom-input :deep(.v-field__outline) {
  display: none !important; /* Quitar borde predeterminado */
}

.custom-input :deep(.v-field--focused) {
  border: 1px solid var(--v-theme-primary) !important;
  box-shadow: 0 0 12px rgba(99, 102, 241, 0.25) !important;
}

.custom-input :deep(.v-label) {
  color: rgba(255, 255, 255, 0.5) !important;
}

.custom-input :deep(.v-field__prepend-inner) {
  color: var(--v-theme-primary) !important;
}

/* Botón de login con gradiente y efecto de elevación */
.login-btn {
  background: linear-gradient(135deg, #6366F1 0%, #4F46E5 100%) !important;
  border: none !important;
  border-radius: 12px !important;
  font-weight: 700 !important;
  letter-spacing: 0.5px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1) !important;
  box-shadow: 0 4px 15px rgba(99, 102, 241, 0.35) !important;
  height: 48px !important;
}

.login-btn:hover:not(:disabled) {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(99, 102, 241, 0.5) !important;
  filter: brightness(1.1);
}

.login-btn:active:not(:disabled) {
  transform: translateY(0);
}

.login-btn:disabled {
  opacity: 0.6;
  background: #334155 !important;
}

/* Alerta de credenciales */
.credentials-alert {
  background: rgba(99, 102, 241, 0.08) !important;
  border: 1px solid rgba(99, 102, 241, 0.15) !important;
  border-radius: 12px;
}

/* Animaciones */
.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.3s ease;
}

.slide-up-enter-from,
.slide-up-leave-to {
  transform: translateY(10px);
  opacity: 0;
}
</style>
