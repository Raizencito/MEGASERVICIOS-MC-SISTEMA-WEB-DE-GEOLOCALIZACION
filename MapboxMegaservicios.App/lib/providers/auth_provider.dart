import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/empleado.dart';
import '../services/api_service.dart';

class AuthProvider extends ChangeNotifier {
  Empleado? _user;
  bool _isLoading = false;
  String? _error;

  Empleado? get user => _user;
  bool get isLoading => _isLoading;
  bool get isLoggedIn => _user != null;
  String? get error => _error;

  Future<void> checkAuth() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('token');
    if (token != null && token.isNotEmpty) {
      final userStr = prefs.getString('user');
      if (userStr != null) {
        try {
          final userJson = jsonDecode(userStr);
          _user = Empleado.fromJson(userJson as Map<String, dynamic>);
        } catch (_) {}
      }
      notifyListeners();
    }
  }

  Future<bool> login(String usuario, String password) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final data = await apiService.post(
        '/auth/login',
        {'usuario': usuario, 'password': password},
        withAuth: false,
      );

      if (data['success'] == true) {
        final empleadoJson = data['empleado'] as Map<String, dynamic>;
        _user = Empleado.fromJson(empleadoJson);

        final prefs = await SharedPreferences.getInstance();
        await prefs.setString('token', data['token'] ?? '');
        await prefs.setString('user', jsonEncode(empleadoJson));

        _isLoading = false;
        notifyListeners();
        return true;
      }

      _error = 'Credenciales incorrectas';
      _isLoading = false;
      notifyListeners();
      return false;
    } on ApiException catch (e) {
      _error = e.message;
      _isLoading = false;
      notifyListeners();
      return false;
    } catch (e) {
      _error = 'Error de conexión';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
    await prefs.remove('user');
    _user = null;
    notifyListeners();
  }
}
