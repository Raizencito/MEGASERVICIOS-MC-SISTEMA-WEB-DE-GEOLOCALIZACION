import 'dart:convert';

class LugarConGeocerca {
  final int id;
  final String nombre;
  final String direccion;
  final String? descripcion;
  final int? departamentoId;
  final int totalEmpleados;
  final bool activo;
  final String? geocercaGeoJSON;
  final double? centroLatitud;
  final double? centroLongitud;

  LugarConGeocerca({
    required this.id,
    required this.nombre,
    required this.direccion,
    this.descripcion,
    this.departamentoId,
    this.totalEmpleados = 0,
    this.activo = true,
    this.geocercaGeoJSON,
    this.centroLatitud,
    this.centroLongitud,
  });

  factory LugarConGeocerca.fromJson(Map<String, dynamic> json) {
    return LugarConGeocerca(
      id: json['id'] ?? json['Id'] ?? 0,
      nombre: json['nombre'] ?? json['Nombre'] ?? '',
      direccion: json['direccion'] ?? json['Direccion'] ?? '',
      descripcion: json['descripcion'] ?? json['Descripcion'],
      departamentoId:
          json['departamentoId'] ?? json['DepartamentoId'],
      totalEmpleados:
          json['totalEmpleados'] ?? json['TotalEmpleados'] ?? 0,
      activo: json['activo'] ?? json['Activo'] ?? true,
      geocercaGeoJSON:
          json['geocercaGeoJSON'] ?? json['GeocercaGeoJSON'],
      centroLatitud: json['centroLatitud'] != null
          ? (json['centroLatitud'] as num).toDouble()
          : json['CentroLatitud'] != null
              ? (json['CentroLatitud'] as num).toDouble()
              : null,
      centroLongitud: json['centroLongitud'] != null
          ? (json['centroLongitud'] as num).toDouble()
          : json['CentroLongitud'] != null
              ? (json['CentroLongitud'] as num).toDouble()
              : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'nombre': nombre,
      'direccion': direccion,
      'descripcion': descripcion,
      'departamentoId': departamentoId,
      'totalEmpleados': totalEmpleados,
      'activo': activo,
      'geocercaGeoJSON': geocercaGeoJSON,
      'centroLatitud': centroLatitud,
      'centroLongitud': centroLongitud,
    };
  }

  /// Parses the raw GeoJSON string into a list of [lng, lat] coordinate pairs.
  /// Returns an empty list if the GeoJSON is null, empty, or cannot be parsed.
  List<List<double>> get coordenadas => _parseGeoJSON();

  List<List<double>> _parseGeoJSON() {
    if (geocercaGeoJSON == null || geocercaGeoJSON!.isEmpty) {
      return [];
    }

    try {
      final parsed = jsonDecode(geocercaGeoJSON!);

      // Handle GeoJSON Feature
      if (parsed is Map<String, dynamic> && parsed['type'] == 'Feature') {
        return _extractFromGeometry(
            parsed['geometry'] as Map<String, dynamic>?);
      }

      // Handle GeoJSON FeatureCollection
      if (parsed is Map<String, dynamic> &&
          parsed['type'] == 'FeatureCollection') {
        final features = parsed['features'] as List? ?? [];
        if (features.isNotEmpty) {
          final firstFeature = features[0] as Map<String, dynamic>;
          return _extractFromGeometry(
              firstFeature['geometry'] as Map<String, dynamic>?);
        }
        return [];
      }

      // Handle direct Geometry object (Polygon, MultiPolygon)
      if (parsed is Map<String, dynamic> && parsed['coordinates'] != null) {
        return _extractFromGeometry(parsed);
      }

      return [];
    } catch (_) {
      return [];
    }
  }

  List<List<double>> _extractFromGeometry(Map<String, dynamic>? geometry) {
    if (geometry == null || geometry['coordinates'] == null) {
      return [];
    }

    final type = geometry['type'] as String? ?? '';
    final coordinates = geometry['coordinates'];

    if (type == 'Polygon') {
      // Polygon coordinates: [[[lng, lat], [lng, lat], ...]]
      // Take the outer ring (first element)
      final outerRing = (coordinates as List).first as List;
      return _convertCoordList(outerRing);
    }

    if (type == 'MultiPolygon') {
      // MultiPolygon coordinates: [[[[lng, lat], ...]], [[[lng, lat], ...]]]
      // Take the outer ring of the first polygon
      final firstPolygon = (coordinates as List).first as List;
      final outerRing = firstPolygon.first as List;
      return _convertCoordList(outerRing);
    }

    if (type == 'Point') {
      // Point coordinates: [lng, lat]
      final coord = coordinates as List;
      if (coord.length >= 2) {
        return [
          [(coord[0] as num).toDouble(), (coord[1] as num).toDouble()]
        ];
      }
      return [];
    }

    if (type == 'LineString') {
      // LineString coordinates: [[lng, lat], [lng, lat], ...]
      return _convertCoordList(coordinates as List);
    }

    return [];
  }

  List<List<double>> _convertCoordList(List coords) {
    final result = <List<double>>[];
    for (final coord in coords) {
      if (coord is List && coord.length >= 2) {
        result.add([
          (coord[0] as num).toDouble(),
          (coord[1] as num).toDouble(),
        ]);
      }
    }
    return result;
  }

  /// Whether this place has a valid geocerca defined.
  bool get tieneGeocerca => coordenadas.isNotEmpty;

  /// Whether this place has a valid center coordinate.
  bool get tieneCentro => centroLatitud != null && centroLongitud != null;

  @override
  String toString() =>
      'LugarConGeocerca(id: $id, nombre: $nombre, tieneGeocerca: $tieneGeocerca)';
}
