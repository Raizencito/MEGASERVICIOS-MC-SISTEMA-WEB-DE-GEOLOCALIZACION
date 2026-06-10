class JornadaCompleta {
  final int id;
  final DateTime? fecha;
  final DateTime? horaEntrada;
  final DateTime? horaSalida;
  final double? totalHoras;
  final String estado;
  final int tiempoFueraGeocerca;
  final int alertasGeneradas;
  final List<RegistroAsistencia> registros;
  final String? mensaje;

  JornadaCompleta({
    required this.id,
    this.fecha,
    this.horaEntrada,
    this.horaSalida,
    this.totalHoras,
    this.estado = '',
    this.tiempoFueraGeocerca = 0,
    this.alertasGeneradas = 0,
    this.registros = const [],
    this.mensaje,
  });

  factory JornadaCompleta.fromJson(Map<String, dynamic> json) {
    DateTime? parseDateTime(dynamic value) {
      if (value == null) return null;
      if (value is String && value.isNotEmpty) {
        return DateTime.tryParse(value);
      }
      return null;
    }

    DateTime? parseFecha(dynamic value) {
      if (value is String && value.isNotEmpty) {
        return DateTime.tryParse(value);
      }
      return null;
    }

    final registrosRaw =
        json['registros'] ?? json['Registros'] ?? [];
    final registros = (registrosRaw as List)
        .map((r) => RegistroAsistencia.fromJson(r as Map<String, dynamic>))
        .toList();

    return JornadaCompleta(
      id: json['id'] ?? json['Id'] ?? 0,
      fecha: parseFecha(json['fecha'] ?? json['Fecha']),
      horaEntrada:
          parseDateTime(json['horaEntrada'] ?? json['HoraEntrada']),
      horaSalida:
          parseDateTime(json['horaSalida'] ?? json['HoraSalida']),
      totalHoras: json['totalHoras'] != null
          ? (json['totalHoras'] as num).toDouble()
          : json['TotalHoras'] != null
              ? (json['TotalHoras'] as num).toDouble()
              : null,
      estado: json['estado'] ?? json['Estado'] ?? '',
      tiempoFueraGeocerca:
          json['tiempoFueraGeocerca'] ?? json['TiempoFueraGeocerca'] ?? 0,
      alertasGeneradas:
          json['alertasGeneradas'] ?? json['AlertasGeneradas'] ?? 0,
      registros: registros,
      mensaje: json['mensaje'] ?? json['Mensaje'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fecha': fecha?.toIso8601String(),
      'horaEntrada': horaEntrada?.toIso8601String(),
      'horaSalida': horaSalida?.toIso8601String(),
      'totalHoras': totalHoras,
      'estado': estado,
      'tiempoFueraGeocerca': tiempoFueraGeocerca,
      'alertasGeneradas': alertasGeneradas,
      'registros': registros.map((r) => r.toJson()).toList(),
      'mensaje': mensaje,
    };
  }

  bool get estaActiva => horaEntrada != null && horaSalida == null;

  bool get estaFinalizada => horaEntrada != null && horaSalida != null;

  @override
  String toString() =>
      'JornadaCompleta(id: $id, fecha: $fecha, estado: $estado)';

}

class RegistroAsistencia {
  final int id;
  final String tipoRegistro;
  final DateTime fechaHora;
  final String? observaciones;
  final bool verificado;
  final String? ubicacionCoords;

  RegistroAsistencia({
    required this.id,
    required this.tipoRegistro,
    required this.fechaHora,
    this.observaciones,
    this.verificado = false,
    this.ubicacionCoords,
  });

  factory RegistroAsistencia.fromJson(Map<String, dynamic> json) {
    DateTime parseFechaHora(dynamic value) {
      if (value is String && value.isNotEmpty) {
        return DateTime.tryParse(value) ?? DateTime.now();
      }
      return DateTime.now();
    }

    return RegistroAsistencia(
      id: json['id'] ?? json['Id'] ?? 0,
      tipoRegistro:
          json['tipoRegistro'] ?? json['TipoRegistro'] ?? '',
      fechaHora:
          parseFechaHora(json['fechaHora'] ?? json['FechaHora']),
      observaciones:
          json['observaciones'] ?? json['Observaciones'],
      verificado:
          json['verificado'] ?? json['Verificado'] ?? false,
      ubicacionCoords:
          json['ubicacionCoords'] ?? json['UbicacionCoords'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'tipoRegistro': tipoRegistro,
      'fechaHora': fechaHora.toIso8601String(),
      'observaciones': observaciones,
      'verificado': verificado,
      'ubicacionCoords': ubicacionCoords,
    };
  }

  bool get esEntrada => tipoRegistro.toUpperCase() == 'ENTRADA';

  bool get esSalida => tipoRegistro.toUpperCase() == 'SALIDA';

  @override
  String toString() =>
      'RegistroAsistencia(id: $id, tipo: $tipoRegistro, hora: $fechaHora)';
}

class RegistroResult {
  final bool success;
  final String message;
  final String tipo;
  final DateTime? fechaHora;

  RegistroResult({
    required this.success,
    required this.message,
    this.tipo = '',
    this.fechaHora,
  });

  factory RegistroResult.fromJson(Map<String, dynamic> json) {
    DateTime? parseFechaHora(dynamic value) {
      if (value == null) return null;
      if (value is String && value.isNotEmpty) {
        return DateTime.tryParse(value);
      }
      return null;
    }

    return RegistroResult(
      success: json['success'] ?? json['Success'] ?? false,
      message: json['message'] ?? json['Message'] ?? '',
      tipo: json['tipo'] ?? json['Tipo'] ?? '',
      fechaHora:
          parseFechaHora(json['fechaHora'] ?? json['FechaHora']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'success': success,
      'message': message,
      'tipo': tipo,
      'fechaHora': fechaHora?.toIso8601String(),
    };
  }

  @override
  String toString() =>
      'RegistroResult(success: $success, message: $message, tipo: $tipo)';
}
