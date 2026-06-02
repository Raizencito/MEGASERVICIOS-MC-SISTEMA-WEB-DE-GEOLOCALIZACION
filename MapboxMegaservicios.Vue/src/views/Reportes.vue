<template>
  <div class="h-100 d-flex flex-column gap-6">
    <!-- Header -->
    <v-row align="center" no-gutters class="mb-2">
      <v-col>
        <h1 class="text-h4 font-weight-bold" style="color: var(--v-theme-primary); letter-spacing: -1px;">
          Centro de Informes
        </h1>
        <p class="text-subtitle-1 text-medium-emphasis mt-1">
          Generación y exportación de métricas de asistencia y desempeño
        </p>
      </v-col>
    </v-row>

    <!-- Selector de Tipo de Reporte (Tarjetas Grandes) -->
    <v-row class="mb-4">
      <v-col cols="12" sm="6" md="3" v-for="opcion in opcionesReportes" :key="opcion.value">
        <v-card 
          @click="tipoReporte = opcion.value"
          :class="{ 'card-active': tipoReporte === opcion.value }"
          class="h-100 bg-surface rounded-xl cursor-pointer hover-card transition-all"
          elevation="0"
          style="border: 2px solid transparent;"
        >
          <v-card-text class="pa-6 text-center d-flex flex-column align-center justify-center h-100">
            <v-avatar :color="tipoReporte === opcion.value ? 'primary' : 'rgba(255,255,255,0.05)'" size="64" class="mb-4 transition-all">
              <v-icon size="32" :color="tipoReporte === opcion.value ? 'white' : 'primary'">{{ opcion.icon }}</v-icon>
            </v-avatar>
            <div class="text-h6 font-weight-bold mb-2">{{ opcion.title }}</div>
            <div class="text-caption text-medium-emphasis">{{ opcion.desc }}</div>
          </v-card-text>
        </v-card>
      </v-col>
    </v-row>

    <!-- Panel de Filtros -->
    <v-card class="bg-surface rounded-xl glass-panel" elevation="12">
      <v-card-title class="pa-6 pb-2 text-h6 font-weight-bold d-flex align-center">
        <v-icon start color="primary" class="mr-2">mdi-filter-variant</v-icon>
        Parámetros del Reporte
      </v-card-title>
      
      <v-card-text class="pa-6 pt-2">
        <v-row>
          <!-- Período de Tiempo -->
          <v-col cols="12" md="6">
             <div class="text-subtitle-2 font-weight-bold mb-3 text-medium-emphasis text-uppercase">Período de Evaluación</div>
             <v-row>
               <v-col cols="6">
                 <v-text-field 
                   v-model="filtros.desde" 
                   type="date" 
                   label="Fecha Inicio"
                   variant="solo-filled"
                   flat
                   bg-color="rgba(255,255,255,0.05)"
                 ></v-text-field>
               </v-col>
               <v-col cols="6">
                 <v-text-field 
                   v-model="filtros.hasta" 
                   type="date" 
                   label="Fecha Fin"
                   variant="solo-filled"
                   flat
                   bg-color="rgba(255,255,255,0.05)"
                 ></v-text-field>
               </v-col>
             </v-row>
          </v-col>

          <v-divider vertical class="mx-4 hidden-sm-and-down border-opacity-25"></v-divider>

          <!-- Filtros Contextuales -->
          <v-col cols="12" md="5">
            <div class="text-subtitle-2 font-weight-bold mb-3 text-medium-emphasis text-uppercase">Filtros Específicos</div>
            
            <template v-if="mostrarFiltroLugar">
              <v-autocomplete 
                v-model="filtros.departamentoId" 
                :items="departamentos" 
                item-title="nombre" 
                item-value="id" 
                label="Departamento"
                variant="solo-filled"
                flat
                bg-color="rgba(255,255,255,0.05)"
                clearable
                class="mb-3"
              ></v-autocomplete>
              
              <v-autocomplete 
                v-model="filtros.lugarTrabajoId" 
                :items="lugares" 
                item-title="nombre" 
                item-value="id" 
                label="Lugar de Trabajo"
                :disabled="!filtros.departamentoId"
                variant="solo-filled"
                flat
                bg-color="rgba(255,255,255,0.05)"
                clearable
                class="mb-3"
              ></v-autocomplete>
            </template>

            <template v-if="mostrarFiltroEmpleado">
              <v-autocomplete 
                v-model="filtros.empleadoId" 
                :items="empleados" 
                item-title="nombreCompleto" 
                item-value="id" 
                label="Seleccionar Empleado (Opcional)"
                variant="solo-filled"
                flat
                bg-color="rgba(255,255,255,0.05)"
                clearable
                class="mb-3"
              ></v-autocomplete>
            </template>

            <!-- RF-05 Parámetro -->
            <template v-if="tipoReporte === 'improductividad'">
               <v-text-field
                  v-model.number="toleranciaMinutosDiarios"
                  type="number"
                  label="Tolerancia Diaria (Minutos)"
                  min="0"
                  max="180"
                  suffix="min"
                  variant="solo-filled"
                  flat
                  bg-color="rgba(255,255,255,0.05)"
                  color="warning"
              ></v-text-field>
            </template>
          </v-col>
        </v-row>
      </v-card-text>
      
      <v-divider class="border-opacity-25"></v-divider>
      
      <v-card-actions class="pa-6">
        <v-spacer></v-spacer>
        <v-btn 
          color="primary" 
          size="x-large" 
          variant="flat"
          rounded="lg"
          class="text-none font-weight-bold px-8 bg-gradient-primary elevation-8"
          @click="generarReporte" 
          :loading="cargando"
          prepend-icon="mdi-printer"
        >
          Generar Documento PDF
        </v-btn>
      </v-card-actions>
    </v-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import reportesService from '@/services/reportes.service'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'

const filtros = ref({
  departamentoId: null as number | null,
  lugarTrabajoId: null as number | null,
  empleadoId: null as number | null,
  desde: new Date().toISOString().split('T')[0],
  hasta: new Date().toISOString().split('T')[0]
})

const tipoReporte = ref('general')
const toleranciaMinutosDiarios = ref(30)
const cargando = ref(false)
const departamentos = ref<any[]>([])
const lugares = ref<any[]>([])
const empleados = ref<any[]>([])

const opcionesReportes = [
  { value: 'general', title: 'Global', desc: 'Resumen general de asistencia y alertas.', icon: 'mdi-web' },
  { value: 'individual', title: 'Individual', desc: 'Detalle específico por empleado.', icon: 'mdi-account-details' },
  { value: 'descuentos', title: 'Descuentos', desc: 'Cálculo de penalizaciones salariales.', icon: 'mdi-cash-remove' },
  { value: 'improductividad', title: 'RF-05', desc: 'Consolidado Inasistencias/Improductividad.', icon: 'mdi-file-document-alert' }
]

const mostrarFiltroEmpleado = computed(() => ['individual', 'descuentos', 'improductividad'].includes(tipoReporte.value))
const mostrarFiltroLugar = computed(() => ['general', 'descuentos', 'improductividad'].includes(tipoReporte.value))

async function generarReporte() {
  cargando.value = true
  try {
    if (tipoReporte.value === 'descuentos') await generarPDFDescuentos()
    else if (tipoReporte.value === 'individual') await generarPDFIndividual()
    else if (tipoReporte.value === 'improductividad') await generarPDFImproductividad()
    else await generarPDFGeneral()
  } catch (e) {
    console.error(e)
    alert('Error generando reporte')
  } finally {
    cargando.value = false
  }
}

async function generarPDFGeneral() {
  const f = { ...filtros.value, empleadoId: undefined }
  const datosAlertas = await reportesService.obtenerAlertas(f)
  const datosTiempos = await reportesService.obtenerTiemposFuera(f)
  
  const doc = new jsPDF()
  doc.setFontSize(18)
  doc.setTextColor(99, 102, 241) // primary
  doc.text('REPORTE GENERAL DE ASISTENCIA', 14, 20)
  
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta} | Alertas: ${datosAlertas.totalAlertas || 0}`, 14, 28)
  
  const rowsTiempos = Object.entries(datosTiempos.tiemposPorEmpleado || {}).map(([emp, tiempo]: any) => [emp, tiempo])
  
  autoTable(doc, {
    startY: 35,
    head: [['Empleado', 'Tiempo Fuera (HH:MM:SS)']],
    body: rowsTiempos,
    theme: 'grid',
    headStyles: { fillColor: [99, 102, 241] },
  })
  
  const currentY = (doc as any).lastAutoTable.finalY + 15
  doc.setFontSize(14)
  doc.setTextColor(0)
  doc.text('Últimas Alertas', 14, currentY)
  
  const rowsAlertas = (datosAlertas.alertas || []).map((a: any) => [
    a.empleadoNombre, new Date(a.fechaHora).toLocaleString(), a.tipoAlerta, a.observaciones
  ])
  
  autoTable(doc, {
    startY: currentY + 5,
    head: [['Empleado', 'Fecha/Hora', 'Tipo', 'Observaciones']],
    body: rowsAlertas,
    theme: 'grid',
    headStyles: { fillColor: [239, 68, 68] }, // error color
  })

  doc.save(`Reporte-General-${filtros.value.desde}.pdf`)
}

async function generarPDFIndividual() {
  if (!filtros.value.empleadoId) return alert("Seleccione un empleado.")
  const f = { ...filtros.value, departamentoId: undefined, lugarTrabajoId: undefined }
  const datosAlertas = await reportesService.obtenerAlertas(f)
  const datosTiempos = await reportesService.obtenerTiemposFuera(f)
  const empleadoInfo = empleados.value.find(e => e.id === filtros.value.empleadoId)
  
  const doc = new jsPDF()
  doc.setFontSize(18)
  doc.setTextColor(16, 185, 129) // success color
  doc.text('REPORTE INDIVIDUAL', 14, 20)
  
  doc.setFontSize(12)
  doc.setTextColor(0)
  doc.text(`Empleado: ${empleadoInfo?.nombreCompleto || 'Desconocido'}`, 14, 30)
  
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta}`, 14, 36)
  
  const tiempoFuera = Object.values(datosTiempos.tiemposPorEmpleado || {})[0] || '00:00:00'
  doc.text(`Total tiempo fuera: ${tiempoFuera} | Alertas: ${datosAlertas.totalAlertas || 0}`, 14, 42)

  const rowsAlertas = (datosAlertas.alertas || []).map((a: any) => [
    new Date(a.fechaHora).toLocaleString(), a.tipoAlerta, a.observaciones
  ])

  autoTable(doc, {
    startY: 50,
    head: [['Fecha/Hora', 'Tipo', 'Observaciones']],
    body: rowsAlertas,
    theme: 'grid',
    headStyles: { fillColor: [16, 185, 129] },
  })

  doc.save(`Reporte-Individual-${filtros.value.desde}.pdf`)
}

async function generarPDFDescuentos() {
  const datosTiempos = await reportesService.obtenerTiemposFuera({ ...filtros.value })
  const doc = new jsPDF()
  
  doc.setFontSize(18)
  doc.setTextColor(245, 158, 11) // warning
  doc.text('CÁLCULO DE DESCUENTOS', 14, 20)
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta}`, 14, 28)

  const rows = Object.entries(datosTiempos.tiemposPorEmpleado || {}).map(([emp, tiempo]: any) => {
      const partes = tiempo.split(':')
      const horas = parseInt(partes[0]) + (parseInt(partes[1]) / 60) + (parseInt(partes[2]) / 3600)
      if (horas <= 0) return null
      return [emp, tiempo, `-${(horas * 15).toFixed(2)} Bs`]
  }).filter(Boolean)

  if (rows.length === 0) {
      doc.text("No se registraron tiempos fuera de geocerca.", 14, 40)
  } else {
      autoTable(doc, {
          startY: 35,
          head: [['Empleado', 'Tiempo Fuera', 'Descuento Est.']],
          body: rows as any[],
          theme: 'grid',
          headStyles: { fillColor: [245, 158, 11] },
      })
  }
  doc.save(`Descuentos-${filtros.value.desde}.pdf`)
}

async function generarPDFImproductividad() {
  const datos = await reportesService.obtenerReporteImproductividad({
    ...filtros.value, toleranciaMinutosDiarios: toleranciaMinutosDiarios.value
  })

  const doc = new jsPDF()
  doc.setFontSize(18)
  doc.setTextColor(99, 102, 241)
  doc.text('INASISTENCIAS Y TIEMPO IMPRODUCTIVO (RF-05)', 14, 20)
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta} | Tolerancia: ${toleranciaMinutosDiarios.value}m`, 14, 28)

  const rows = datos.map((item: any) => [
    item.empleadoNombre, item.diasInasistencia, item.tiempoTotalFueraRuta, item.tiempoToleranciaAplicado, item.tiempoNetoPenalizable
  ])

  autoTable(doc, {
    startY: 35,
    head: [['Empleado', 'Días Faltas', 'Fuera de Ruta', 'Tolerancia', 'Neto Penalizable']],
    body: rows,
    theme: 'grid',
    headStyles: { fillColor: [99, 102, 241] },
  })

  doc.save(`RF-05-${filtros.value.desde}.pdf`)
}

async function cargarCatalogos() {
   try {
     const dObj = await reportesService.obtenerDepartamentos()
     if (dObj?.length) departamentos.value = dObj
   } catch (e) {
     departamentos.value = [{id: 1, nombre: 'La Paz'}, {id: 2, nombre: 'Cochabamba'}, {id: 3, nombre: 'Santa Cruz'}]
   }
   try {
     const eObj = await reportesService.obtenerEmpleadosActivos()
     empleados.value = eObj.map((e: any) => ({
         id: e.id, nombreCompleto: `${e.nombres} ${e.paterno} - ${e.ci}`
     }))
   } catch (e) {}
}

async function cargarLugares() {
   if(!filtros.value.departamentoId) { lugares.value = []; return }
   try { lugares.value = await reportesService.obtenerLugaresPorDepartamento(filtros.value.departamentoId) } 
   catch (e) { lugares.value = [] }
}

watch(() => filtros.value.departamentoId, cargarLugares)

onMounted(cargarCatalogos)
</script>

<style scoped>
.gap-6 { gap: 24px; }
.hover-card { transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1); }
.hover-card:hover { transform: translateY(-4px); box-shadow: 0 12px 24px rgba(0,0,0,0.2) !important; }
.card-active { border-color: var(--v-theme-primary) !important; background: rgba(99, 102, 241, 0.1) !important; }
.transition-all { transition: all 0.3s ease; }
</style>
