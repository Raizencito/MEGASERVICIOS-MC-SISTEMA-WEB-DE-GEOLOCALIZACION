<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import reportesService from '@/services/reportes.service'
import api from '@/services/api'
import jsPDF from 'jspdf'
import autoTable from 'jspdf-autotable'

// ================= ESTADO =================
const filtros = ref({
  departamentoId: null as number | null,
  lugarTrabajoId: null as number | null,
  empleadoId: null as number | null,
  desde: new Date().toISOString().split('T')[0],
  hasta: new Date().toISOString().split('T')[0]
})

const tipoReporte = ref('general') // general | individual | descuentos
const cargando = ref(false)

// Datos Auxiliares
const departamentos = ref<any[]>([])
const lugares = ref<any[]>([])
const empleados = ref<any[]>([])

// ================= COMPUTED =================
const mostrarFiltroEmpleado = computed(() => tipoReporte.value === 'individual' || tipoReporte.value === 'descuentos')
const mostrarFiltroLugar = computed(() => tipoReporte.value === 'general' || tipoReporte.value === 'descuentos')

// ================= MÉTODOS =================

async function generarReporte() {
  cargando.value = true
  try {
    if (tipoReporte.value === 'descuentos') {
      await generarPDFDescuentos()
    } else {
      await generarPDFServidor()
    }
  } catch (e) {
    console.error(e)
    alert('Error generando reporte')
  } finally {
    cargando.value = false
  }
}

// 1. PDF SERVIDOR (General / Individual - Asistencia Standard)
async function generarPDFServidor() {
  // Ajustar filtros según tipo
  const filtroEnvio = { ...filtros.value }
  
  if (tipoReporte.value === 'general') {
     filtroEnvio.empleadoId = undefined // Ignorar empleado si es general
  }

  const blob = await reportesService.generarReportePDF({
      ...filtroEnvio,
      tipo: 'asistencia' // Backend siempre usa este por ahora
  })
  
  descargarBlob(blob, `Reporte-${tipoReporte.value}-${filtros.value.desde}`)
}

// 2. PDF CLIENTE (Descuentos - Lógica Custom)
async function generarPDFDescuentos() {
  // Obtener datos crudos
  const datos = await reportesService.obtenerDatosReporte({
      ...filtros.value
  })

  const doc = new jsPDF()
  
  // Header
  doc.setFontSize(18)
  doc.setTextColor(192, 57, 43) // Rojo Oscuro
  doc.text('REPORTE DE DESCUENTOS Y FALTAS', 14, 20)
  
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta}`, 14, 28)
  doc.text(`Generado: ${new Date().toLocaleString()}`, 14, 33)

  // LOGICA DE CALCULO
  // Filtramos jornadas con menos de 8 horas o faltas
  const jornadasDescuento = datos.jornadas.filter((j: any) => {
     return (j.totalHoras || 0) < 8
  })

  const rows = jornadasDescuento.map((j: any) => {
      const horasTrabajadas = j.totalHoras || 0
      const horasDebe = 8 - horasTrabajadas
      const penalizacion = horasDebe * 10 // Ejemplo: 10 Bs por hora (o unidad arbitraria)
      
      return [
         `${j.empleado?.nombres} ${j.empleado?.paterno}`,
         new Date(j.fecha).toLocaleDateString(),
         horasTrabajadas.toFixed(2) + ' hrs',
         horasDebe.toFixed(2) + ' hrs',
         `-${penalizacion.toFixed(2)} Bs`
      ]
  })

  if (rows.length === 0) {
      doc.text("¡Felicidades! No hay descuentos aplicables en este período.", 14, 50)
  } else {
      autoTable(doc, {
          startY: 40,
          head: [['Empleado', 'Fecha', 'Trabajado', 'Debe', 'Desc. Est.']],
          body: rows,
          theme: 'grid',
          headStyles: { fillColor: [192, 57, 43] },
      })
  }

  doc.save(`Descuentos-${filtros.value.desde}.pdf`)
}

function descargarBlob(blob: Blob, nombre: string) {
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `${nombre}.pdf`
    link.click()
    window.URL.revokeObjectURL(url)
}

// Carga de Datos
async function cargarCatalogos() {
   // Deptos
   const dObj = await api.get('/departamentos') // Asumiendo endpoint existe o simulamos
   if (dObj.data) departamentos.value = dObj.data
   else {
      // Fallback estatico si no existe endpoint
      departamentos.value = [
          {id: 1, nombre: 'La Paz'}, {id: 2, nombre: 'Cochabamba'}, {id: 3, nombre: 'Santa Cruz'}
      ]
   }

   // Empleados
   const eObj = await api.get('/empleados/activos')
   empleados.value = eObj.data.map((e: any) => ({
       id: e.id,
       nombreCompleto: `${e.nombres} ${e.paterno} - ${e.ci}`
   }))
}

async function cargarLugares() {
   if(!filtros.value.departamentoId) {
       lugares.value = []
       return
   }
   const lObj = await api.get(`/lugares/departamento/${filtros.value.departamentoId}`)
   lugares.value = lObj.data
}

// WATCHERS
watch(() => filtros.value.departamentoId, cargarLugares)

onMounted(() => {
    cargarCatalogos()
})
</script>

<template>
  <v-container>
    <v-row>
        <v-col cols="12">
            <h1 class="text-h4 mb-4 text-primary">Centro de Reportes</h1>
            <p class="text-subtitle-1 text-grey">Generación de informes de asistencia y descuentos</p>
        </v-col>
    </v-row>

    <v-card class="mt-4 pa-4" variant="outlined">
        <v-row>
            <!-- TIPO DE REPORTE -->
            <v-col cols="12" md="12">
                <v-label class="mb-2 font-weight-bold">Tipo de Reporte</v-label>
                <v-btn-toggle v-model="tipoReporte" mandatory color="primary" class="d-flex flex-wrap w-100" divided>
                    <v-btn value="general" class="flex-grow-1">
                        <v-icon start>mdi-file-table-box</v-icon> General (Deptos/Lugares)
                    </v-btn>
                     <v-btn value="individual" class="flex-grow-1">
                        <v-icon start>mdi-account</v-icon> Individual (Por Empleado)
                    </v-btn>
                     <v-btn value="descuentos" class="flex-grow-1">
                        <v-icon start>mdi-cash-remove</v-icon> Cálculo de Descuentos
                    </v-btn>
                </v-btn-toggle>
            </v-col>
        </v-row>

        <v-divider class="my-4"></v-divider>

        <v-row>
            <!-- FILTROS COMUNES -->
            <v-col cols="12" md="6">
                <v-text-field v-model="filtros.desde" type="date" label="Desde"></v-text-field>
            </v-col>
            <v-col cols="12" md="6">
                <v-text-field v-model="filtros.hasta" type="date" label="Hasta"></v-text-field>
            </v-col>

            <!-- FILTROS CONTEXTUALES -->
            <v-col cols="12" md="4" v-if="mostrarFiltroLugar">
                 <v-autocomplete 
                    v-model="filtros.departamentoId" 
                    :items="departamentos" 
                    item-title="nombre" 
                    item-value="id" 
                    label="Departamento"
                    clearable
                ></v-autocomplete>
            </v-col>

             <v-col cols="12" md="4" v-if="mostrarFiltroLugar">
                 <v-autocomplete 
                    v-model="filtros.lugarTrabajoId" 
                    :items="lugares" 
                    item-title="nombre" 
                    item-value="id" 
                    label="Lugar de Trabajo"
                    :disabled="!filtros.departamentoId"
                    clearable
                ></v-autocomplete>
            </v-col>

             <v-col cols="12" md="4" v-if="mostrarFiltroEmpleado">
                 <v-autocomplete 
                    v-model="filtros.empleadoId" 
                    :items="empleados" 
                    item-title="nombreCompleto" 
                    item-value="id" 
                    label="Seleccionar Empleado"
                    clearable
                ></v-autocomplete>
            </v-col>
        </v-row>

        <v-row>
            <v-col cols="12" class="d-flex justify-end">
                <v-btn 
                    color="primary" 
                    size="large" 
                    @click="generarReporte" 
                    :loading="cargando"
                    prepend-icon="mdi-printer"
                >
                    Generar Reporte PDF
                </v-btn>
            </v-col>
        </v-row>
    </v-card>
  </v-container>
</template>
