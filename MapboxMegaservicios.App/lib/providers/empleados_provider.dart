import 'package:flutter/material.dart';
import '../models/empleado.dart';
import '../services/api_service.dart';

class EmpleadosProvider extends ChangeNotifier {
  List<Empleado> _empleados = [];
  List<LugarTrabajo> _lugares = [];
  DashboardStats _stats = DashboardStats();
  bool _isLoading = false;
  String? _error;
  String _searchQuery = '';

  List<Empleado> get empleados => _empleados;
  List<Empleado> get empleadosFiltrados {
    if (_searchQuery.isEmpty) return _empleados;
    final term = _searchQuery.toLowerCase();
    return _empleados.where((e) {
      return e.nombreDisplay.toLowerCase().contains(term) ||
          e.ci.toLowerCase().contains(term) ||
          (e.usuario?.toLowerCase().contains(term) ?? false) ||
          (e.lugarActual?.toLowerCase().contains(term) ?? false);
    }).toList();
  }

  List<LugarTrabajo> get lugares => _lugares;
  DashboardStats get stats => _stats;
  bool get isLoading => _isLoading;
  String? get error => _error;
  String get searchQuery => _searchQuery;

  void setSearchQuery(String query) {
    _searchQuery = query;
    notifyListeners();
  }

  Future<void> loadDashboard() async {
    _isLoading = true;
    notifyListeners();

    try {
      final data = await apiService.get('/admin/dashboard/estadisticas');
      _stats = DashboardStats.fromJson(data);
      _error = null;
    } on ApiException catch (e) {
      _error = e.message;
    } catch (e) {
      _error = 'Error cargando dashboard';
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> loadEmpleados() async {
    _isLoading = true;
    notifyListeners();

    try {
      final data = await apiService.get('/admin/empleados');
      _empleados = (data as List).map((e) => Empleado.fromJson(e)).toList();
      _error = null;
    } on ApiException catch (e) {
      _error = e.message;
    } catch (e) {
      _error = 'Error cargando empleados';
    }

    _isLoading = false;
    notifyListeners();
  }

  Future<void> loadLugares() async {
    try {
      final data = await apiService.get('/admin/lugares');
      _lugares = (data as List).map((l) => LugarTrabajo.fromJson(l)).toList();
      notifyListeners();
    } on ApiException catch (e) {
      _error = e.message;
      notifyListeners();
    } catch (e) {
      _error = 'Error cargando lugares';
      notifyListeners();
    }
  }

  Future<bool> createEmpleado(Map<String, dynamic> data) async {
    try {
      await apiService.post('/admin/empleados', data);
      await loadEmpleados();
      return true;
    } on ApiException catch (e) {
      _error = e.message;
      notifyListeners();
      return false;
    } catch (e) {
      _error = 'Error creando empleado';
      notifyListeners();
      return false;
    }
  }

  Future<bool> updateEmpleado(int id, Map<String, dynamic> data) async {
    try {
      await apiService.put('/admin/empleados/$id', data);
      await loadEmpleados();
      return true;
    } on ApiException catch (e) {
      _error = e.message;
      notifyListeners();
      return false;
    } catch (e) {
      _error = 'Error actualizando empleado';
      notifyListeners();
      return false;
    }
  }

  Future<bool> toggleActivo(int id) async {
    try {
      await apiService.patch('/admin/empleados/$id/estadoemp', {});
      await loadEmpleados();
      return true;
    } on ApiException catch (e) {
      _error = e.message;
      notifyListeners();
      return false;
    } catch (e) {
      _error = 'Error cambiando estado';
      notifyListeners();
      return false;
    }
  }

  Future<bool> cambiarLugar(int empleadoId, int? lugarId,
      {String observaciones = ''}) async {
    try {
      await apiService.patch('/admin/empleados/$empleadoId/lugar-trabajo', {
        'lugarTrabajoId': lugarId,
        'observaciones': observaciones.isEmpty
            ? 'Cambio desde app móvil'
            : observaciones,
      });
      await loadEmpleados();
      return true;
    } on ApiException catch (e) {
      _error = e.message;
      notifyListeners();
      return false;
    } catch (e) {
      _error = 'Error cambiando lugar';
      notifyListeners();
      return false;
    }
  }

  void clearError() {
    _error = null;
    notifyListeners();
  }
}
