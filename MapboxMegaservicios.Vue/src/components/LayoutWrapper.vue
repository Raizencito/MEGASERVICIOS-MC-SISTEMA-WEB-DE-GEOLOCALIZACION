<template>
  <Layout v-if="showLayout">
    <router-view />
  </Layout>
  <router-view v-else />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import Layout from './Layout.vue'

const route = useRoute()

// No mostrar layout en login
const showLayout = computed(() => route.path !== '/login')
</script>
