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
    } else if (tipoReporte.value === 'individual') {
      await generarPDFIndividual()
    } else {
      await generarPDFGeneral()
    }
  } catch (e) {
    console.error(e)
    alert('Error generando reporte')
  } finally {
    cargando.value = false
  }
}

// 1. PDF General (Todos los empleados)
async function generarPDFGeneral() {
  const f = { ...filtros.value, empleadoId: undefined }
  const datosAlertas = await reportesService.obtenerAlertas(f)
  const datosTiempos = await reportesService.obtenerTiemposFuera(f)
  
  const doc = new jsPDF()
  
  doc.setFontSize(18)
  doc.setTextColor(33, 150, 243)
  doc.text('REPORTE GENERAL DE ASISTENCIA Y ALERTAS', 14, 20)
  
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta}`, 14, 28)
  doc.text(`Total Alertas: ${datosAlertas.totalAlertas || 0}`, 14, 33)
  
  // Tabla de Tiempos Fuera
  doc.setFontSize(14)
  doc.setTextColor(0)
  doc.text('Tiempo fuera de geocerca por empleado', 14, 45)
  
  const rowsTiempos = Object.entries(datosTiempos.tiemposPorEmpleado || {}).map(([emp, tiempo]: any) => {
    return [emp, tiempo]
  })
  
  autoTable(doc, {
    startY: 50,
    head: [['Empleado', 'Tiempo Fuera (HH:MM:SS)']],
    body: rowsTiempos,
    theme: 'grid',
    headStyles: { fillColor: [33, 150, 243] },
  })
  
  // Tabla de Alertas
  const currentY = (doc as any).lastAutoTable.finalY + 15
  doc.setFontSize(14)
  doc.text('Últimas Alertas', 14, currentY)
  
  const rowsAlertas = (datosAlertas.alertas || []).map((a: any) => [
    a.empleadoNombre,
    new Date(a.fechaHora).toLocaleString(),
    a.tipoAlerta,
    a.observaciones
  ])
  
  autoTable(doc, {
    startY: currentY + 5,
    head: [['Empleado', 'Fecha/Hora', 'Tipo', 'Observaciones']],
    body: rowsAlertas,
    theme: 'grid',
    headStyles: { fillColor: [244, 67, 54] },
  })

  doc.save(`Reporte-General-${filtros.value.desde}.pdf`)
}

// 2. PDF Individual
async function generarPDFIndividual() {
  if (!filtros.value.empleadoId) {
    alert("Debe seleccionar un empleado para este reporte.")
    return
  }

  const f = { ...filtros.value, departamentoId: undefined, lugarTrabajoId: undefined }
  const datosAlertas = await reportesService.obtenerAlertas(f)
  const datosTiempos = await reportesService.obtenerTiemposFuera(f)
  
  const empleadoInfo = empleados.value.find(e => e.id === filtros.value.empleadoId)
  
  const doc = new jsPDF()
  
  doc.setFontSize(18)
  doc.setTextColor(76, 175, 80) // Verde
  doc.text('REPORTE INDIVIDUAL DEL EMPLEADO', 14, 20)
  
  doc.setFontSize(12)
  doc.setTextColor(0)
  doc.text(`Empleado: ${empleadoInfo?.nombreCompleto || 'Desconocido'}`, 14, 30)
  
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta}`, 14, 36)
  
  const tiempoFuera = Object.values(datosTiempos.tiemposPorEmpleado || {})[0] || '00:00:00'
  doc.text(`Total tiempo fuera de geocerca: ${tiempoFuera}`, 14, 42)
  doc.text(`Total alertas registradas: ${datosAlertas.totalAlertas || 0}`, 14, 48)

  const rowsAlertas = (datosAlertas.alertas || []).map((a: any) => [
    new Date(a.fechaHora).toLocaleString(),
    a.tipoAlerta,
    a.observaciones
  ])

  autoTable(doc, {
    startY: 55,
    head: [['Fecha/Hora', 'Tipo de Alerta', 'Observaciones']],
    body: rowsAlertas,
    theme: 'striped',
    headStyles: { fillColor: [76, 175, 80] },
  })

  doc.save(`Reporte-Individual-${filtros.value.desde}.pdf`)
}

// 3. PDF CLIENTE (Descuentos)
async function generarPDFDescuentos() {
  // Para descuentos, evaluaremos el "tiempo fuera" como falta
  const datosTiempos = await reportesService.obtenerTiemposFuera({ ...filtros.value })

  const doc = new jsPDF()
  
  // Header
  doc.setFontSize(18)
  doc.setTextColor(192, 57, 43) // Rojo Oscuro
  doc.text('REPORTE DE DESCUENTOS POR TIEMPO FUERA', 14, 20)
  
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(`Período: ${filtros.value.desde} al ${filtros.value.hasta}`, 14, 28)
  doc.text(`Generado: ${new Date().toLocaleString()}`, 14, 33)

  // LOGICA DE CALCULO
  // Por cada hora fuera de la geocerca, se descuenta 15 Bs (por ejemplo)
  const rows = Object.entries(datosTiempos.tiemposPorEmpleado || {}).map(([emp, tiempo]: any) => {
      // Parsear "HH:MM:SS" a horas decimales
      const partes = tiempo.split(':')
      const horasDebe = parseInt(partes[0]) + (parseInt(partes[1]) / 60) + (parseInt(partes[2]) / 3600)
      
      const penalizacion = horasDebe * 15 // 15 Bs por hora fuera
      
      // Solo mostrar si hay tiempo fuera
      if (horasDebe <= 0) return null

      return [
         emp,
         tiempo,
         `-${penalizacion.toFixed(2)} Bs`
      ]
  }).filter(Boolean)

  if (rows.length === 0) {
      doc.text("¡Felicidades! Todos cumplieron su tiempo en geocerca sin faltas.", 14, 50)
  } else {
      autoTable(doc, {
          startY: 40,
          head: [['Empleado', 'Tiempo Total Fuera', 'Descuento Estimado']],
          body: rows as any[],
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
   try {
     // Departamentos
     const dObj = await reportesService.obtenerDepartamentos()
     if (dObj && dObj.length > 0) {
        departamentos.value = dObj
     }
   } catch (error) {
     // Fallback estático si falla el endpoint de admin
     departamentos.value = [
         {id: 1, nombre: 'La Paz'}, {id: 2, nombre: 'Cochabamba'}, {id: 3, nombre: 'Santa Cruz'},
         {id: 4, nombre: 'Oruro'}, {id: 5, nombre: 'Potosí'}, {id: 6, nombre: 'Chuquisaca'},
         {id: 7, nombre: 'Tarija'}, {id: 8, nombre: 'Beni'}, {id: 9, nombre: 'Pando'}
     ]
   }

   try {
     // Empleados
     const eObj = await reportesService.obtenerEmpleadosActivos()
     empleados.value = eObj.map((e: any) => ({
         id: e.id,
         nombreCompleto: `${e.nombres} ${e.paterno} - ${e.ci}`
     }))
   } catch (error) {
     console.error("Error cargando empleados activos", error)
   }
}

async function cargarLugares() {
   if(!filtros.value.departamentoId) {
       lugares.value = []
       return
   }
   try {
     const lugaresData = await reportesService.obtenerLugaresPorDepartamento(filtros.value.departamentoId)
     lugares.value = lugaresData
   } catch (error) {
     console.error("Error cargando lugares", error)
     lugares.value = []
   }
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
