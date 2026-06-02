import 'api_service.dart';
import '../models/jornada.dart';

class AsistenciaService {
  /// Marks attendance entry for the authenticated employee at the given location.
  /// POST /api/asistencia/marcar-entrada
  Future<RegistroResult> marcarEntrada(double lat, double lng) async {
    try {
      final data = await apiService.post(
        '/asistencia/marcar-entrada',
        {'latitud': lat, 'longitud': lng},
      );

      return RegistroResult.fromJson(data as Map<String, dynamic>);
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error al marcar entrada: ${e.toString()}');
    }
  }

  /// Marks attendance exit for the authenticated employee at the given location.
  /// POST /api/asistencia/marcar-salida
  Future<RegistroResult> marcarSalida(double lat, double lng) async {
    try {
      final data = await apiService.post(
        '/asistencia/marcar-salida',
        {'latitud': lat, 'longitud': lng},
      );

      return RegistroResult.fromJson(data as Map<String, dynamic>);
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error al marcar salida: ${e.toString()}');
    }
  }

  /// Fetches today's workday for the authenticated employee.
  /// GET /api/asistencia/mi-jornada-hoy
  Future<JornadaCompleta> obtenerJornadaHoy() async {
    try {
      final data = await apiService.get('/asistencia/mi-jornada-hoy');

      return JornadaCompleta.fromJson(data as Map<String, dynamic>);
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException(
          'Error al obtener jornada de hoy: ${e.toString()}');
    }
  }

  /// Fetches attendance history for the authenticated employee,
  /// optionally filtered by date range.
  /// GET /api/asistencia/mis-asistencias
  Future<List<JornadaCompleta>> obtenerMisAsistencias({
    DateTime? desde,
    DateTime? hasta,
  }) async {
    try {
      final queryParams = <String, String>{};
      if (desde != null) {
        queryParams['desde'] = desde.toIso8601String();
      }
      if (hasta != null) {
        queryParams['hasta'] = hasta.toIso8601String();
      }

      String endpoint = '/asistencia/mis-asistencias';
      if (queryParams.isNotEmpty) {
        final queryString = queryParams.entries
            .map((e) =>
                '${Uri.encodeComponent(e.key)}=${Uri.encodeComponent(e.value)}')
            .join('&');
        endpoint = '$endpoint?$queryString';
      }

      final data = await apiService.get(endpoint);

      if (data is List) {
        return data
            .map((item) =>
                JornadaCompleta.fromJson(item as Map<String, dynamic>))
            .toList();
      }

      return [];
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException(
          'Error al obtener historial de asistencias: ${e.toString()}');
    }
  }
}

final asistenciaService = AsistenciaService();
