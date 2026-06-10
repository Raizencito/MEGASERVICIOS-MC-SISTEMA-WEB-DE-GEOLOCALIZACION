import 'dart:async';
import 'dart:ui';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:geolocator/geolocator.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'ubicaciones_service.dart';

class BgLocationService {
  static const String notificationChannelId = 'bg_location_sync';
  static const int notificationId = 888;

  /// Initializes and configures the background service.
  static Future<void> initialize() async {
    final service = FlutterBackgroundService();

    // Set up local notifications channel
    const AndroidNotificationChannel channel = AndroidNotificationChannel(
      notificationChannelId,
      'Monitoreo GPS Activo',
      description: 'Este canal se usa para mostrar la notificación de geolocalización en segundo plano.',
      importance: Importance.low,
    );

    final FlutterLocalNotificationsPlugin flutterLocalNotificationsPlugin =
        FlutterLocalNotificationsPlugin();

    await flutterLocalNotificationsPlugin
        .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(channel);

    await service.configure(
      androidConfiguration: AndroidConfiguration(
        onStart: onStart,
        autoStart: false,
        isForegroundMode: true,
        notificationChannelId: notificationChannelId,
        initialNotificationTitle: 'Rastreo de Ubicación Activo',
        initialNotificationContent: 'Sincronizando ubicación en segundo plano...',
        foregroundServiceNotificationId: notificationId,
      ),
      iosConfiguration: IosConfiguration(
        autoStart: false,
        onForeground: onStart,
        onBackground: onIosBackground,
      ),
    );
  }

  @pragma('vm:entry-point')
  static Future<bool> onIosBackground(ServiceInstance service) async {
    return true;
  }

  @pragma('vm:entry-point')
  static void onStart(ServiceInstance service) async {
    DartPluginRegistrant.ensureInitialized();

    final FlutterLocalNotificationsPlugin flutterLocalNotificationsPlugin =
        FlutterLocalNotificationsPlugin();

    // Event listener to stop the service from Dart/UI
    service.on('stopService').listen((event) {
      service.stopSelf();
    });

    // Start a periodic timer to fetch and post location coordinates every 10 seconds (tiempo real)
    Timer.periodic(const Duration(seconds: 10), (timer) async {
      if (service is AndroidServiceInstance) {
        if (!(await service.isForegroundService())) {
          timer.cancel();
          return;
        }
      }

      try {
        // 1. Verify that GPS location services are enabled
        final bool isEnabled = await Geolocator.isLocationServiceEnabled();
        if (!isEnabled) {
          _updateNotification(
            flutterLocalNotificationsPlugin,
            'SGE MegaServicios — GPS Desactivado',
            'Por favor, active la ubicación/GPS en su dispositivo.',
          );
          return;
        }

        // 2. Verify and obtain the current location permission
        LocationPermission permission = await Geolocator.checkPermission();
        if (permission == LocationPermission.denied ||
            permission == LocationPermission.deniedForever) {
          _updateNotification(
            flutterLocalNotificationsPlugin,
            'SGE MegaServicios — Permiso Denegado',
            'Se requiere permiso de ubicación siempre activa para rastreo.',
          );
          return;
        }

        // 3. Query the current GPS location coordinates
        final Position pos = await Geolocator.getCurrentPosition(
          locationSettings: const LocationSettings(
            accuracy: LocationAccuracy.high,
            distanceFilter: 10,
          ),
        );

        // 4. Retrieve stored authentication token
        final prefs = await SharedPreferences.getInstance();
        final token = prefs.getString('token');

        if (token == null) {
          _updateNotification(
            flutterLocalNotificationsPlugin,
            'SGE MegaServicios — Sesión Requerida',
            'Inicie sesión en la app para sincronizar su ubicación.',
          );
          return;
        }

        // 5. Post location to server
        final result = await ubicacionesService.registrarUbicacion(
          pos.latitude,
          pos.longitude,
        );

        // 6. Update persistent foreground notification with actual status
        final String inGeofence = result.estaEnGeocerca == true 
            ? 'Dentro de geocerca' 
            : 'Fuera de geocerca ⚠️';
        final String workplace = result.lugarTrabajo.isNotEmpty 
            ? ' en ${result.lugarTrabajo}' 
            : '';

        _updateNotification(
          flutterLocalNotificationsPlugin,
          'Monitoreo GPS Activo',
          '$inGeofence$workplace — SGE MegaServicios',
        );
      } catch (e) {
        _updateNotification(
          flutterLocalNotificationsPlugin,
          'SGE — Error de sincronización',
          '${e.toString().substring(0, (e.toString().length > 80) ? 80 : e.toString().length)}',
        );
      }
    });
  }

  static void _updateNotification(
    FlutterLocalNotificationsPlugin plugin,
    String title,
    String body,
  ) {
    plugin.show(
      notificationId,
      title,
      body,
      const NotificationDetails(
        android: AndroidNotificationDetails(
          notificationChannelId,
          'Monitoreo GPS Activo',
          channelDescription: 'Monitoreo GPS en segundo plano',
          ongoing: true,
          importance: Importance.low,
          priority: Priority.low,
          icon: '@mipmap/ic_launcher',
        ),
      ),
    );
  }
}
