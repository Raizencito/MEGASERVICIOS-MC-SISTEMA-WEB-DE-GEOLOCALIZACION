import 'api_service.dart';
import '../models/ubicacion.dart';
import '../models/lugar_geocerca.dart';

class UbicacionesService {
  /// Registers the current device location for the authenticated employee.
  /// POST /api/ubicaciones/registrar
  Future<UbicacionDTO> registrarUbicacion(double lat, double lng) async {
    try {
      final data = await apiService.post(
        '/ubicaciones/registrar',
        {'latitud': lat, 'longitud': lng},
      );

      return UbicacionDTO.fromJson(data as Map<String, dynamic>);
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error al registrar ubicación: ${e.toString()}');
    }
  }

  /// Fetches all work places with their geocerca (geofence) GeoJSON data.
  /// GET /api/admin/lugares/geocercas
  Future<List<LugarConGeocerca>> obtenerLugaresConGeocercas() async {
    try {
      final data = await apiService.get('/admin/lugares/geocercas');

      if (data is List) {
        return data
            .map((item) =>
                LugarConGeocerca.fromJson(item as Map<String, dynamic>))
            .toList();
      }

      return [];
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException(
          'Error al obtener lugares con geocercas: ${e.toString()}');
    }
  }

  /// Fetches location alerts, optionally filtered by date range and employee.
  /// GET /api/ubicaciones/alertas
  Future<List<dynamic>> obtenerAlertas({
    DateTime? desde,
    DateTime? hasta,
    int? empleadoId,
  }) async {
    try {
      final queryParams = <String, String>{};
      if (desde != null) {
        queryParams['desde'] = desde.toIso8601String();
      }
      if (hasta != null) {
        queryParams['hasta'] = hasta.toIso8601String();
      }
      if (empleadoId != null) {
        queryParams['empleadoId'] = empleadoId.toString();
      }

      String endpoint = '/ubicaciones/alertas';
      if (queryParams.isNotEmpty) {
        final queryString = queryParams.entries
            .map((e) =>
                '${Uri.encodeComponent(e.key)}=${Uri.encodeComponent(e.value)}')
            .join('&');
        endpoint = '$endpoint?$queryString';
      }

      final data = await apiService.get(endpoint);

      if (data is List) {
        return data;
      }

      return [];
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error al obtener alertas: ${e.toString()}');
    }
  }
}

final ubicacionesService = UbicacionesService();
