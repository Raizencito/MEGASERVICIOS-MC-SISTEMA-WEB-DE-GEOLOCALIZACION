class Empleado {
  final int id;
  final String? nombreCompleto;
  final String paterno;
  final String? materno;
  final String nombres;
  final String ci;
  final String? usuario;
  final String? telefono;
  final String? rol;
  final String? lugarActual;
  final int? idLugarTrabajo;
  final int? idRol;
  final bool activo;
  final String? fechaCreacion;

  Empleado({
    required this.id,
    this.nombreCompleto,
    this.paterno = '',
    this.materno,
    this.nombres = '',
    this.ci = '',
    this.usuario,
    this.telefono,
    this.rol,
    this.lugarActual,
    this.idLugarTrabajo,
    this.idRol,
    this.activo = true,
    this.fechaCreacion,
  });

  factory Empleado.fromJson(Map<String, dynamic> json) {
    return Empleado(
      id: json['id'] ?? json['Id'] ?? 0,
      nombreCompleto: json['nombreCompleto'] ?? json['NombreCompleto'],
      paterno: json['paterno'] ?? json['Paterno'] ?? '',
      materno: json['materno'] ?? json['Materno'],
      nombres: json['nombres'] ?? json['Nombres'] ?? '',
      ci: json['ci'] ?? json['Ci'] ?? '',
      usuario: json['usuario'] ?? json['Usuario'],
      telefono: json['telefono'] ?? json['Telefono'],
      rol: json['rol'] ?? json['Rol'],
      lugarActual: json['lugarActual'] ?? json['LugarActual'],
      idLugarTrabajo: json['idLugarTrabajo'] ?? json['IdLugarTrabajo'] ?? json['lugarTrabajoActualId'],
      idRol: json['idRol'] ?? json['IdRol'],
      activo: json['activo'] ?? json['Activo'] ?? true,
      fechaCreacion: json['fechaCreacion'] ?? json['FechaCreacion'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'paterno': paterno,
      'materno': materno ?? '',
      'nombres': nombres,
      'ci': ci,
      'telefono': telefono ?? '',
      'idRol': idRol ?? 2,
    };
  }

  String get nombreDisplay =>
      nombreCompleto ?? '$nombres $paterno${materno != null ? ' ${materno}' : ''}';
}

class LugarTrabajo {
  final int id;
  final String nombre;
  final String direccion;
  final String? descripcion;
  final int totalEmpleados;
  final bool activo;
  final int? departamentoId;

  LugarTrabajo({
    required this.id,
    required this.nombre,
    required this.direccion,
    this.descripcion,
    this.totalEmpleados = 0,
    this.activo = true,
    this.departamentoId,
  });

  factory LugarTrabajo.fromJson(Map<String, dynamic> json) {
    return LugarTrabajo(
      id: json['id'] ?? json['Id'] ?? 0,
      nombre: json['nombre'] ?? json['Nombre'] ?? '',
      direccion: json['direccion'] ?? json['Direccion'] ?? '',
      descripcion: json['descripcion'] ?? json['Descripcion'],
      totalEmpleados: json['totalEmpleados'] ?? json['TotalEmpleados'] ?? 0,
      activo: json['activo'] ?? json['Activo'] ?? true,
      departamentoId: json['departamentoId'] ?? json['DepartamentoId'],
    );
  }
}

class DashboardStats {
  final int totalEmpleados;
  final int empleadosEnGeocerca;
  final int empleadosFueraGeocerca;
  final int alertasHoy;
  final List<Alerta> ultimasAlertas;

  DashboardStats({
    this.totalEmpleados = 0,
    this.empleadosEnGeocerca = 0,
    this.empleadosFueraGeocerca = 0,
    this.alertasHoy = 0,
    this.ultimasAlertas = const [],
  });

  factory DashboardStats.fromJson(Map<String, dynamic> json) {
    final alertasRaw = json['ultimasAlertas'] ?? json['UltimasAlertas'] ?? [];
    final alertas = (alertasRaw as List)
        .map((a) => Alerta.fromJson(a as Map<String, dynamic>))
        .toList();

    return DashboardStats(
      totalEmpleados: json['totalEmpleados'] ?? json['TotalEmpleados'] ?? 0,
      empleadosEnGeocerca:
          json['empleadosEnGeocerca'] ?? json['EmpleadosEnGeocerca'] ?? 0,
      empleadosFueraGeocerca:
          json['empleadosFueraGeocerca'] ?? json['EmpleadosFueraGeocerca'] ?? 0,
      alertasHoy: json['alertasHoy'] ?? json['AlertasHoy'] ?? 0,
      ultimasAlertas: alertas,
    );
  }
}

class Alerta {
  final int id;
  final String empleado;
  final String? lugar;
  final String alerta;
  final String fechaHora;
  final String? observaciones;

  Alerta({
    required this.id,
    required this.empleado,
    this.lugar,
    required this.alerta,
    required this.fechaHora,
    this.observaciones,
  });

  factory Alerta.fromJson(Map<String, dynamic> json) {
    return Alerta(
      id: json['id'] ?? json['Id'] ?? 0,
      empleado: json['empleado'] ?? json['Empleado'] ?? json['empleadoNombre'] ?? '',
      lugar: json['lugar'] ?? json['Lugar'],
      alerta: json['alerta'] ?? json['Alerta'] ?? json['tipoAlerta'] ?? json['Estado'] ?? '',
      fechaHora: json['fechaHora'] ?? json['FechaHora'] ?? '',
      observaciones: json['observaciones'] ?? json['Observaciones'],
    );
  }
}

class JornadaDTO {
  final int id;
  final String fecha;
  final String? horaEntrada;
  final String? horaSalida;
  final double? totalHoras;
  final String estado;

  JornadaDTO({
    required this.id,
    required this.fecha,
    this.horaEntrada,
    this.horaSalida,
    this.totalHoras,
    this.estado = '',
  });

  factory JornadaDTO.fromJson(Map<String, dynamic> json) {
    return JornadaDTO(
      id: json['id'] ?? 0,
      fecha: json['fecha'] ?? '',
      horaEntrada: json['horaEntrada'],
      horaSalida: json['horaSalida'],
      totalHoras: (json['totalHoras'] ?? 0).toDouble(),
      estado: json['estado'] ?? '',
    );
  }
}
