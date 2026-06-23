import 'dart:convert';
import 'dart:io' show Platform;
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiService {
  static String? customBaseUrl;

  static String get _baseUrl {
    if (customBaseUrl != null && customBaseUrl!.isNotEmpty) {
      return customBaseUrl!;
    }
    if (kIsWeb) {
      return 'http://localhost:5001/api';
    }
    if (Platform.isAndroid) {
      return 'http://192.168.0.191:5001/api';
    }
    return 'http://localhost:5001/api';
  }

  static String get baseUrl => _baseUrl;

  Future<Map<String, String>> _getHeaders({bool withAuth = true}) async {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (withAuth) {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('token');
      if (token != null) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    return headers;
  }

  Future<dynamic> get(String endpoint, {bool withAuth = true}) async {
    try {
      final url = Uri.parse('$_baseUrl$endpoint');
      final headers = await _getHeaders(withAuth: withAuth);

      final response = await http.get(url, headers: headers).timeout(
        const Duration(seconds: 30),
      );

      return _handleResponse(response);
    } catch (e) {
      throw ApiException('Error de conexión: ${e.toString()}');
    }
  }

  Future<dynamic> post(String endpoint, Map<String, dynamic> body,
      {bool withAuth = true}) async {
    try {
      final url = Uri.parse('$_baseUrl$endpoint');
      final headers = await _getHeaders(withAuth: withAuth);

      final response = await http
          .post(url, headers: headers, body: jsonEncode(body))
          .timeout(const Duration(seconds: 30));

      return _handleResponse(response);
    } catch (e) {
      throw ApiException('Error de conexión: ${e.toString()}');
    }
  }

  Future<dynamic> postList(String endpoint, List<dynamic> body,
      {bool withAuth = true}) async {
    try {
      final url = Uri.parse('$_baseUrl$endpoint');
      final headers = await _getHeaders(withAuth: withAuth);

      final response = await http
          .post(url, headers: headers, body: jsonEncode(body))
          .timeout(const Duration(seconds: 30));

      return _handleResponse(response);
    } catch (e) {
      throw ApiException('Error de conexión: ${e.toString()}');
    }
  }

  Future<dynamic> put(String endpoint, Map<String, dynamic> body,
      {bool withAuth = true}) async {
    try {
      final url = Uri.parse('$_baseUrl$endpoint');
      final headers = await _getHeaders(withAuth: withAuth);

      final response = await http
          .put(url, headers: headers, body: jsonEncode(body))
          .timeout(const Duration(seconds: 30));

      return _handleResponse(response);
    } catch (e) {
      throw ApiException('Error de conexión: ${e.toString()}');
    }
  }

  Future<dynamic> patch(String endpoint, Map<String, dynamic> body,
      {bool withAuth = true}) async {
    try {
      final url = Uri.parse('$_baseUrl$endpoint');
      final headers = await _getHeaders(withAuth: withAuth);

      final response = await http
          .patch(url, headers: headers, body: jsonEncode(body))
          .timeout(const Duration(seconds: 30));

      return _handleResponse(response);
    } catch (e) {
      throw ApiException('Error de conexión: ${e.toString()}');
    }
  }

  Future<dynamic> delete(String endpoint, {bool withAuth = true}) async {
    try {
      final url = Uri.parse('$_baseUrl$endpoint');
      final headers = await _getHeaders(withAuth: withAuth);

      final response = await http.delete(url, headers: headers).timeout(
        const Duration(seconds: 30),
      );

      return _handleResponse(response);
    } catch (e) {
      throw ApiException('Error de conexión: ${e.toString()}');
    }
  }

  dynamic _handleResponse(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) return {};
      return jsonDecode(response.body);
    } else if (response.statusCode == 401) {
      // Token expired - clear storage
      SharedPreferences.getInstance().then((prefs) {
        prefs.remove('token');
        prefs.remove('user');
      });
      throw ApiException('Sesión expirada. Inicie sesión nuevamente.');
    } else {
      String message = 'Error del servidor';
      try {
        final body = jsonDecode(response.body);
        message = body['message'] ?? body['Message'] ?? message;
      } catch (_) {}
      throw ApiException(message);
    }
  }
}

class ApiException implements Exception {
  final String message;
  ApiException(this.message);

  @override
  String toString() => message;
}

final apiService = ApiService();
