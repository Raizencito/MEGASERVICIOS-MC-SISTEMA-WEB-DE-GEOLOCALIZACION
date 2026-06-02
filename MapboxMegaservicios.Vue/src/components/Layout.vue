<template>
  <v-app class="bg-background">
    <!-- Navigation Drawer con diseño Dark/Glassmorphism -->
    <v-navigation-drawer
      v-model="drawer"
      :rail="rail"
      permanent
      @click="rail = false"
      rail-width="72"
      width="280"
      class="drawer-premium"
      :elevation="12"
    >
      <!-- Logo y Header -->
      <div class="pa-4 d-flex align-center justify-center" :class="{ 'px-2': rail }">
        <img 
          src="@/assets/logo-megaservicios.png" 
          alt="Mega Servicios Logo" 
          class="loguito" 
          :style="rail ? 'max-width: 40px;' : 'max-width: 200px;'"
        />
      </div>

      <v-divider class="mx-4 mb-4 border-opacity-25"></v-divider>

      <!-- Menu Items -->
      <v-list density="comfortable" nav class="px-3">
        <v-list-item
          v-for="item in menuItems"
          :key="item.title"
          :to="item.to"
          :prepend-icon="item.icon"
          :title="rail ? '' : item.title"
          class="mb-2 menu-item"
          rounded="lg"
          active-class="menu-item-active"
          exact
        >
          <template v-if="rail" #title>
            <v-tooltip activator="parent" location="right" open-delay="400">
              {{ item.title }}
            </v-tooltip>
          </template>
        </v-list-item>
      </v-list>

      <v-spacer></v-spacer>

      <!-- Footer con botón de logout elegante -->
      <template v-slot:append>
        <div class="pa-4">
          <v-divider class="mb-4 border-opacity-25"></v-divider>

          <!-- Perfil de usuario compacto -->
          <transition name="slide-fade">
            <div v-if="!rail && user" class="user-profile mb-4 pa-3 rounded-lg d-flex align-center">
              <v-avatar color="primary" size="40" class="elevation-4">
                <span class="text-white font-weight-bold text-h6">
                  {{ user.usuario.charAt(0).toUpperCase() }}
                </span>
              </v-avatar>
              <div class="ml-3 flex-grow-1 overflow-hidden">
                <div class="text-subtitle-2 font-weight-bold text-truncate user-name">{{ user.usuario }}</div>
                <div class="text-caption text-medium-emphasis">Administrador</div>
              </div>
            </div>
          </transition>

          <v-btn
            block
            color="error"
            variant="tonal"
            @click="logout"
            rounded="lg"
            :prepend-icon="rail ? undefined : 'mdi-logout'"
            :icon="rail ? 'mdi-logout' : undefined"
            class="text-none font-weight-bold"
          >
            <span v-if="!rail">Cerrar Sesión</span>
            <v-tooltip v-if="rail" activator="parent" location="right">Cerrar Sesión</v-tooltip>
          </v-btn>
        </div>
      </template>
    </v-navigation-drawer>

    <!-- App Bar moderno y minimalista -->
    <v-app-bar elevation="0" color="background" class="px-4">
      <template v-slot:prepend>
        <v-btn icon @click.stop="rail = !rail" variant="tonal" color="primary" size="small" class="mr-3">
          <v-icon>{{ rail ? 'mdi-menu' : 'mdi-menu-open' }}</v-icon>
        </v-btn>
      </template>

      <v-app-bar-title class="text-h6 font-weight-black">
        <span class="text-primary">SGE</span> <span class="text-medium-emphasis">/ MegaServicios</span>
      </v-app-bar-title>

      <v-spacer></v-spacer>

      <!-- Usuario y Notificaciones -->
      <div class="d-flex align-center gap-4">
        <!-- Botón de Modo Claro / Modo Oscuro -->
        <v-btn
          icon
          variant="tonal"
          color="medium-emphasis"
          size="small"
          @click="toggleTheme"
          class="mr-1"
        >
          <v-icon>
            {{ theme.global.name.value === 'customDarkTheme' ? 'mdi-weather-sunny' : 'mdi-weather-night' }}
          </v-icon>
          <v-tooltip activator="parent" location="bottom">
            {{ theme.global.name.value === 'customDarkTheme' ? 'Modo Claro' : 'Modo Oscuro' }}
          </v-tooltip>
        </v-btn>

        <v-btn icon variant="tonal" color="medium-emphasis" size="small">
          <v-badge dot color="error">
            <v-icon>mdi-bell-outline</v-icon>
          </v-badge>
        </v-btn>
        
        <v-menu offset-y>
          <template v-slot:activator="{ props }">
            <v-chip
              v-if="user"
              v-bind="props"
              color="primary"
              variant="flat"
              prepend-icon="mdi-account-circle"
              class="font-weight-bold cursor-pointer"
            >
              {{ user.usuario }}
            </v-chip>
          </template>
        </v-menu>
      </div>
    </v-app-bar>

    <!-- Contenido principal -->
    <v-main>
      <v-container fluid class="pa-md-8 pa-4 h-100">
        <transition name="page" mode="out-in">
          <router-view :key="$route.fullPath" />
        </transition>
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTheme } from 'vuetify'
import authService from '@/services/auth'
import type { Empleado } from '@/types'

const theme = useTheme()

function toggleTheme() {
  const newTheme = theme.global.name.value === 'customDarkTheme' ? 'customLightTheme' : 'customDarkTheme'
  theme.global.name.value = newTheme
  localStorage.setItem('sge-theme', newTheme)
}

const router = useRouter()
const drawer = ref(true)
const rail = ref(false)
const user = ref<Empleado | null>(null)

onMounted(() => {
  user.value = authService.getUser()
})

function logout() {
  authService.logout()
  user.value = null
  router.push('/login')
}

const menuItems = [
  { title: 'Panorama', icon: 'mdi-view-dashboard', to: '/' },
  { title: 'Personal', icon: 'mdi-account-group', to: '/empleados' },
  { title: 'Lugares De Trabajo', icon: 'mdi-map-polygon', to: '/lugares' },
  { title: 'Informes', icon: 'mdi-chart-box-outline', to: '/reportes' },
]
</script>

<style scoped>
/* Rediseño Dark Premium para el Sidebar */
.drawer-premium {
  background-color: var(--v-theme-surface) !important;
  transition: background-color 0.3s ease, border-color 0.3s ease;
}

.v-theme--customDarkTheme .drawer-premium {
  border-right: 1px solid rgba(255, 255, 255, 0.05) !important;
}

.v-theme--customLightTheme .drawer-premium {
  border-right: 1px solid rgba(0, 0, 0, 0.05) !important;
}

.loguito {
  width: 100%;
  height: auto;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  filter: drop-shadow(0 4px 6px rgba(0, 0, 0, 0.3));
}

/* Menu Items Hover & Active States */
.menu-item {
  transition: all 0.2s ease-in-out;
}

.v-theme--customDarkTheme .menu-item {
  color: rgba(255, 255, 255, 0.7) !important;
}

.v-theme--customLightTheme .menu-item {
  color: rgba(15, 23, 42, 0.7) !important;
}

.menu-item:hover {
  background-color: rgba(99, 102, 241, 0.1) !important;
  color: var(--v-theme-primary) !important;
  transform: translateX(4px);
}

.menu-item-active {
  background: linear-gradient(90deg, rgba(99, 102, 241, 0.2) 0%, rgba(99, 102, 241, 0) 100%) !important;
  color: var(--v-theme-primary) !important;
  border-left: 3px solid var(--v-theme-primary);
  font-weight: 600;
}

/* User Profile Mini-Card */
.user-profile {
  transition: background-color 0.3s ease, border-color 0.3s ease;
}

.v-theme--customDarkTheme .user-profile {
  background-color: rgba(0, 0, 0, 0.2) !important;
  border: 1px solid rgba(255, 255, 255, 0.05) !important;
}

.v-theme--customLightTheme .user-profile {
  background-color: rgba(0, 0, 0, 0.04) !important;
  border: 1px solid rgba(0, 0, 0, 0.05) !important;
}

.v-theme--customDarkTheme .user-name {
  color: #F8FAFC !important;
}

.v-theme--customLightTheme .user-name {
  color: #0F172A !important;
}

/* Transiciones de Página */
.page-enter-active,
.page-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.page-enter-from {
  opacity: 0;
  transform: translateY(15px);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-15px);
}

/* Transiciones Suaves Globales */
.slide-fade-enter-active,
.slide-fade-leave-active {
  transition: all 0.3s ease;
}
.slide-fade-enter-from,
.slide-fade-leave-to {
  transform: translateX(-10px);
  opacity: 0;
}
</style>
