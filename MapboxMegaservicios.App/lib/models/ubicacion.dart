class UbicacionDTO {
  final int empleadoId;
  final String empleadoNombre;
  final double latitud;
  final double longitud;
  final DateTime? fechaHora;
  final bool? estaEnGeocerca;
  final String estado;
  final String lugarTrabajo;
  final bool isPossibleSpoofing;

  UbicacionDTO({
    required this.empleadoId,
    required this.empleadoNombre,
    required this.latitud,
    required this.longitud,
    this.fechaHora,
    this.estaEnGeocerca,
    this.estado = '',
    this.lugarTrabajo = '',
    this.isPossibleSpoofing = false,
  });

  factory UbicacionDTO.fromJson(Map<String, dynamic> json) {
    DateTime? parseFecha(dynamic value) {
      if (value == null) return null;
      if (value is String && value.isNotEmpty) {
        return DateTime.tryParse(value);
      }
      return null;
    }

    return UbicacionDTO(
      empleadoId: json['empleadoId'] ?? json['EmpleadoId'] ?? 0,
      empleadoNombre:
          json['empleadoNombre'] ?? json['EmpleadoNombre'] ?? '',
      latitud: (json['latitud'] ?? json['Latitud'] ?? 0).toDouble(),
      longitud: (json['longitud'] ?? json['Longitud'] ?? 0).toDouble(),
      fechaHora: parseFecha(json['fechaHora'] ?? json['FechaHora']),
      estaEnGeocerca:
          json['estaEnGeocerca'] ?? json['EstaEnGeocerca'],
      estado: json['estado'] ?? json['Estado'] ?? '',
      lugarTrabajo:
          json['lugarTrabajo'] ?? json['LugarTrabajo'] ?? '',
      isPossibleSpoofing:
          json['isPossibleSpoofing'] ?? json['IsPossibleSpoofing'] ?? false,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'empleadoId': empleadoId,
      'empleadoNombre': empleadoNombre,
      'latitud': latitud,
      'longitud': longitud,
      'fechaHora': fechaHora?.toIso8601String(),
      'estaEnGeocerca': estaEnGeocerca,
      'estado': estado,
      'lugarTrabajo': lugarTrabajo,
      'isPossibleSpoofing': isPossibleSpoofing,
    };
  }

  @override
  String toString() =>
      'UbicacionDTO(empleado: $empleadoNombre, lat: $latitud, lng: $longitud, estado: $estado)';
}
