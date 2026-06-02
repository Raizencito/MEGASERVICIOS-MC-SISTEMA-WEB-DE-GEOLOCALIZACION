# Auditoría de Código — MEGASERVICIOS MC

> Sistema web de geolocalización y monitoreo de personal.
> **Fecha:** 01/06/2026
> **Autor:** Auditoría automatizada

---

## 1. Resumen del Proyecto

| Atributo | Valor |
|---|---|
| **Propósito** | Monitoreo en tiempo real de ubicación de empleados, geocercas, reportes, dashboard |
| **Arquitectura** | 3 capas desacopladas (API REST, SPA Web, App Móvil) |
| **Backend** | .NET 8 Minimal API + Entity Framework Core + PostGIS |
| **Frontend Web** | Vue 3 + Vite 7 + Vuetify 3 + Pinia + Mapbox GL JS |
| **App Móvil** | Flutter 3.x + Provider + http + shared_preferences |
| **Base de datos** | PostgreSQL 16 + PostGIS + NetTopologySuite |
| **Autenticación** | JWT (Bearer) + custom handler |
| **Repositorio** | `https://github.com/Raizencito/MEGASERVICIOS-MC-SISTEMA-WEB-DE-GEOLOCALIZACION` |

---

## 2. Vista General de la Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│                        FLUTTER APP (Movil)                       │
│  Provider (State) ─→ http (API calls) ─→ shared_preferences     │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP (JWT Bearer)
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                      VUE 3 FRONTEND (Web)                        │
│  Pinia Stores ─→ Axios / API Services ─→ Vue Router ─→ Vuetify  │
│  Mapbox GL JS ─→ Chart.js ─→ jsPDF                              │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP (JWT Bearer)
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                   .NET 8 API (Backend)                            │
│  Controllers ─→ Services ─→ EF Core ─→ DbContext                 │
│  JWT Auth Handler ─→ PostGIS Spatial Queries                     │
└───────────────────────────┬─────────────────────────────────────┘
                            │ Npgsql + NetTopologySuite
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    PostgreSQL 16 + PostGIS                        │
│  Tablas: Usuarios, Empleados, Departamentos, Alertas, Cercas,   │
│          HistorialCoordenadas (Point), Cercas (Polygon)          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Backend (.NET 8 Minimal API)

### 3.1 Estructura de Archivos

```
MapboxMegaservicios.API/
├── Program.cs                          # Startup, DI, CORS, JWT, Migrations
├── appsettings.json / Development.json # Conexión DB, JWT Secret, credenciales Mapbox
├── MapboxMegaservicios.API.http        # Pruebas HTTP
│
├── Controllers/
│   ├── AuthController.cs               # POST /api/auth/login
│   ├── EmpleadosController.cs          # CRUD + toggleActivo + upload image
│   ├── UbicacionesController.cs        # Ubicación, alertas, cercas
│   ├── ReportesController.cs           # Reportes con filtros
│   └── UserController.cs               # GET /api/user/info
│
├── Auth/
│   └── JwtAuthHandler.cs               # Custom AuthenticationHandler
│
├── Data/
│   └── ApplicationDbContext.cs         # EF Core DbContext + OnModelCreating
│
├── Models/
│   ├── Usuario.cs
│   ├── Empleado.cs
│   ├── Departamento.cs
│   ├── HistorialCoordenada.cs
│   ├── Alerta.cs
│   └── Cerca.cs
│
├── DTOs/
│   ├── LoginDTO.cs
│   ├── EmpleadoDTO.cs
│   ├── UbicacionDTO.cs
│   └── ReporteDTO.cs
│
└── Migrations/
    └── *.cs                            # Migraciones EF Core
```

### 3.2 Modelos de Datos

#### Usuario
| Campo | Tipo | Notas |
|---|---|---|
| Id | int (PK) | Auto-increment |
| Login | string(50) | Único, no nulo |
| PasswordHash | string | BCrypt hash |
| Rol | string(20) | "ADMIN" o "USER" |
| Activo | bool | Default true |

#### Empleado
| Campo | Tipo | Notas |
|---|---|---|
| Id | int (PK) | Auto-increment |
| Nombres | string(100) | Requerido |
| Apellidos | string(100) | Requerido |
| Telefono | string(20) | Regex: `^(|[67]\d{7})$` |
| Direccion | string(200) | Opcional |
| ImagePath | string(500) | Opcional |
| DepartamentoId | int (FK) | → Departamento.Id |
| Activo | bool | Default true |

#### Departamento
| Campo | Tipo | Notas |
|---|---|---|
| Id | int (PK) | |
| Nombre | string(100) | Requerido |

#### HistorialCoordenada
| Campo | Tipo | Notas |
|---|---|---|
| Id | long (PK) | Auto-increment |
| EmpleadoId | int (FK) | → Empleado.Id |
| Ubicacion | Point (PostGIS) | SRID 4326 (WGS84) |
| FechaHora | DateTime | Default SQL `NOW()` |
| NotificacionEnviada | bool | Default false |
| Velocidad | decimal(5,2) | Opcional |
| Bateria | int | Opcional |

#### Cerca
| Campo | Tipo | Notas |
|---|---|---|
| Id | int (PK) | Auto-increment |
| Nombre | string(100) | Requerido |
| Area | Polygon (PostGIS) | SRID 4326 |
| Color | string(20) | Opcional |
| Descripcion | text | Opcional |
| Activo | bool | Default true |

#### Alerta
| Campo | Tipo | Notas |
|---|---|---|
| Id | int (PK) | Auto-increment |
| EmpleadoId | int (FK) | → Empleado.Id |
| CercaId | int (FK) | → Cerca.Id |
| Tipo | string(20) | "ENTRADA" o "SALIDA" |
| Mensaje | text | |
| FechaHora | DateTime | Default `NOW()` |
| Leida | bool | Default false |

### 3.3 Endpoints de la API

| Método | Ruta | Descripción | Auth |
|---|---|---|---|
| POST | `/api/auth/login` | Login, devuelve JWT | No |
| GET | `/api/user/info` | Info del usuario actual | JWT |
| GET | `/api/empleados` | Listar todos los empleados | JWT |
| GET | `/api/empleados/{id}` | Obtener empleado por ID | JWT |
| POST | `/api/empleados` | Crear empleado | JWT |
| PUT | `/api/empleados/{id}` | Actualizar empleado | JWT |
| DELETE | `/api/empleados/{id}` | Eliminar empleado | JWT |
| PATCH | `/api/empleados/{id}/toggle-activo` | Activar/desactivar | JWT |
| POST | `/api/empleados/{id}/upload-image` | Subir foto | JWT |
| GET | `/api/ubicaciones/alertas` | Últimas 50 alertas | JWT |
| GET | `/api/ubicaciones/historial/{empleadoId}` | Historial ubicaciones | JWT |
| GET | `/api/ubicaciones/ultimas-ubicaciones` | Última ubicación c/empleado | JWT |
| GET | `/api/ubicaciones/cercas` | Listar cercas activas | JWT |
| POST | `/api/ubicaciones/cercas` | Crear cerca | JWT |
| PUT | `/api/ubicaciones/cercas/{id}` | Actualizar cerca | JWT |
| DELETE | `/api/ubicaciones/cercas/{id}` | Eliminar cerca | JWT |
| POST | `/api/ubicaciones/guardar-ubicacion` | Guardar ubicación actual | JWT |
| POST | `/api/ubicaciones/verificar-cercas` | Verificar cruce de cercas | JWT |
| GET | `/api/departamentos` | Listar departamentos | JWT |
| GET | `/api/reportes` | Generar reportes (xlsx?) | JWT |

### 3.4 Autenticación JWT

**Flujo:**
1. `POST /api/auth/login` recibe `{ login, password }`
2. Busca usuario por `Login`, verifica `PasswordHash` con BCrypt
3. Genera JWT con claims: `Id`, `Login`, `Rol`, `Activo`
4. JWT firmado con clave simétrica (`appsettings JwtSecret`)
5. `JwtAuthHandler` valida token en cada request, establece `HttpContext.User`
6. Política por defecto: `[Authorize]` en todos los controllers (excepto login)

**Claims incluidos en el token JWT:**
- `sub` → Usuario.Id
- `unique_name` → Usuario.Login
- `role` → Usuario.Rol
- `activo` → Usuario.Activo

### 3.5 Lógica Espacial (PostGIS + NetTopologySuite)

- **Guardar ubicación** (`POST /api/ubicaciones/guardar-ubicacion`):
  - Inserta `HistorialCoordenada` con `Ubicacion` como `Point(lon, lat)` SRID 4326
  - Inserta en cola de verificación de cercas

- **Verificar cercas** (`POST /api/ubicaciones/verificar-cercas`):
  - Para cada ubicación no verificada, consulta:
    ```sql
    SELECT c.Id, c.Nombre, c.Area
    FROM Cercas c
    WHERE c.Activo = true
      AND ST_Within(h.Ubicacion, c.Area) = false
    ```
  - Detecta transiciones ENTRADA/SALIDA comparando estado anterior
  - Genera `Alerta` si hay cruce

- **Listar cercas** (`GET /api/ubicaciones/cercas`):
  - Devuelve `CercaDTO` con `Area` serializado como GeoJSON (array de coordenadas)

- **Historial** (`GET /api/ubicaciones/historial/{empleadoId}`):
  - Devuelve lista de puntos ordenados por fecha descendente

---

## 4. Frontend (Vue 3 + Vuetify 3)

### 4.1 Estructura de Archivos

```
MapboxMegaservicios.Vue/src/
├── main.ts                       # Entry point, createApp, plugins
├── App.vue                       # Layout raíz, v-app, router-view
├── router/
│   └── index.ts                  # Vue Router + navigation guards
│
├── types/
│   ├── empleado.ts               # Interfaces de Empleado
│   ├── ubicacion.ts              # Interfaces de Ubicacion, Alerta, Cerca
│   └── reporte.ts                # Interfaces de Reporte
│
├── services/
│   ├── authService.ts            # login, getUserInfo, token management
│   ├── apiService.ts             # Axios instance + JWT interceptor
│   ├── empleadosService.ts       # CRUD empleados
│   ├── ubicacionesService.ts     # Ubicaciones, alertas, cercas
│   └── reportesService.ts        # Reportes
│
├── stores/
│   ├── authStore.ts              # Pinia store de autenticación
│   ├── empleadosStore.ts         # Pinia store de empleados
│   └── ubicacionesStore.ts       # Pinia store de ubicaciones
│
├── views/
│   ├── LoginView.vue             # Formulario de login
│   ├── DashboardView.vue         # Dashboard con gráficos
│   ├── EmpleadosView.vue         # CRUD empleados + DataTable
│   ├── LugaresView.vue           # Mapa + geocercas
│   ├── UbicacionesView.vue       # Mapa con marcadores en tiempo real
│   ├── ReportesView.vue          # Reportes PDF
│   └── WebMapView.vue            # Visor de mapa general
│
├── components/
│   ├── Layout.vue                # AppBar + NavigationDrawer + Footer
│   ├── LoginLayout.vue           # Layout para login
│   ├── AlertNotifications.vue    # Menú de notificaciones de alertas
│   └── charts/
│       ├── GPSActivosChart.vue   # Chart.js: empleados con GPS activo
│       ├── EmpleadosAreaChart.vue# Chart.js: empleados por área
│       └── AlertasTimeline.vue   # Chart.js: línea de tiempo de alertas
│
└── style.css                     # Estilos globales
```

### 4.2 Enrutamiento

| Ruta | Vista | Layout | Auth |
|---|---|---|---|
| `/login` | LoginView | LoginLayout | No |
| `/` | DashboardView | Layout | Sí |
| `/empleados` | EmpleadosView | Layout | Sí |
| `/lugares` | LugaresView | Layout | Sí |
| `/ubicaciones` | UbicacionesView | Layout | Sí |
| `/reportes` | ReportesView | Layout | Sí |
| `/webmap` | WebMapView | Layout | Sí |

**Navigation Guard:** `beforeEach` verifica token en localStorage; si no hay token y la ruta no es `/login`, redirige a `/login`.

### 4.3 Stores (Pinia)

#### authStore
- **State:** `user`, `token`
- **Actions:** `login(credentials)`, `logout()`, `checkAuth()`
- **Getters:** `isAuthenticated`, `isAdmin`, `currentUser`

#### empleadosStore
- **State:** `empleados[]`, `loading`, `error`
- **Actions:** `fetchAll()`, `fetchById(id)`, `create(data)`, `update(id, data)`, `remove(id)`, `toggleActivo(id)`
- Se usa en `EmpleadosView.vue`

#### ubicacionesStore
- **State:** `ubicaciones[]`, `alertas[]`, `cercas[]`, `loading`
- **Actions:** `fetchUltimasUbicaciones()`, `fetchAlertas()`, `fetchCercas()`, `guardarUbicacion(data)`
- Se usa en `UbicacionesView.vue` y `DashboardView.vue`

### 4.4 Flujo de Datos por Vista

#### Login
```
LoginView → authStore.login() → authService.login() → POST /api/auth/login
    → JWT → localStorage → router.push('/')
```

#### Dashboard
```
DashboardView → onMounted → 
    ubicacionesStore.fetchUltimasUbicaciones() → GET /api/ubicaciones/ultimas-ubicaciones
    empleadosStore.fetchAll() → GET /api/empleados
    ubicacionesStore.fetchAlertas() → GET /api/ubicaciones/alertas
    → GPSActivosChart, EmpleadosAreaChart, AlertasTimeline
```

#### Empleados (CRUD)
```
EmpleadosView → empleadosStore.fetchAll() → GET /api/empleados
    → v-data-table con filtros
    → Dialog: create/update → POST/PUT /api/empleados
    → Eliminar: DELETE /api/empleados/{id}
    → Toggle: PATCH /api/empleados/{id}/toggle-activo
    → Upload image: POST /api/empleados/{id}/upload-image (FormData)
```

#### Lugares (Geocercas en Mapa)
```
LugaresView → mapboxgl.Map + draw controls
    → ubicacionesStore.fetchCercas() → GET /api/ubicaciones/cercas
    → Renderizar polígonos en mapa
    → CRUD: POST/PUT/DELETE /api/ubicaciones/cercas
```

#### Ubicaciones (Tiempo Real)
```
UbicacionesView → mapboxgl.Map + markers
    → ubicacionesStore.fetchUltimasUbicaciones() → GET /api/ubicaciones/ultimas-ubicaciones
    → Polling cada N segundos (setInterval)
    → Renderizar markers con popups
```

#### Reportes
```
ReportesView → formulario filtros (fecha, empleado, área)
    → GET /api/reportes?filtros=...
    → jsPDF: generar PDF descargable con tabla de datos
```

### 4.5 Manejo de Errores (Frontend)

- `apiService.ts` usa interceptor de Axios para:
  - Añadir `Authorization: Bearer <token>` a todos los requests
  - Capturar 401 → redirigir a `/login`
- Las vistas capturan errores en bloques `try/catch` y muestran con `alert()` o notificación Vuetify

---

## 5. App Móvil (Flutter)

### 5.1 Estructura de Archivos

```
MapboxMegaservicios.App/lib/
├── main.dart                           # Entry point, runApp, providers setup
├── services/
│   ├── auth_service.dart               # login, getUserInfo, token storage
│   ├── api_service.dart                # HTTP client con JWT header
│   ├── empleados_service.dart          # CRUD empleados
│   └── ubicaciones_service.dart        # Ubicaciones, alertas, cercas
│
├── models/
│   ├── user.dart                       # User model fromJson
│   ├── empleado.dart                   # Empleado model fromJson
│   ├── ubicacion.dart                  # Ubicacion model fromJson
│   ├── cerca.dart                      # Cerca model fromJson
│   └── alerta.dart                     # Alerta model fromJson
│
├── providers/
│   ├── auth_provider.dart              # ChangeNotifier: auth state
│   ├── empleados_provider.dart         # ChangeNotifier: empleados state
│   └── ubicaciones_provider.dart       # ChangeNotifier: ubicaciones state
│
├── screens/
│   ├── login_page.dart                 # Formulario de login
│   ├── home_page.dart                  # BottomNavigationBar + PageView
│   ├── dashboard_page.dart             # Dashboard (cards, stats)
│   ├── empleados_page.dart             # Lista de empleados
│   ├── empleado_detail_page.dart       # Detalle/formulario empleado
│   ├── lugares_page.dart               # Mapa + geocercas
│   ├── reportes_page.dart              # Reportes
│   ├── ubicaciones_page.dart           # Mapa + ubicaciones
│   └── profile_page.dart               # Perfil de usuario
│
└── widgets/                            # Widgets reutilizables
    ├── empleado_card.dart              # Card de empleado
    └── location_tracker.dart           # Widget de tracking GPS
```

### 5.2 Providers (ChangeNotifier + Provider)

#### AuthProvider
- **State:** `User? user`, `String? token`, `bool isLoading`, `String? error`
- **Methods:** `login(login, password)`, `logout()`, `checkAuth()`
- **Persistencia:** Token guardado en `shared_preferences`

#### EmpleadosProvider
- **State:** `List<Empleado> empleados`, `bool isLoading`, `String? error`
- **Methods:** `fetchAll()`, `create(data)`, `update(id, data)`, `delete(id)`, `toggleActivo(id)`

#### UbicacionesProvider
- **State:** `List<Ubicacion> ubicaciones`, `List<Alerta> alertas`, `List<Cerca> cercas`
- **Methods:** `fetchUltimasUbicaciones()`, `fetchAlertas()`, `fetchCercas()`, `guardarUbicacion(data)`

### 5.3 Pantallas

| Pantalla | Ruta | Descripción |
|---|---|---|
| `login_page` | `/login` | Login con usuario/contraseña |
| `home_page` | `/home` | Navegación principal con BottomNavigationBar |
| `dashboard_page` | `/home/dashboard` | Cards con resumen de datos |
| `empleados_page` | `/home/empleados` | Lista de empleados |
| `empleado_detail_page` | `/home/empleados/detail` | Detalle/formulario empleado |
| `lugares_page` | `/home/lugares` | Mapa con geocercas |
| `ubicaciones_page` | `/home/ubicaciones` | Mapa con ubicaciones |
| `reportes_page` | `/home/reportes` | Reportes |
| `profile_page` | `/home/profile` | Perfil del usuario |

### 5.4 Navegación (BottomNavigationBar)

```
HomePage
├── Botón 1: Dashboard   → DashboardPage
├── Botón 2: Empleados   → EmpleadosPage (lista) → EmpleadoDetailPage (push)
├── Botón 3: Lugares     → LugaresPage
├── Botón 4: Ubicaciones → UbicacionesPage
└── Botón 5: Perfil      → ProfilePage
```

---

## 6. Flujo de Datos Completo

### 6.1 Autenticación

```
                    Flutter App                         Vue Web
                        │                                  │
                        ▼                                  ▼
               login_page.tsx                      LoginView.vue
                        │                                  │
                        ▼                                  ▼
              AuthProvider.login()                 authStore.login()
                        │                                  │
                        ▼                                  ▼
              auth_service.login()                authService.login()
                        │                                  │
                        └──────────┬───────────────────────┘
                                   │ POST /api/auth/login
                                   ▼
                          AuthController.Login()
                                   │
                                   ▼
                          Valida credenciales
                                   │
                                   ▼
                          Genera JWT (BCrypt verify)
                                   │
                                   ▼
                     ┌─────────────┴─────────────┐
                     ▼                            ▼
              shared_preferences            localStorage
              (token persist)               (token persist)
```

### 6.2 CRUD Empleados

```
   Flutter App / Vue Web
           │
           ▼
   GET /api/empleados ───────→ EmpleadosController.ObtenerTodos()
                                       │
                                       ▼
                               ApplicationDbContext.Empleados
                               .Include(e => e.Departamento)
                               .ToListAsync()
                                       │
                                       ▼
                               Mapper → EmpleadoDTO (sin PasswordHash)
                                       │
                                       ▼
                               JSON ←─────── Response
           │
           ▼
   Empleado[] ← mapeo a modelo local
           │
           ▼
   DataTable (Vue) / ListView (Flutter)
```

### 6.3 Tracking de Ubicaciones en Tiempo Real

```
   Flutter App (GPS nativo)
           │
           ▼
   location_tracker.dart ← location plugin
           │
           ▼
   Timer.periodic(30s) → guardarUbicacion(lat, lng, velocidad, bateria)
           │
           ▼
   POST /api/ubicaciones/guardar-ubicacion
           │
           ▼
   UbicacionesController.GuardarUbicacion()
           │
           ├──→ Inserta HistorialCoordenada (Point en PostGIS)
           │
           └──→ Enqueue verificación de cercas
                    │
                    ▼
              POST /api/ubicaciones/verificar-cercas (background)
                    │
                    ├──→ ST_Within(ubicacion, cerca.Area)
                    │
                    └──→ Si cambia estado → INSERT Alerta

   Vue Web (Dashboard + UbicacionesView)
           │
           ▼
   Polling cada N segundos → GET /api/ubicaciones/ultimas-ubicaciones
           │
           ▼
   Mapbox markers actualizados
```

### 6.4 Geocercas (Cercas)

```
   Vue Web (LugaresView) / Flutter App (LugaresPage)
           │
           ▼
   Mapbox Draw / Mapa → Polígono dibujado
           │
           ▼
   POST /api/ubicaciones/cercas  { nombre, area: [[lng,lat],...], color }
           │
           ▼
   UbicacionesController.CrearCerca()
           │
           ▼
   Convierte GeoJSON array → Polygon (NetTopologySuite)
           │
           ▼
   INSERT INTO Cercas (Nombre, Area, Color, Descripcion, Activo)

   Lectura:
   GET /api/ubicaciones/cercas
       → SELECT Id, Nombre, ST_AsGeoJSON(Area), Color, Descripcion, Activo
       → Convierte a CercaDTO con coordenadas [[lng,lat],...]
```

### 6.5 Reportes

```
   Vue Web (ReportesView)
           │
           ▼
   Filtros: fecha inicio, fecha fin, empleadoId, departamentoId
           │
           ▼
   GET /api/reportes?desde=&hasta=&empleadoId=&departamentoId=
           │
           ▼
   ReportesController.GetReportes()
           │
           ▼
   Query a HistorialCoordenadas con filtros
           │
           ▼
   jsPDF: genera PDF con tabla, logo, título
           │
           ▼
   window.open(pdfURL) → descarga
```

---

## 7. Estados de Autenticación y Autorización

### 7.1 Roles
- **ADMIN:** Acceso completo a todas las rutas y operaciones
- **USER:** Acceso limitado (no especificado en detalle, pero el handler permite ambos roles)

### 7.2 Flujo de Validación de Token

```
Request entrante
       │
       ▼
JwtAuthHandler.AuthenticateAsync()
       │
       ├── Token ausente → AuthenticateResult.NoResult() → 401
       │
       ├── Token inválido/expirado → AuthenticateResult.Fail() → 401
       │
       └── Token válido → Extrae claims (Id, Login, Rol, Activo)
                │
                ▼
           Crea ClaimsPrincipal
                │
                ▼
           HttpContext.User = principal
                │
                ▼
           [Authorize] → Controller action ejecutada
```

### 7.3 Seguridad Actual

| Aspecto | Estado |
|---|---|
| Contraseñas hasheadas con BCrypt | ✅ |
| JWT con expiración | ✅ (configurable) |
| CORS configurado | ✅ (permite localhost:5173) |
| HTTPS en desarrollo | ✅ (launchSettings) |
| SQL Injection | ✅ (EF Core parameterized queries) |
| Validación de entrada | ⚠️ Parcial (solo server-side, algunos campos sin validación) |
| Rate limiting | ❌ No implementado |
| Refresh tokens | ❌ No implementado |
| Row-level authorization | ❌ Cualquier usuario autenticado puede ver/editar cualquier recurso |

---

## 8. Bugs Detectados y Correcciones Aplicadas

### Bug 1 — Botón de Notificaciones Sin Funcionalidad
- **Archivo:** `Layout.vue`
- **Problema:** El botón de notificaciones en el AppBar tenía un `@click` que navegaba a `/alertas` inexistente
- **Solución:** Reemplazado con `v-menu` que lista alertas obtenidas de `GET /api/ubicaciones/alertas`, con indicador de no leídas

### Bug 2 — Navegación del Menú Lateral se Rompe en `/lugares`
- **Archivo:** `Lugares.vue`
- **Problema:** `v-list-item` del directorio no tenía `.stop` en `@click`, propagando el evento al mapa y causando conflicto con Mapbox GL
- **Solución:** Añadido `.stop` al `@click` para detener propagación

### Bug 3 — Eliminar Cerca Fallaba Silenciosamente
- **Archivo:** `Lugares.vue`
- **Problema:** Mensaje de error genérico `alert('Error eliminando lugar')` sin mostrar la causa real
- **Solución:** Cambiado a `alert(error.response?.data?.message)` para ver el error del servidor

### Bug 4 — Error al Guardar Empleado (Validación de Teléfono)
- **Archivos:** `Empleados.vue`, `EmpleadoDTO.cs`
- **Problema:** DTO requería teléfono obligatorio con formato `[67]\d{7}`; en creación se enviaba vacío causando error de validación
- **Solución:**
  - Backend: Regex cambiado a `^(|[67]\d{7})$` para permitir vacío
  - Frontend: Añadida validación client-side que requiere 8 dígitos empezando con 6 o 7
  - Display de errores: Mejorado para mostrar errores campo por campo

### Bug 5 — `toggleActivo()` sin Mensaje de Error
- **Archivo:** `EmpleadosView.vue`
- **Problema:** Capturaba error pero no mostraba mensaje
- **Solución:** `alert(error.response?.data?.message || error.message)`

### Bug 6 — `DepartamentoId` Faltante en Algunas Consultas
- **Archivo:** `EmpleadosController.cs`
- **Problema:** `ObtenerTodos()` no incluía `DepartamentoId` en la proyección
- **Solución:** Añadido `DepartamentoId` al DTO

---

## 9. Observaciones y Recomendaciones

### 9.1 Seguridad
1. **JWT Secret en appsettings:** La clave secreta JWT está en `appsettings.json` (texto plano). Se recomienda usar variables de entorno o Azure Key Vault / AWS Secrets Manager.
2. **Sin refresh tokens:** El JWT no tiene mecanismo de renovación. Al expirar, el usuario debe volver a hacer login.
3. **Contraseñas en texto plano potencial:** Verificar que ninguna ruta devuelva `PasswordHash` en los DTOs de empleados. Actualmente los DTOs parecen no incluirlo, pero conviene auditar.
4. **Validación de entrada limitada:** Solo el DTO de empleado tiene `[RegularExpression]`. Los demás endpoints confían en EF Core.

### 9.2 Backend
1. **Error handling inconsistente:** Algunos controllers usan `try/catch` con `statusCode`, otros no.
2. **No hay logging estructurado:** No se observa Serilog u otro logger. Solo `Console.WriteLine`.
3. **Migrations en producción:** Ejecuta `app.EnsureMigration()` en startup. En producción podría causar problemas.
4. **Falta paginación:** `GET /api/empleados` devuelve todos los registros sin paginación.
5. **DTOs vs Models:** Buena separación, pero hay campos que podrían filtrarse (ej: `PasswordHash` en alguna query mal escrita).

### 9.3 Frontend
1. **Uso de `alert()` para errores:** Debería reemplazarse por notificaciones Vuetify (`v-snackbar` o `v-alert`).
2. **Sin TypeScript estricto:** `any` se usa en varios lugares. Migrar a tipos fuertes.
3. **Sin pruebas automatizadas:** No hay archivos `.spec.ts` ni `__tests__`.
4. **Polling sin cleanup:** El polling de ubicaciones puede causar memory leaks si no se limpia con `onUnmounted()`.
5. **Mapbox token hardcodeado:** El token de Mapbox debería estar en variables de entorno (`.env`), no en el código.

### 9.4 App Móvil (Flutter)
1. **Provider vs Riverpod:** Provider (ChangeNotifier) es funcional pero menos escalable. Considerar Riverpod o BLoC.
2. **Sin manejo de permisos GPS:** No se verifica `Permission.location` antes de activar tracking.
3. **Sin background location:** La app solo envía ubicación cuando está en primer plano.
4. **Sin testing:** No hay archivos `_test.dart`.
5. **Mismo token hardcodeado de Mapbox que el frontend web** (si usa Mapbox).

### 9.5 Arquitectura General
1. **Duplicación de lógica de negocio:** Tanto el frontend como la app móvil tienen su propia lógica de validación y mapeo. Un API Gateway podría centralizar.
2. **Sin caché:** Cada request va directamente a PostgreSQL. Redis o caché en memoria para endpoints de solo lectura mejoraría performance.
3. **Sin WebSockets / SignalR:** El polling HTTP para ubicaciones en tiempo real es ineficiente. SignalR (ASP.NET Core) o WebSockets reducirían latencia y carga.
4. **Documentación de API ausente:** No se observa Swagger/OpenAPI.

---

## 10. Resumen de Archivos Clave

### Backend
| Archivo | Líneas | Propósito |
|---|---|---|
| `Program.cs` | ~80 | Configuración, DI, CORS, JWT, migraciones |
| `AuthController.cs` | ~40 | Login endpoint |
| `EmpleadosController.cs` | ~160 | CRUD empleados |
| `UbicacionesController.cs` | ~180 | Ubicaciones, alertas, cercas |
| `ReportesController.cs` | ~60 | Reportes endpoint |
| `JwtAuthHandler.cs` | ~80 | Custom JWT auth handler |
| `ApplicationDbContext.cs` | ~100 | EF Core context + config |
| `EmpleadoDTO.cs` | ~25 | DTO con validación |

### Frontend
| Archivo | Líneas | Propósito |
|---|---|---|
| `Layout.vue` | ~150 | AppBar + NavigationDrawer + Footer |
| `DashboardView.vue` | ~200 | Dashboard con charts |
| `EmpleadosView.vue` | ~350 | CRUD completo con DataTable + Dialog |
| `LugaresView.vue` | ~300 | Mapa Mapbox + gestión de geocercas |
| `UbicacionesView.vue` | ~250 | Mapa con marcadores en tiempo real |
| `ReportesView.vue` | ~150 | Reportes PDF con jsPDF |
| `authStore.ts` | ~60 | Pinia store de autenticación |
| `apiService.ts` | ~40 | Axios instance + interceptor |

### App Móvil
| Archivo | Líneas | Propósito |
|---|---|---|
| `main.dart` | ~30 | Entry point, providers |
| `auth_service.dart` | ~80 | Login + token management |
| `home_page.dart` | ~80 | Navegación BottomNavigationBar |
| `empleados_page.dart` | ~150 | Lista de empleados |
| `lugares_page.dart` | ~200 | Mapa + geocercas |

---

## 11. Conclusión

El sistema MEGASERVICIOS MC es una aplicación de geolocalización funcional de 3 capas con arquitectura moderna. La separación en backend .NET, frontend Vue y app Flutter es correcta y permite desarrollo independiente. El uso de PostGIS para operaciones espaciales es apropiado y bien implementado.

**Fortalezas:**
- Arquitectura desacoplada y bien estructurada
- Uso correcto de PostGIS para consultas espaciales
- Autenticación JWT implementada correctamente
- Separación de concerns (DTOs, Models, Controllers)

**Debilidades principales:**
- Falta de pruebas automatizadas en las 3 capas
- Sin documentación de API (Swagger)
- Polling HTTP en lugar de WebSockets para datos en tiempo real
- Manejo de errores inconsistente
- Validación de entrada limitada

**Prioridades sugeridas:**
1. Agregar Swagger/OpenAPI para documentar endpoints
2. Implementar SignalR para reemplazar polling de ubicaciones
3. Agregar pruebas unitarias e integración
4. Migrar errores de `alert()` a notificaciones Vuetify
5. Centralizar validación con FluentValidation
6. Agregar refresh tokens y rate limiting
