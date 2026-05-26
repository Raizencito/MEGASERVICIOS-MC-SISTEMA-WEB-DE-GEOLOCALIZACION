<template>
  <v-container class="fill-height" fluid>
    <v-row align="center" justify="center">
      <v-col cols="12" sm="8" md="6" lg="4">
        <v-card class="elevation-6 rounded-xl" :style="{ maxWidth: '420px' }">
          <!-- Logo -->
          <div class="d-flex justify-center mt-6">
            <img
              src="@/assets/logo-megaservicios.png"
              alt="Mega Servicios"
              style="width: 280px; height: auto; filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.1))"
            />
          </div>

          <v-divider class="my-4"></v-divider>

          <!-- Formulario -->
          <v-card-text>
            <v-form @submit.prevent="login">
              <v-text-field
                v-model="form.usuario"
                label="Usuario"
                prepend-icon="mdi-account"
                variant="outlined"
                required
                :error-messages="errors.usuario"
                class="mb-3"
                density="comfortable"
              ></v-text-field>

              <v-text-field
                v-model="form.password"
                label="Contraseña"
                prepend-icon="mdi-lock"
                type="password"
                variant="outlined"
                required
                :error-messages="errors.password"
                class="mb-4"
                density="comfortable"
              ></v-text-field>

              <v-alert v-if="errorMessage" type="error" class="mb-4" variant="tonal">
                {{ errorMessage }}
              </v-alert>

              <v-btn
                type="submit"
                color="#FF6B00"
                block
                size="large"
                :loading="loading"
                :disabled="loading"
                class="text-white font-weight-bold text-capitalize"
                style="border-radius: 8px; transition: all 0.3s ease"
                @mouseover="hover = true"
                @mouseleave="hover = false"
              >
                {{ loading ? 'Iniciando...' : 'Iniciar Sesión' }}
              </v-btn>
            </v-form>
          </v-card-text>

          <!-- Información de acceso -->
          <v-card-actions class="justify-center pb-6">
            <v-alert
              type="info"
              variant="outlined"
              density="compact"
              border="start"
              icon="mdi-information"
              color="#1A5276"
              class="text-center w-100"
            >
              Usuario: <strong>admin</strong> | Contraseña: <strong>admin123</strong>
            </v-alert>
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import authService from '@/services/auth'

const router = useRouter()
const loading = ref(false)
const errorMessage = ref('')
const hover = ref(false)

const form = reactive({
  usuario: 'admin',
  password: 'admin123',
})

const errors = reactive({
  usuario: '',
  password: '',
})

async function login() {
  // Validación simple
  if (!form.usuario.trim()) {
    errors.usuario = 'Usuario es requerido'
    return
  }
  if (!form.password.trim()) {
    errors.password = 'Contraseña es requerida'
    return
  }

  loading.value = true
  errorMessage.value = ''

  try {
    const success = await authService.login({
      usuario: form.usuario,
      password: form.password,
    })

    if (success) {
      router.push('/')
    } else {
      errorMessage.value = 'Credenciales incorrectas'
    }
  } catch (error: any) {
    errorMessage.value = error.response?.data?.message || 'Error de conexión'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.fill-height {
  min-height: 100vh;
  background: linear-gradient(135deg, #ff6b00 0%, #1a5276 100%);
  padding: 20px;
}

.v-card {
  backdrop-filter: blur(10px);
  background: rgba(255, 255, 255, 0.95);
}

.v-btn {
  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease;
}

.v-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(255, 107, 0, 0.3);
}

.v-btn:active {
  transform: translateY(0);
}

.v-alert {
  border-left-width: 4px !important;
}

/* Estilos para el logo si no tienes el archivo, puedes usar un placeholder */
/* Si no tienes el logo en assets, reemplaza la img por este div temporal */
/* 
<div class="logo-placeholder d-flex justify-center align-center" style="width: 280px; height: 120px; background: #f0f0f0; border-radius: 8px;">
  <span class="text-h6 font-weight-bold">Mega Servicios</span>
</div>
*/
</style>
