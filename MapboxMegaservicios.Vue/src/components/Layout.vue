<template>
  <v-app>
    <!-- Navigation Drawer con diseño moderno -->
    <v-navigation-drawer
      v-model="drawer"
      :rail="rail"
      permanent
      @click="rail = false"
      rail-width="72"
      width="280"
      :class="{ 'drawer-rail': rail }"
      class="drawer-modern"
    >
      <!-- Logo y Header -->
      <div class="drawer-header" :class="{ 'drawer-header-rail': rail }">
        <img src="@/assets/logo-megaservicios.png" alt="Mega Servicios Logo" class="loguito" />
      </div>

      <v-divider class="mx-3 mb-2 opacity-50"></v-divider>

      <!-- Menu Items con animaciones -->
      <v-list density="comfortable" nav class="px-2">
        <v-list-item
          v-for="item in menuItems"
          :key="item.title"
          :to="item.to"
          :prepend-icon="item.icon"
          :title="rail ? '' : item.title"
          class="mb-2 menu-item"
          rounded="xl"
          active-color="primary"
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
        <div class="drawer-footer pa-3">
          <v-divider class="mb-4 opacity-50"></v-divider>

          <!-- Perfil de usuario compacto -->
          <transition name="slide-fade">
            <div v-if="!rail && user" class="user-profile mb-3 pa-3">
              <div class="d-flex align-center">
                <v-avatar color="primary" size="40">
                  <span class="text-white font-weight-bold">
                    {{ user.usuario.charAt(0).toUpperCase() }}
                  </span>
                </v-avatar>
                <div class="ml-3 flex-grow-1">
                  <div class="text-body-2 font-weight-medium">{{ user.usuario }}</div>
                  <div class="text-caption text-grey">Administrador</div>
                </div>
              </div>
            </div>
          </transition>

          <v-btn
            block
            color="error"
            @click="logout"
            rounded="xl"
            :prepend-icon="rail ? undefined : 'mdi-logout'"
            :icon="rail ? 'mdi-logout' : undefined"
            variant="flat"
            class="logout-btn"
            elevation="0"
          >
            <span v-if="!rail">Cerrar Sesión</span>
            <v-tooltip v-if="rail" activator="parent" location="right"> Cerrar Sesión </v-tooltip>
          </v-btn>
        </div>
      </template>
    </v-navigation-drawer>

    <!-- App Bar moderno con gradiente -->
    <v-app-bar :elevation="0" class="app-bar-modern">
      <template v-slot:prepend>
        <v-btn icon @click.stop="rail = !rail" class="ml-2 toggle-btn" size="large" variant="text">
          <v-icon>{{ rail ? 'mdi-menu' : 'mdi-menu-open' }}</v-icon>
        </v-btn>
      </template>

      <v-app-bar-title class="app-title">
        <v-icon size="28" class="mr-2">mdi-map-check</v-icon>
        <span class="font-weight-bold">Mapbox Megaservicios</span>
      </v-app-bar-title>

      <v-spacer></v-spacer>

      <!-- Usuario con menú -->
      <v-menu offset-y>
        <template v-slot:activator="{ props }">
          <v-chip
            v-if="user"
            v-bind="props"
            color="white"
            variant="flat"
            class="user-chip"
            prepend-icon="mdi-account-circle"
          >
            <span class="font-weight-medium">{{ user.usuario }}</span>
          </v-chip>
        </template>
      </v-menu>
    </v-app-bar>

    <!-- Contenido principal con animaciones -->
    <v-main class="main-content">
      <v-container fluid class="pa-6">
        <transition name="page" mode="out-in">
          <router-view :key="$route.fullPath" />
        </transition>
      </v-container>
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import authService from '@/services/auth'
import type { Empleado } from '@/types'

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
  { title: 'Dashboard', icon: 'mdi-view-dashboard-outline', to: '/' },
  { title: 'Empleados', icon: 'mdi-account-group-outline', to: '/empleados' },
  { title: 'Lugares', icon: 'mdi-map-marker-outline', to: '/lugares' },

  { title: 'Reportes', icon: 'mdi-chart-bar', to: '/reportes' },
]
</script>

<style scoped>
/* Drawer moderno */
.drawer-modern {
  background: linear-gradient(180deg, #ffffff 0%, #f8f9fa 100%) !important;
  border-right: 1px solid rgba(0, 0, 0, 0.08) !important;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.drawer-rail {
  background: #ffffff !important;
}

.drawer-header {
  padding: 24px 20px;
  transition: all 0.3s ease;
}

.drawer-header-rail {
  padding: 20px 16px;
  display: flex;
  justify-content: center;
}

.logo-container {
  display: flex;
  align-items: center;
}

.logo-avatar {
  box-shadow: 0 4px 12px rgba(255, 107, 0, 0.2);
  transition: all 0.3s ease;
}

.logo-avatar:hover {
  transform: scale(1.05);
  box-shadow: 0 6px 16px rgba(255, 107, 0, 0.3);
}

.logo-text {
  line-height: 1.2;
}

/* Menu Items con efectos hover mejorados */
.menu-item {
  margin-bottom: 4px;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  overflow: hidden;
}

.menu-item::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  height: 100%;
  width: 0;
  background: linear-gradient(90deg, rgba(255, 107, 0, 0.1), transparent);
  transition: width 0.3s ease;
}

.menu-item:hover::before {
  width: 100%;
}

.menu-item:hover {
  transform: translateX(4px);
  background-color: rgba(255, 107, 0, 0.08) !important;
}

/* User Profile Card */
.user-profile {
  background: linear-gradient(135deg, rgba(255, 107, 0, 0.08), rgba(255, 107, 0, 0.04));
  border-radius: 16px;
  border: 1px solid rgba(255, 107, 0, 0.1);
  transition: all 0.3s ease;
}

.user-profile:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(255, 107, 0, 0.15);
}

/* Logout Button */
.logout-btn {
  text-transform: none;
  font-weight: 600;
  letter-spacing: 0.5px;
  transition: all 0.3s ease !important;
}

.logout-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(244, 67, 54, 0.3) !important;
}

/* App Bar moderno con gradiente */
.app-bar-modern {
  background: linear-gradient(135deg, #ff6b00 0%, #ff8f3d 100%) !important;
  color: white !important;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
}

.app-title {
  display: flex;
  align-items: center;
  font-size: 1.25rem;
  text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.toggle-btn {
  color: white !important;
  transition: all 0.3s ease;
}

.toggle-btn:hover {
  background-color: rgba(255, 255, 255, 0.15) !important;
  transform: rotate(180deg);
}

/* User Chip mejorado */
.user-chip {
  background-color: rgba(255, 255, 255, 0.95) !important;
  color: #ff6b00 !important;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: all 0.3s ease;
  cursor: pointer;
}

.user-chip:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  background-color: white !important;
}

/* Main Content */
.main-content {
  background: linear-gradient(135deg, #f5f7fa 0%, #e8eef3 100%);
  min-height: 100vh;
}

/* Animaciones */
.slide-fade-enter-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.slide-fade-leave-active {
  transition: all 0.2s cubic-bezier(0.4, 0, 1, 1);
}

.slide-fade-enter-from {
  transform: translateX(-10px);
  opacity: 0;
}

.slide-fade-leave-to {
  transform: translateX(-10px);
  opacity: 0;
}

/* Page transitions mejoradas */
.page-enter-active {
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.page-leave-active {
  transition: all 0.3s cubic-bezier(0.4, 0, 1, 1);
}

.page-enter-from {
  opacity: 0;
  transform: translateY(20px);
}

.page-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}

/* Scrollbar personalizado */
:deep(.v-navigation-drawer__content)::-webkit-scrollbar {
  width: 6px;
}

:deep(.v-navigation-drawer__content)::-webkit-scrollbar-track {
  background: transparent;
}

:deep(.v-navigation-drawer__content)::-webkit-scrollbar-thumb {
  background: rgba(0, 0, 0, 0.2);
  border-radius: 10px;
}

:deep(.v-navigation-drawer__content)::-webkit-scrollbar-thumb:hover {
  background: rgba(0, 0, 0, 0.3);
}
.loguito {
  width: 220px;
  height: auto;
  transition: all 0.3s ease;
}

/* Responsivo */
@media (max-width: 960px) {
  .app-title span {
    font-size: 1rem;
  }
}
</style>
