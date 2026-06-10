# Cronograma de Proyecto SGE — MEGASERVICIOS MC
## Planificación Ágil en 3 Sprints y Distribución de Roles

Este documento detalla la reestructuración del cronograma original del proyecto **SGE (Sistema de Geolocalización de Empleados)** de 24 semanas a un formato de **16 semanas en total**, comprimiendo la fase de desarrollo en **3 Sprints quincenales (6 semanas de desarrollo activo)**. Se definen los roles y se asignan tareas específicas a los 4 integrantes del equipo de acuerdo con el stack tecnológico existente del proyecto.

---

## 1. Matriz de Roles y Responsabilidades

Basándonos en la arquitectura actual del sistema (Backend en **.NET 8 API / PostGIS**, Frontend en **Vue 3 / Vuetify**, y App Móvil en **Flutter**), se definen los siguientes perfiles para los miembros del equipo:

| Integrante | Rol Principal | Enfoque de Responsabilidad |
| :--- | :--- | :--- |
| **Josue Ramos Zeballos** | Backend Developer & DBA | Administración de PostgreSQL + PostGIS, desarrollo de la Minimal API en .NET 8, seguridad con JWT y lógica de consultas espaciales (NetTopologySuite). |
| **Alan Aguilar Morales** | Frontend Web Developer | Desarrollo de la SPA en Vue 3 con Vuetify 3, manejo de estado global con Pinia, integración de Mapbox GL JS para la visualización web y generación de reportes en PDF. |
| **Gerald Aquise Illanes** | Mobile App Developer | Desarrollo de la aplicación móvil nativa en Flutter (Android/iOS), configuración de servicios en background (`flutter_background_service`), geolocalización activa y flujos de asistencia. |
| **Ethan Nicolas Cardenas Luna** | QA Engineer & DevOps | Aseguramiento de la calidad, diseño y ejecución de pruebas de integración, automatización de pruebas unitarias (Backend, Web, Móvil), pruebas de carga y configuración de CI/CD. |

---

## 2. Resumen Temporal del Proyecto (16 Semanas)

Se reduce el período de ejecución a **4 meses (16 semanas)** manteniendo las fases predictivas de inicio y cierre, pero optimizando los tiempos y consolidando el desarrollo en 3 Sprints de 2 semanas cada uno:

```
[Fase 1: Inicio] ──► [Fase 2: Diseño/Plan.] ──► [Fase 3: Desarrollo (3 Sprints)] ──► [Fase 4: Cierre/Pruebas/Despliegue]
  (Sem 1 - 2)           (Sem 3 - 5)                  (Sem 6 - 11)                           (Sem 12 - 16)
```

| Fase / Hito | Inicio | Fin | Duración | Descripción Clave |
| :--- | :--- | :--- | :--- | :--- |
| **Fase 1: Inicio y Constitución** | Semana 1 | Semana 2 | 2 semanas | Acta de constitución, alineación de requerimientos, diseño del entorno de trabajo inicial. |
| **Fase 2: Planificación y Diseño** | Semana 3 | Semana 5 | 3 semanas | Modelado físico de base de datos espaciales, arquitectura detallada y mockups aprobados. |
| **Sprint 1: Cimientos y Tracking Base** | Semana 6 | Semana 7 | 2 semanas | BD PostgreSQL/PostGIS activa, API de autenticación y empleados lista. App móvil capturando y enviando GPS en background. |
| **Sprint 2: Mapas, Geocercas y Alertas** | Semana 8 | Semana 9 | 2 semanas | Panel web interactivo con Mapbox, motor espacial de alertas en backend por entrada/salida de geocercas, marcación de asistencia. |
| **Sprint 3: Reportes, Dashboard y Pulido** | Semana 10 | Semana 11 | 2 semanas | Dashboard con gráficos estadísticos, reportes PDF exportables, soporte offline en móvil, y corrección de bugs. |
| **Fase 4: Integración, Pruebas y Cierre** | Semana 12 | Semana 16 | 5 semanas | Pruebas de estrés de extremo a extremo, capacitación de usuarios finales, preparación de tiendas de apps (PlayStore/AppStore) y despliegue final. |

---

## 3. Planificación Detallada de los Sprints (Fase de Ejecución)

### Sprint 1: Cimientos del Sistema y Tracking Base (Semanas 6 - 7)
* **Objetivo del Sprint:** Tener la infraestructura de base de datos activa, autenticación JWT funcionando extremo a extremo, y la aplicación móvil transmitiendo coordenadas geográficas en background.

#### Tareas por Integrante:
* **Josue Ramos Zeballos (Backend):**
  * Configurar e inicializar la base de datos PostgreSQL 16 con la extensión PostGIS.
  * Diseñar el esquema de base de datos (tablas: `Usuarios`, `Empleados`, `Departamentos`, `HistorialCoordenadas`).
  * Implementar el controlador de autenticación (`AuthController`) y el custom `JwtAuthHandler`.
  * Desarrollar el CRUD de empleados (`EmpleadosController`) con soporte para subida de fotos de perfil.
* **Alan Aguilar Morales (Web):**
  * Crear el esqueleto de la aplicación Vue 3 + Vuetify + Pinia y configurar el enrutador.
  * Diseñar e implementar la pantalla de Login y configurar `authStore` para el manejo de sesiones y tokens.
  * Desarrollar la vista de administración de empleados (DataTable con opciones de búsqueda, filtrado, creación, edición y desactivación).
* **Gerald Aquise Illanes (Móvil):**
  * Estructurar el proyecto Flutter e implementar la pantalla de Login conectada al backend.
  * Configurar el almacenamiento seguro del token JWT (`flutter_secure_storage`).
  * Configurar los permisos nativos de GPS en Android e iOS (incluyendo `ACCESS_BACKGROUND_LOCATION`).
  * Integrar `flutter_background_service` para registrar coordenadas cada 30 segundos y enviarlas a `/api/ubicaciones/guardar-ubicacion`.
* **Ethan Nicolas Cardenas Luna (QA & DevOps):**
  * Configurar el repositorio central, definir la estrategia de ramas (GitFlow) y montar linters/estándares de código.
  * Diseñar los planes de prueba y matrices de pruebas unitarias para autenticación y CRUD de empleados.
  * Escribir pruebas de integración para la API REST utilizando xUnit.
  * Configurar un pipeline básico en GitHub Actions para verificar que el código compila y pasa tests unitarios en cada pull request.

* **Hito de Fin de Sprint (H-01):** API REST base publicada localmente, login funcional en web y móvil, y registro de coordenadas GPS en background guardándose en la base de datos.

---

### Sprint 2: Mapas, Geocercas y Alertas (Semanas 8 - 9)
* **Objetivo del Sprint:** Implementar el control geográfico completo. Dibujar cercas digitales en la interfaz web, detectar automáticamente cuándo un empleado cruza los límites mediante lógica espacial en backend y enviar las respectivas alertas a la interfaz.

#### Tareas por Integrante:
* **Josue Ramos Zeballos (Backend):**
  * Diseñar la tabla `Cercas` (geocercas con tipos de datos `Polygon`) y la tabla `Alertas`.
  * Implementar endpoints CRUD para geocercas (`/api/ubicaciones/cercas`).
  * Desarrollar el servicio espacial que corre sobre cada coordenada insertada utilizando `NetTopologySuite` (`ST_Within`) para verificar si el empleado ingresó o salió de su geocerca asignada.
  * Desarrollar endpoints para consultar alertas recientes (`/api/ubicaciones/alertas`) y últimas ubicaciones conocidas (`/api/ubicaciones/ultimas-ubicaciones`).
* **Alan Aguilar Morales (Web):**
  * Integrar Mapbox GL JS en la aplicación Vue.
  * Desarrollar la vista de Lugares de Trabajo (`LugaresView.vue`) permitiendo a los administradores dibujar, editar y guardar geocercas poligonales usando `Mapbox Draw`.
  * Implementar la vista de monitoreo en tiempo real (`UbicacionesView.vue`) con marcadores de empleados que se actualizan automáticamente en el mapa.
  * Diseñar el menú dinámico de notificaciones de alertas en la barra superior.
* **Gerald Aquise Illanes (Móvil):**
  * Integrar un mapa interactivo en la app móvil (ej. `flutter_map` con OpenStreetMap o Mapbox).
  * Crear la pantalla donde el empleado pueda visualizar la geocerca de su área de trabajo asignada.
  * Implementar los botones "Marcar Entrada" y "Marcar Salida" del registro de asistencia diario, consumiendo `/api/asistencia/marcar-entrada` y `/api/asistencia/marcar-salida` (bloqueando la acción si el GPS está fuera del polígono asignado).
* **Ethan Nicolas Cardenas Luna (QA & DevOps):**
  * Realizar pruebas funcionales del motor de geocercas (simulando coordenadas dentro, fuera y en los bordes de los límites).
  * Escribir tests de integración para verificar que la inserción de coordenadas que rompen geocercas genera alertas en base de datos.
  * Implementar pruebas automatizadas de UI en la web (usando Playwright o Cypress) para simular el dibujo de geocercas.
  * Configurar una base de datos de pruebas aislada para ejecuciones en el CI/CD.

* **Hito de Fin de Sprint (H-02):** Visualización de empleados en tiempo real sobre el mapa web, capacidad de dibujar y almacenar geocercas, y generación automática de alertas por violaciones de geocercas en el backend.

---

### Sprint 3: Reportes, Dashboard y Pulido (Semanas 10 - 11)
* **Objetivo del Sprint:** Cerrar el ciclo de funcionalidades principales añadiendo analítica de datos a través de gráficos interactivos, la generación de reportes exportables para auditoría y la optimización del rendimiento general de la aplicación.

#### Tareas por Integrante:
* **Josue Ramos Zeballos (Backend):**
  * Crear el endpoint optimizado para reportes (`/api/reportes`) que aplique filtros avanzados por rango de fechas, empleado, área o departamento.
  * Implementar índices espaciales (`GIST` sobre la columna `Ubicacion` e `Id` en base de datos) para agilizar búsquedas de trayectorias e historiales de ubicaciones.
  * Diseñar los endpoints agregados para las estadísticas de Dashboard (total de empleados, empleados dentro/fuera de geocerca en el día, cantidad de alertas generadas).
* **Alan Aguilar Morales (Web):**
  * Desarrollar el Dashboard principal (`DashboardView.vue`) e integrar `Chart.js` para mostrar resúmenes analíticos (ej. gráficos de barra de alertas semanales, gráficos circulares de empleados activos por departamento).
  * Implementar la vista de generación y descarga de reportes utilizando `jsPDF` para estructurar la información exportada de manera estética y legible.
  * Solucionar los bugs de interfaz detectados durante las fases de prueba previas (ej. fugas de memoria en el polling de Mapbox).
* **Gerald Aquise Illanes (Móvil):**
  * Diseñar el dashboard personalizado del empleado que muestre su resumen de jornada diario (hora de entrada registrada, horas transcurridas, alertas propias generadas).
  * Implementar persistencia offline en Flutter (SQLite o Hive) para almacenar de manera temporal las coordenadas registradas en zonas sin conectividad y sincronizarlas cuando la red retorne.
  * Optimizar el consumo de batería del servicio en background limitando la frecuencia de actualización GPS si el dispositivo se detecta estacionario.
* **Ethan Nicolas Cardenas Luna (QA & DevOps):**
  * Escribir y ejecutar scripts de pruebas de carga en JMeter para simular el consumo concurrente del API por parte de 50+ empleados transmitiendo GPS al mismo tiempo.
  * Realizar auditorías de seguridad sobre los endpoints expuestos (verificación de SQL Injection, cross-site scripting y fugas de datos de contraseñas).
  * Automatizar la generación de compilados del frontend Vue y empaquetar la aplicación Flutter en formato `.apk` firmado para distribución de pruebas.

* **Hito de Fin de Sprint (H-03):** Dashboard con métricas visuales completo, generación funcional de reportes PDF/Excel, aplicación móvil capaz de operar en áreas de baja conectividad y reporte de pruebas de estrés satisfactorio.

---

## 4. Fase de Cierre, Integración y Despliegue (Semanas 12 - 16)

Una vez completados los 3 Sprints de desarrollo, el equipo transiciona a la fase final para garantizar la estabilidad de la plataforma y su puesta en producción exitosa:

* **Semana 12 (Pruebas de Sistema e Integración de Campo):**
  * Todo el equipo participa en pruebas de campo reales (usuarios móviles moviéndose físicamente por áreas de prueba para validar el comportamiento del mapa web, velocidad del envío GPS, y la precisión en la activación de geocercas).
  * **Ethan Nicolas** recopila los logs de errores y coordina las soluciones.
* **Semana 13 (Corrección de Bugs Críticos e Infraestructura de Producción):**
  * **Josue** migra la base de datos a un entorno en la nube (ej. AWS RDS PostgreSQL o Azure Database for PostgreSQL) y configura políticas de copias de seguridad automáticas.
  * **Alan** realiza la compilación de producción del frontend web y la despliega en un hosting estático optimizado (ej. Vercel, Netlify o AWS S3).
  * **Ethan** implementa SSL en los servidores y asegura que el API corra solo sobre HTTPS con políticas seguras de encabezados HTTP.
* **Semana 14 (Capacitación y Documentación Técnica):**
  * Creación del manual de usuario para el panel de administración web (desarrollado por **Alan** e **Ethan**).
  * Creación de la guía de instalación y uso de la aplicación móvil para el personal en campo (desarrollado por **Gerald**).
  * Sesiones de capacitación grabadas para los administradores del sistema.
* **Semana 15 - 16 (Despliegue y Cierre de Proyecto):**
  * Firma y publicación de la versión final de la aplicación móvil en Google Play Console y Apple Developer Portal (realizado por **Gerald** y **Ethan**).
  * Firma de acta de entrega del software y transferencia formal del repositorio al cliente.
  * Reunión de retrospectiva final del proyecto (Lessons Learned).

---

## 5. Dinámica de Trabajo y Ceremonias Ágiles

Para asegurar la correcta ejecución del plan, se mantendrán las siguientes ceremonias Scrum durante las 6 semanas de desarrollo (Semanas 6 a 11):

1. **Sprint Planning (Cada dos lunes por la mañana - Duración: 2 horas):**
   * El equipo se reúne para revisar el backlog del sprint correspondiente, definir el Sprint Goal y comprometerse con las tareas detalladas anteriormente.
2. **Daily Standup (De lunes a viernes - Duración: 15 minutos):**
   * Cada miembro responde tres preguntas: ¿Qué hice ayer?, ¿Qué haré hoy?, ¿Tengo algún impedimento?
   * *Josue* y *Alan* coordinan la sincronización de contratos de endpoints de API para que el desarrollo de vistas web no se detenga.
   * *Gerald* y *Josue* coordinan los payloads de coordenadas del móvil hacia el backend.
3. **Sprint Review & Demo (Cada dos viernes por la tarde - Duración: 1 hora):**
   * El equipo demuestra el software funcional desarrollado durante el sprint ante los stakeholders interesados.
4. **Sprint Retrospective (Cada dos viernes después de la Demo - Duración: 45 minutos):**
   * Espacio para reflexionar sobre lo que funcionó bien, lo que salió mal y definir compromisos de mejora de procesos para el siguiente sprint.
