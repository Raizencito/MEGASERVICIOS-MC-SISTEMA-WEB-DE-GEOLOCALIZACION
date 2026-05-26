import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import 'api_service.dart';
import '../models/empleado.dart';

class AuthService {
  Future<Empleado?> login(String usuario, String password) async {
    try {
      final data = await apiService.post(
        '/auth/login',
        {'usuario': usuario, 'password': password},
        withAuth: false,
      );

      if (data['success'] == true) {
        final token = data['token'];
        final empleadoJson = data['empleado'];

        if (empleadoJson != null) {
          final empleado = Empleado.fromJson(empleadoJson);

          final prefs = await SharedPreferences.getInstance();
          await prefs.setString('token', token);
          await prefs.setString('user', jsonEncode(empleadoJson));

          return empleado;
        }
      }
      return null;
    } on ApiException {
      rethrow;
    } catch (e) {
      throw ApiException('Error de conexión: ${e.toString()}');
    }
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
    await prefs.remove('user');
  }

  Future<bool> isAuthenticated() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('token');
    return token != null && token.isNotEmpty;
  }

  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('token');
  }

  Future<Empleado?> getUser() async {
    final prefs = await SharedPreferences.getInstance();
    final userStr = prefs.getString('user');
    if (userStr != null) {
      return Empleado.fromJson(jsonDecode(userStr));
    }
    return null;
  }
}

final authService = AuthService();
