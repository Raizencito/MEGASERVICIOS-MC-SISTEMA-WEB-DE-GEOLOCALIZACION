# Guía para la App Móvil — Antigravity

## Conectar la App Flutter con la API

### API Base URL (configurable)
```
http://<IP_SERVIDOR>:5001/api
```
En desarrollo local: `http://10.0.2.2:5001/api` (Android emulator)  
En producción: URL pública del servidor

### Autenticación

Todas las rutas excepto `/api/auth/login` requieren el header:

```
Authorization: Bearer <token>
```

**Flujo de login:**

1. `POST /api/auth/login`
   ```json
   { "usuario": "admin", "password": "admin123" }
   ```
   **Respuesta exitosa:**
   ```json
   {
     "success": true,
     "message": "Login exitoso",
     "token": "eyJhbG...",
     "empleado": {
       "id": 1,
       "nombreCompleto": "Admin Sistema",
       "usuario": "admin",
       "rol": "ADMIN"
     }
   }
   ```

2. Guardar el `token` en `shared_preferences`
3. Incluir `Authorization: Bearer <token>` en todos los requests posteriores
4. Si el backend responde 401 → redirigir a login

---

## Endpoints de la API

### Autenticación

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/auth/login` | No | Login, devuelve JWT |
| GET | `/auth/current-user` | JWT | Info del usuario logueado |
| POST | `/auth/logout` | No | Cierre de sesión |

### Asistencia (para el empleado en campo)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/asistencia/marcar-entrada` | JWT | Marcar entrada (solo dentro de geocerca) |
| POST | `/asistencia/marcar-salida` | JWT | Marcar salida (solo dentro de geocerca) |
| GET | `/asistencia/mi-jornada-hoy` | JWT | Ver jornada del día actual |
| GET | `/asistencia/mis-asistencias?desde=&hasta=` | JWT | Historial de asistencias |

**Body para marcar entrada/salida:**
```json
{ "latitud": -16.489, "longitud": -68.119 }
```

**Respuesta:**
```json
{
  "success": true,
  "message": "Entrada registrada exitosamente",
  "tipo": "ENTRADA",
  "fechaHora": "2026-06-01T14:30:00Z"
}
```

Posibles errores:
- `"Solo puedes marcar entrada dentro de tu área de trabajo asignada"` (fuera de geocerca)
- `"Ya tienes una entrada registrada hoy"` (duplicada)
- `"No tienes una entrada registrada hoy"` (salida sin entrada)

### Registro / Ubicaciones (para la app del empleado)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/registro/marcar-entrada` | JWT | Alternativa a asistencia (sin validación de duplicado) |
| POST | `/registro/marcar-salida` | JWT | Alternativa a asistencia |
| GET | `/registro/mi-ubicacion-actual` | JWT | Última ubicación registrada |
| POST | `/ubicaciones/registrar` | JWT | Enviar ubicación GPS en tiempo real |

**Body para registrar ubicación:**
```json
{ "latitud": -16.489, "longitud": -68.119 }
```

**Respuesta de `POST /ubicaciones/registrar`:**
```json
{
  "empleadoId": 1,
  "empleadoNombre": "Juan Perez",
  "latitud": -16.489,
  "longitud": -68.119,
  "fechaHora": "2026-06-01T14:30:00Z",
  "estaEnGeocerca": true,
  "estado": "Dentro de geocerca",
  "lugarTrabajo": "Oficina Central",
  "isPossibleSpoofing": false
}
```

### Admin (solo usuarios con rol "Administrador")

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/admin/dashboard/estadisticas` | Estadísticas del dashboard |
| GET | `/admin/empleados` | Listar todos los empleados |
| GET | `/admin/empleados/{id}` | Obtener empleado por ID |
| POST | `/admin/empleados` | Crear empleado |
| PUT | `/admin/empleados/{id}` | Actualizar empleado |
| DELETE | `/admin/empleados/{id}` | Desactivar empleado |
| PATCH | `/admin/empleados/{id}/estadoemp` | Toggle activo/inactivo |
| PATCH | `/admin/empleados/{id}/lugar-trabajo` | Cambiar lugar de trabajo |
| GET | `/admin/empleados/buscar?termino=` | Buscar empleados |
| GET | `/admin/lugares` | Listar lugares de trabajo |
| GET | `/admin/lugares/geocercas` | Listar lugares con GeoJSON |
| POST | `/admin/lugares` | Crear lugar de trabajo |
| PUT | `/admin/lugares/{id}` | Actualizar lugar |
| DELETE | `/admin/lugares/{id}` | Eliminar lugar |
| GET | `/admin/lugares/{id}/geocerca` | Obtener GeoJSON de una geocerca |
| PUT | `/admin/lugares/{id}/geocerca` | Actualizar geocerca |
| GET | `/admin/reportes/alertas` | Reporte de alertas |
| GET | `/admin/reportes/asistencia` | Reporte de asistencia |
| GET | `/admin/reportes/tiempos-fuera` | Reporte de tiempos fuera |
| GET | `/admin/reportes/improductividad` | Reporte RF-05 improductividad |
| GET | `/ubicaciones/ultimas` | Últimas ubicaciones de todos los empleados |
| GET | `/ubicaciones/alertas` | Alertas recientes (filtro `?desde=&hasta=&empleadoId=`) |

---

## Modelos de Datos

### Empleado (DTO para el frontend/mobile)
```json
{
  "id": 1,
  "nombreCompleto": "Juan Perez Mamani",
  "paterno": "Perez",
  "materno": "Mamani",
  "nombres": "Juan",
  "ci": "12345678",
  "usuario": "juan.perez",
  "telefono": "71234567",
  "rol": "Empleado",
  "lugarActual": "Oficina Central",
  "idLugarTrabajo": 1,
  "idRol": 2,
  "activo": true,
  "fechaCreacion": "2026-01-01T00:00:00Z"
}
```

### EmpleadoCreadoDTO (respuesta después de crear)
```json
{
  "id": 1,
  "nombreCompleto": "Juan Perez",
  "usuario": "juan.perez",
  "password": "perez123",
  "telefono": "71234567",
  "mensaje": "¡GUARDE ESTAS CREDENCIALES!"
}
```

### UbicacionDTO
```json
{
  "empleadoId": 1,
  "empleadoNombre": "Juan Perez",
  "latitud": -16.489,
  "longitud": -68.119,
  "fechaHora": "2026-06-01T14:30:00Z",
  "estaEnGeocerca": true,
  "estado": "Dentro de geocerca",
  "lugarTrabajo": "Oficina Central"
}
```

### AlertaGeocercaDTO
```json
{
  "id": 1,
  "empleadoNombre": "Juan Perez",
  "tipoAlerta": "DENTRO",
  "fechaHora": "2026-06-01T14:30:00Z",
  "observaciones": "Empleado ingresó al área de trabajo"
}
```

### DashboardEstadisticasDTO
```json
{
  "totalEmpleados": 50,
  "empleadosActivos": 45,
  "empleadosEnGeocerca": 40,
  "empleadosFueraGeocerca": 5,
  "alertasHoy": 12,
  "totalLugares": 8,
  "empleadosSinUbicacion": 5,
  "ultimasAlertas": []
}
```

### JornadaDTO
```json
{
  "id": 1,
  "fecha": "2026-06-01",
  "horaEntrada": "2026-06-01T08:00:00Z",
  "horaSalida": "2026-06-01T17:00:00Z",
  "totalHoras": 9.0,
  "estado": "COMPLETADA",
  "tiempoFueraGeocerca": 15,
  "alertasGeneradas": 2,
  "registros": [
    {
      "id": 1,
      "tipoRegistro": "ENTRADA",
      "fechaHora": "2026-06-01T08:00:00Z",
      "observaciones": "Entrada manual",
      "verificado": true,
      "ubicacionCoords": "-16.489, -68.119"
    }
  ]
}
```

---

## Funcionalidades Esperadas en la App Móvil

### 1. Autenticación
- Pantalla de login con usuario y contraseña
- Almacenar token en `shared_preferences`
- Recuperar sesión al iniciar (verificar token no expirado)
- Cerrar sesión (limpiar token)

### 2. Envío de Ubicación GPS en Background
- Usar `flutter_background_service` o similar para enviar ubicación periódicamente
- Enviar a `POST /api/ubicaciones/registrar` cada 30-60 segundos
- Incluir latitud, longitud, velocidad (opcional), batería (opcional)
- El backend detecta automáticamente entrada/salida de geocercas y genera alertas

### 3. Marcación de Asistencia (Entrada/Salida)
- Botón "Marcar Entrada" al iniciar jornada
- Botón "Marcar Salida" al terminar jornada
- Enviar a `POST /api/asistencia/marcar-entrada` o `/marcar-salida`
- Mostrar resultado (éxito o error: "fuera de geocerca", "ya registrado")
- Obtener jornada actual con `GET /api/asistencia/mi-jornada-hoy`

### 4. Dashboard (Resumen para el empleado)
- Datos de su jornada actual (hora entrada, horas trabajadas, estado)
- Alertas recientes propias
- Última ubicación registrada

### 5. Visualización de Mapa (Opcional)
- Mostrar geocercas asignadas (requiere Mapbox o mapa OpenStreetMap)
- Mostrar su propia ubicación en tiempo real

### 6. Reportes (Opcional)
- Ver resumen de alertas y asistencias de los últimos días

---

## Consideraciones Técnicas

### Manejo de Errores HTTP
La API siempre responde con código HTTP estándar:
- `200` → éxito
- `400` → error de validación (mensaje en `message`)
- `401` → no autenticado (redirigir a login)
- `403` → no autorizado (sin permisos de admin)
- `404` → recurso no encontrado
- `500` → error interno del servidor

### Formato de Errores
```json
// Error simple
{ "success": false, "message": "Descripción del error" }

// Error con detalles de validación
{
  "success": false,
  "message": "Errores de validación",
  "errors": ["El teléfono ya está registrado"]
}
```

### Permisos de GPS Requeridos
- `android.permission.ACCESS_FINE_LOCATION`
- `android.permission.ACCESS_BACKGROUND_LOCATION`
- `android.permission.INTERNET`
- iOS: `NSLocationWhenInUseUsageDescription`, `NSLocationAlwaysAndWhenInUseUsageDescription`

### Recomendaciones de Implementación

| Aspecto | Recomendación |
|---------|---------------|
| State management | Provider ya implementado, mantener o migrar a Riverpod |
| HTTP client | `http` package ya usado, mantener |
| Background location | `flutter_background_service` + `location` |
| Token storage | `flutter_secure_storage` (más seguro que shared_preferences) |
| Mapas | `flutter_map` + OpenStreetMap (gratuito) o Mapbox GL |
| Testing | Agregar tests unitarios con `flutter_test` |
| Soporte offline | Cachear últimas ubicaciones y sincronizar cuando haya conexión |

---

## DTOs para Flutter (ejemplos)

```dart
// auth_service.dart - Login
class LoginResponse {
  final bool success;
  final String message;
  final String token;
  final Empleado? empleado;

  LoginResponse.fromJson(Map<String, dynamic> json)
    : success = json['success'],
      message = json['message'],
      token = json['token'] ?? '',
      empleado = json['empleado'] != null 
        ? Empleado.fromJson(json['empleado']) 
        : null;
}

// models/empleado.dart
class Empleado {
  final int id;
  final String nombreCompleto;
  final String? paterno;
  final String? materno;
  final String? nombres;
  final String? ci;
  final String? usuario;
  final String? telefono;
  final String? rol;
  final String? lugarActual;
  final bool activo;

  Empleado.fromJson(Map<String, dynamic> json)
    : id = json['id'],
      nombreCompleto = json['nombreCompleto'] ?? '',
      paterno = json['paterno'],
      materno = json['materno'],
      nombres = json['nombres'],
      ci = json['ci'],
      usuario = json['usuario'],
      telefono = json['telefono'],
      rol = json['rol'],
      lugarActual = json['lugarActual'],
      activo = json['activo'] ?? true;
}

// models/ubicacion.dart
class Ubicacion {
  final int empleadoId;
  final String empleadoNombre;
  final double latitud;
  final double longitud;
  final DateTime? fechaHora;
  final bool? estaEnGeocerca;
  final String estado;
  final String lugarTrabajo;

  Ubicacion.fromJson(Map<String, dynamic> json)
    : empleadoId = json['empleadoId'],
      empleadoNombre = json['empleadoNombre'],
      latitud = (json['latitud'] as num).toDouble(),
      longitud = (json['longitud'] as num).toDouble(),
      fechaHora = json['fechaHora'] != null 
        ? DateTime.parse(json['fechaHora']) 
        : null,
      estaEnGeocerca = json['estaEnGeocerca'],
      estado = json['estado'] ?? '',
      lugarTrabajo = json['lugarTrabajo'] ?? '';
}
```

---

## Arquitectura de la API (para depuración)

```
API URL: http://<host>:5001/api
Swagger: http://<host>:5001/swagger
Base de datos: PostgreSQL 16 + PostGIS

Estructura de tablas principales:
- Empleados (id, paterno, materno, nombres, ci, usuario, password_hash, telefono, activo)
- LugaresTrabajo (id, nombre, direccion, geocerca[Polygon], departamento_id, activo)
- Ubicaciones (id, empleado_id, ubicacion_emp[Point], fecha_hora, esta_en_geocerca)
- AlertasGeocerca (id, empleado_id, estado_alerta_id, fecha_hora, observaciones)
- JornadasTrabajo (id, empleado_id, fecha, hora_entrada, hora_salida, estado)
- RegistrosAsistencia (id, empleado_id, ubicacion_id, tipo_registro, fecha_hora)
```

---

## Contacto

Para dudas técnicas sobre la API o backend, contactar al equipo de backend.
Para cambios en la app móvil, modificar los archivos en `MapboxMegaservicios.App/lib/`.
