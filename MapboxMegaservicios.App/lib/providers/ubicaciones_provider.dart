import 'package:flutter/material.dart';
import '../models/ubicacion.dart';
import '../models/lugar_geocerca.dart';
import '../services/ubicaciones_service.dart';
import '../services/api_service.dart';

class UbicacionesProvider extends ChangeNotifier {
  List<LugarConGeocerca> _lugares = [];
  UbicacionDTO? _ultimaUbicacion;
  bool _isLoading = false;
  String? _error;
  bool _trackingActivo = false;

  List<LugarConGeocerca> get lugares => List.unmodifiable(_lugares);
  UbicacionDTO? get ultimaUbicacion => _ultimaUbicacion;
  bool get isLoading => _isLoading;
  String? get error => _error;
  bool get trackingActivo => _trackingActivo;

  /// Returns only places that have a geocerca (geofence) defined.
  List<LugarConGeocerca> get lugaresConGeocerca =>
      _lugares.where((l) => l.tieneGeocerca).toList();

  /// Fetches all work places with their geocerca data from the backend.
  Future<void> loadLugaresConGeocercas() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _lugares = await ubicacionesService.obtenerLugaresConGeocercas();
      _isLoading = false;
      notifyListeners();
    } on ApiException catch (e) {
      _error = e.message;
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _error = 'Error al cargar lugares con geocercas';
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Sends the current device coordinates to the backend for tracking.
  /// Returns the resulting [UbicacionDTO] on success, or null on failure.
  Future<UbicacionDTO?> registrarUbicacion(double lat, double lng) async {
    _error = null;

    try {
      _ultimaUbicacion =
          await ubicacionesService.registrarUbicacion(lat, lng);
      notifyListeners();
      return _ultimaUbicacion;
    } on ApiException catch (e) {
      _error = e.message;
      notifyListeners();
      return null;
    } catch (e) {
      _error = 'Error al registrar ubicación';
      notifyListeners();
      return null;
    }
  }

  /// Toggles the background location tracking state.
  void toggleTracking() {
    _trackingActivo = !_trackingActivo;
    notifyListeners();
  }

  /// Explicitly sets the tracking state.
  void setTracking(bool activo) {
    if (_trackingActivo != activo) {
      _trackingActivo = activo;
      notifyListeners();
    }
  }

  /// Clears the current error message.
  void clearError() {
    if (_error != null) {
      _error = null;
      notifyListeners();
    }
  }
}
