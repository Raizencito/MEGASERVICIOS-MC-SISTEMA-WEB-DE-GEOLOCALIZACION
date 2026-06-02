import 'package:flutter/material.dart';
import '../models/jornada.dart';
import '../services/asistencia_service.dart';
import '../services/api_service.dart';

class AsistenciaProvider extends ChangeNotifier {
  JornadaCompleta? _jornadaHoy;
  List<JornadaCompleta> _historial = [];
  bool _isLoading = false;
  String? _error;
  String? _successMessage;

  JornadaCompleta? get jornadaHoy => _jornadaHoy;
  List<JornadaCompleta> get historial => List.unmodifiable(_historial);
  bool get isLoading => _isLoading;
  String? get error => _error;
  String? get successMessage => _successMessage;

  /// Whether the employee has an active workday (entered but not exited).
  bool get tieneJornadaActiva => _jornadaHoy?.estaActiva ?? false;

  /// Whether the employee has already completed today's workday.
  bool get jornadaFinalizada => _jornadaHoy?.estaFinalizada ?? false;

  /// Whether the employee has not yet started today's workday.
  bool get sinJornada =>
      _jornadaHoy == null || _jornadaHoy!.horaEntrada == null;

  /// Fetches today's workday data for the authenticated employee.
  Future<void> loadJornadaHoy() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _jornadaHoy = await asistenciaService.obtenerJornadaHoy();
      _isLoading = false;
      notifyListeners();
    } on ApiException catch (e) {
      _error = e.message;
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _error = 'Error al cargar jornada de hoy';
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Marks attendance entry at the given coordinates.
  /// Returns true on success, false on failure (check [error] for details).
  Future<bool> marcarEntrada(double lat, double lng) async {
    _isLoading = true;
    _error = null;
    _successMessage = null;
    notifyListeners();

    try {
      final result = await asistenciaService.marcarEntrada(lat, lng);

      if (result.success) {
        _successMessage = result.message;
        // Reload today's workday to get the updated state
        await _reloadJornadaHoy();
        _isLoading = false;
        notifyListeners();
        return true;
      }

      _error = result.message;
      _isLoading = false;
      notifyListeners();
      return false;
    } on ApiException catch (e) {
      _error = e.message;
      _isLoading = false;
      notifyListeners();
      return false;
    } catch (e) {
      _error = 'Error al marcar entrada';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// Marks attendance exit at the given coordinates.
  /// Returns true on success, false on failure (check [error] for details).
  Future<bool> marcarSalida(double lat, double lng) async {
    _isLoading = true;
    _error = null;
    _successMessage = null;
    notifyListeners();

    try {
      final result = await asistenciaService.marcarSalida(lat, lng);

      if (result.success) {
        _successMessage = result.message;
        // Reload today's workday to get the updated state
        await _reloadJornadaHoy();
        _isLoading = false;
        notifyListeners();
        return true;
      }

      _error = result.message;
      _isLoading = false;
      notifyListeners();
      return false;
    } on ApiException catch (e) {
      _error = e.message;
      _isLoading = false;
      notifyListeners();
      return false;
    } catch (e) {
      _error = 'Error al marcar salida';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// Fetches attendance history, optionally filtered by date range.
  Future<void> loadHistorial({DateTime? desde, DateTime? hasta}) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _historial = await asistenciaService.obtenerMisAsistencias(
        desde: desde,
        hasta: hasta,
      );
      _isLoading = false;
      notifyListeners();
    } on ApiException catch (e) {
      _error = e.message;
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _error = 'Error al cargar historial de asistencias';
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Clears both error and success messages.
  void clearMessages() {
    if (_error != null || _successMessage != null) {
      _error = null;
      _successMessage = null;
      notifyListeners();
    }
  }

  /// Internal helper to silently reload today's jornada after marking
  /// entrada/salida, without resetting loading or error state.
  Future<void> _reloadJornadaHoy() async {
    try {
      _jornadaHoy = await asistenciaService.obtenerJornadaHoy();
    } catch (_) {
      // Silently ignore — the main operation already succeeded
    }
  }
}
