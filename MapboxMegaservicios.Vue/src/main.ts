import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'

// Vuetify
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { aliases, mdi } from 'vuetify/iconsets/mdi'
import '@mdi/font/css/materialdesignicons.css'

// Importar Layout component
import Layout from '@/components/Layout.vue'

const customDarkTheme = {
  dark: true,
  colors: {
    background: '#0F172A',
    surface: '#1E293B',
    primary: '#6366F1',
    'primary-darken-1': '#4F46E5',
    secondary: '#10B981',
    'secondary-darken-1': '#059669',
    error: '#EF4444',
    info: '#3B82F6',
    success: '#10B981',
    warning: '#F59E0B',
  },
}

const customLightTheme = {
  dark: false,
  colors: {
    background: '#F8FAFC',
    surface: '#FFFFFF',
    primary: '#4F46E5',
    'primary-darken-1': '#4338CA',
    secondary: '#10B981',
    'secondary-darken-1': '#059669',
    error: '#EF4444',
    info: '#3B82F6',
    success: '#10B981',
    warning: '#F59E0B',
  },
}

const savedTheme = localStorage.getItem('sge-theme') || 'customDarkTheme'

const vuetify = createVuetify({
  components,
  directives,
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi },
  },
  theme: {
    defaultTheme: savedTheme,
    themes: {
      customDarkTheme,
      customLightTheme,
    },
  },
})

const app = createApp(App)

// Registrar Layout globalmente
app.component('Layout', Layout)

app.use(createPinia())
app.use(router)
app.use(vuetify)

app.mount('#app')
