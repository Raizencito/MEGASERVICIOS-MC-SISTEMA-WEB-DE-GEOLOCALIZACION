import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import 'package:geolocator/geolocator.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import '../../theme.dart';
import '../../models/jornada.dart';
import '../../providers/asistencia_provider.dart';
import '../../providers/auth_provider.dart';
import '../../services/bg_location_service.dart';

class AsistenciaScreen extends StatefulWidget {
  const AsistenciaScreen({super.key});

  @override
  State<AsistenciaScreen> createState() => _AsistenciaScreenState();
}

class _AsistenciaScreenState extends State<AsistenciaScreen> {
  bool _bgServiceRunning = false;
  Position? _currentPosition;
  bool _retrievingGPS = false;
  String? _gpsError;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AsistenciaProvider>().loadJornadaHoy();
      _checkBackgroundServiceStatus();
      _getLiveLocation();
    });
  }

  Future<void> _checkBackgroundServiceStatus() async {
    try {
      final running = await FlutterBackgroundService().isRunning();
      if (mounted) {
        setState(() {
          _bgServiceRunning = running;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _bgServiceRunning = false;
        });
      }
    }
  }

  Future<void> _getLiveLocation() async {
    if (_retrievingGPS) return;
    setState(() {
      _retrievingGPS = true;
      _gpsError = null;
    });

    try {
      final isEnabled = await Geolocator.isLocationServiceEnabled();
      if (!isEnabled) {
        setState(() {
          _retrievingGPS = false;
          _gpsError = 'El GPS está desactivado';
        });
        return;
      }

      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) {
          setState(() {
            _retrievingGPS = false;
            _gpsError = 'Permisos GPS denegados';
          });
          return;
        }
      }

      if (permission == LocationPermission.deniedForever) {
        setState(() {
          _retrievingGPS = false;
          _gpsError = 'Permisos permanentemente denegados';
        });
        return;
      }

      final pos = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 10),
        ),
      );

      if (mounted) {
        setState(() {
          _currentPosition = pos;
          _retrievingGPS = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _retrievingGPS = false;
          _gpsError = 'Error al obtener GPS: $e';
        });
      }
    }
  }

  Future<void> _requestLocationPermissions() async {
    // 1. Request foreground location
    final foregroundStatus = await Permission.location.request();
    if (foregroundStatus != PermissionStatus.granted) {
      _showErrorSnackBar('Se requieren permisos de ubicación para activar el rastreo.');
      return;
    }

    // 2. Request background location
    final backgroundStatus = await Permission.locationAlways.request();
    if (backgroundStatus != PermissionStatus.granted) {
      // It's a soft warning as Android 10+ requires background location explicitly
      _showWarningSnackBar('El rastreo en segundo plano podría limitarse sin el permiso "Permitir todo el tiempo".');
    }

    // 3. Request notification permission for Android 13+
    if (await Permission.notification.isDenied) {
      await Permission.notification.request();
    }
  }

  Future<void> _toggleBackgroundService(bool start) async {
    if (start) {
      await _requestLocationPermissions();
      // Ensure initialized
      await BgLocationService.initialize();
      final started = await FlutterBackgroundService().startService();
      if (started) {
        setState(() {
          _bgServiceRunning = true;
        });
        _showSuccessSnackBar('Servicio de geolocalización en segundo plano iniciado.');
      } else {
        _showErrorSnackBar('No se pudo iniciar el servicio de geolocalización.');
      }
    } else {
      FlutterBackgroundService().invoke('stopService');
      setState(() {
        _bgServiceRunning = false;
      });
      _showWarningSnackBar('Servicio de geolocalización detenido.');
    }
  }

  void _showErrorSnackBar(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(children: [const Icon(Icons.error_outline, color: Colors.white), const SizedBox(width: 8), Expanded(child: Text(msg))]),
        backgroundColor: AppTheme.error,
      ),
    );
  }

  void _showWarningSnackBar(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(children: [const Icon(Icons.warning_amber_outlined, color: Colors.white), const SizedBox(width: 8), Expanded(child: Text(msg))]),
        backgroundColor: AppTheme.warning,
      ),
    );
  }

  void _showSuccessSnackBar(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(children: [const Icon(Icons.check_circle_outline, color: Colors.white), const SizedBox(width: 8), Expanded(child: Text(msg))]),
        backgroundColor: AppTheme.success,
      ),
    );
  }

  Future<void> _marcarAsistencia(BuildContext context, bool esEntrada) async {
    await _getLiveLocation();
    if (_currentPosition == null) {
      _showErrorSnackBar(_gpsError ?? 'No se pudo obtener la ubicación GPS precisa. Intente de nuevo.');
      return;
    }

    final provider = context.read<AsistenciaProvider>();
    final double lat = _currentPosition!.latitude;
    final double lng = _currentPosition!.longitude;

    final String actionText = esEntrada ? 'registrar entrada' : 'registrar salida';

    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('¿Desea $actionText?'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Se registrará la asistencia con su ubicación GPS actual:'),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.grey[200],
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  const Icon(Icons.gps_fixed, color: AppTheme.primaryDark, size: 18),
                  const SizedBox(width: 8),
                  Text(
                    'Lat: ${lat.toStringAsFixed(6)}, Lng: ${lng.toStringAsFixed(6)}',
                    style: const TextStyle(fontFamily: 'monospace', fontSize: 13),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancelar'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(
              foregroundColor: esEntrada ? AppTheme.success : AppTheme.error,
              textStyle: const TextStyle(fontWeight: FontWeight.bold),
            ),
            child: Text(esEntrada ? 'Marcar Entrada' : 'Marcar Salida'),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      final success = esEntrada 
          ? await provider.marcarEntrada(lat, lng)
          : await provider.marcarSalida(lat, lng);

      if (success) {
        _showSuccessSnackBar(provider.successMessage ?? 'Asistencia registrada con éxito.');
        // If we marked entrance, let's suggest turning on background service
        if (esEntrada && !_bgServiceRunning) {
          _showBackgroundServiceSuggestion();
        }
      } else {
        _showErrorSnackBar(provider.error ?? 'Error al registrar asistencia.');
      }
    }
  }

  void _showBackgroundServiceSuggestion() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Row(
          children: [
            Icon(Icons.gps_fixed, color: AppTheme.accentOrange),
            SizedBox(width: 8),
            Text('Activar Rastreo'),
          ],
        ),
        content: const Text(
          'Ha marcado su entrada con éxito. Para cumplir con las directivas de la empresa, active el servicio de monitoreo GPS continuo en segundo plano.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Más tarde'),
          ),
          TextButton(
            onPressed: () {
              Navigator.pop(ctx);
              _toggleBackgroundService(true);
            },
            style: TextButton.styleFrom(
              foregroundColor: AppTheme.accentOrange,
              textStyle: const TextStyle(fontWeight: FontWeight.bold),
            ),
            child: const Text('Activar Ahora'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final user = context.read<AuthProvider>().user;

    return Consumer<AsistenciaProvider>(
      builder: (context, provider, _) {
        final jornada = provider.jornadaHoy;

        return RefreshIndicator(
          onRefresh: () => provider.loadJornadaHoy(),
          child: SingleChildScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Premium User Welcome and Current Date Card
                _buildHeaderCard(user),
                const SizedBox(height: 16),

                // Live GPS Status Bar
                _buildGPSStatusBar(),
                const SizedBox(height: 16),

                // Primary Punch In/Out Card
                _buildPunchCard(provider, jornada),
                const SizedBox(height: 16),

                // Background Tracking Switch Card
                _buildTrackingCard(),
                const SizedBox(height: 16),

                // Shift Details Table/Card
                if (jornada != null) _buildShiftDetailsCard(jornada),
                const SizedBox(height: 24),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildHeaderCard(dynamic user) {
    final now = DateTime.now();
    final formatter = DateFormat("EEEE, d 'de' MMMM", "es");
    final dateStr = formatter.format(now);

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [AppTheme.primaryDark, AppTheme.primaryLight],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: AppTheme.primaryDark.withValues(alpha: 0.3),
            blurRadius: 8,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            '¡Hola, ${user?.nombres ?? 'Colaborador'}!',
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            user?.rol ?? 'Empleado',
            style: TextStyle(
              fontSize: 14,
              color: Colors.white.withValues(alpha: 0.8),
              fontWeight: FontWeight.w500,
            ),
          ),
          const Divider(color: Colors.white30, height: 24),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Fecha de hoy',
                    style: TextStyle(
                      fontSize: 12,
                      color: Colors.white.withValues(alpha: 0.7),
                    ),
                  ),
                  Text(
                    dateStr,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                      color: Colors.white,
                    ),
                  ),
                ],
              ),
              const Icon(
                Icons.calendar_month,
                color: Colors.white70,
                size: 28,
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildGPSStatusBar() {
    Color statusColor = Colors.grey;
    IconData gpsIcon = Icons.gps_not_fixed;
    String statusText = 'Consultando localización GPS...';

    if (_retrievingGPS) {
      statusColor = AppTheme.warning;
      gpsIcon = Icons.sync;
      statusText = 'Adquiriendo señal de satélite...';
    } else if (_gpsError != null) {
      statusColor = AppTheme.error;
      gpsIcon = Icons.gps_off;
      statusText = _gpsError!;
    } else if (_currentPosition != null) {
      statusColor = AppTheme.success;
      gpsIcon = Icons.gps_fixed;
      statusText = 'GPS Activo (Lat: ${_currentPosition!.latitude.toStringAsFixed(4)}, Lng: ${_currentPosition!.longitude.toStringAsFixed(4)})';
    }

    return Card(
      elevation: 1,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: statusColor.withValues(alpha: 0.3)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            _retrievingGPS
                ? SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: statusColor,
                    ),
                  )
                : Icon(gpsIcon, color: statusColor, size: 22),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                statusText,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                  color: statusColor == Colors.grey ? AppTheme.greyText : statusColor,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
            IconButton(
              icon: const Icon(Icons.refresh, size: 18),
              onPressed: _retrievingGPS ? null : _getLiveLocation,
              constraints: const BoxConstraints(),
              padding: EdgeInsets.zero,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildPunchCard(AsistenciaProvider provider, JornadaCompleta? jornada) {
    final bool active = provider.tieneJornadaActiva;
    final bool finished = provider.jornadaFinalizada;

    String shiftStateText = 'Sin iniciar turno';
    Color stateColor = AppTheme.greyText;
    IconData stateIcon = Icons.access_time;

    if (active) {
      shiftStateText = 'TURNO ACTIVO';
      stateColor = AppTheme.success;
      stateIcon = Icons.play_circle_fill;
    } else if (finished) {
      shiftStateText = 'TURNO FINALIZADO';
      stateColor = AppTheme.primaryDark;
      stateIcon = Icons.check_circle;
    }

    return Card(
      margin: EdgeInsets.zero,
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            // Shift state indicator
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
              decoration: BoxDecoration(
                color: stateColor.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(stateIcon, color: stateColor, size: 18),
                  const SizedBox(width: 8),
                  Text(
                    shiftStateText,
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      color: stateColor,
                      fontSize: 12,
                      letterSpacing: 1.1,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Big Clock Face Display
            StreamBuilder<DateTime>(
              stream: Stream.periodic(const Duration(seconds: 1), (_) => DateTime.now()),
              builder: (context, snapshot) {
                final time = snapshot.data ?? DateTime.now();
                return Text(
                  DateFormat('HH:mm:ss').format(time),
                  style: const TextStyle(
                    fontSize: 42,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'monospace',
                    letterSpacing: 2,
                    color: AppTheme.primaryDark,
                  ),
                );
              },
            ),
            const SizedBox(height: 28),

            // Entrance / Exit buttons
            if (provider.isLoading)
              const Center(
                child: Padding(
                  padding: EdgeInsets.symmetric(vertical: 16.0),
                  child: CircularProgressIndicator(),
                ),
              )
            else if (!active && !finished)
              // Entry Button (GREEN)
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  icon: const Icon(Icons.login, size: 22),
                  label: const Text('MARCAR ENTRADA'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.success,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    elevation: 3,
                  ),
                  onPressed: () => _marcarAsistencia(context, true),
                ),
              )
            else if (active)
              // Exit Button (RED)
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  icon: const Icon(Icons.logout, size: 22),
                  label: const Text('MARCAR SALIDA'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.error,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    elevation: 3,
                  ),
                  onPressed: () => _marcarAsistencia(context, false),
                ),
              )
            else
              // Workday Done Alert message
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: AppTheme.primaryDark.withValues(alpha: 0.05),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: AppTheme.primaryDark.withValues(alpha: 0.15)),
                ),
                child: const Column(
                  children: [
                    Icon(Icons.stars, color: AppTheme.accentOrange, size: 32),
                    SizedBox(height: 8),
                    Text(
                      '¡Jornada de hoy completada!',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                        color: AppTheme.primaryDark,
                      ),
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 4),
                    const Text(
                      'Has completado tus registros de asistencia de hoy correctamente.',
                      style: TextStyle(fontSize: 13, color: AppTheme.greyText),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildTrackingCard() {
    return Card(
      margin: EdgeInsets.zero,
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: (_bgServiceRunning ? AppTheme.success : AppTheme.greyText).withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(
                _bgServiceRunning ? Icons.radar : Icons.radar_outlined,
                color: _bgServiceRunning ? AppTheme.success : AppTheme.greyText,
                size: 26,
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Rastreo GPS en vivo',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 15,
                      color: AppTheme.primaryDark,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    _bgServiceRunning
                        ? 'Servicio activo en segundo plano'
                        : 'Servicio inactivo',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w500,
                      color: _bgServiceRunning ? AppTheme.success : AppTheme.greyText,
                    ),
                  ),
                ],
              ),
            ),
            Switch.adaptive(
              value: _bgServiceRunning,
              activeTrackColor: AppTheme.success,
              onChanged: (val) => _toggleBackgroundService(val),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildShiftDetailsCard(JornadaCompleta jornada) {
    String formatTime(DateTime? date) {
      if (date == null) return '--:--';
      return DateFormat('HH:mm').format(date.toLocal());
    }

    return Card(
      margin: EdgeInsets.zero,
      elevation: 3,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Title Banner
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: AppTheme.primaryDark.withValues(alpha: 0.05),
              borderRadius: const BorderRadius.vertical(top: Radius.circular(16)),
            ),
            child: const Row(
              children: [
                Icon(Icons.assignment, color: AppTheme.primaryDark, size: 20),
                SizedBox(width: 8),
                Text(
                  'Resumen de la Jornada',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 15,
                    color: AppTheme.primaryDark,
                  ),
                ),
              ],
            ),
          ),

          // Detail rows
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                _buildDetailRow('Hora Entrada', formatTime(jornada.horaEntrada), Icons.login, AppTheme.success),
                const Divider(),
                _buildDetailRow('Hora Salida', formatTime(jornada.horaSalida), Icons.logout, AppTheme.error),
                const Divider(),
                _buildDetailRow(
                  'Horas Trabajadas',
                  jornada.totalHoras != null ? '${jornada.totalHoras!.toStringAsFixed(2)} hrs' : '0.0 hrs',
                  Icons.timelapse,
                  AppTheme.primaryLight,
                ),
                const Divider(),
                _buildDetailRow(
                  'Tiempo fuera de Geocerca',
                  '${jornada.tiempoFueraGeocerca} min',
                  Icons.report_problem,
                  jornada.tiempoFueraGeocerca > 0 ? AppTheme.error : AppTheme.greyText,
                ),
                const Divider(),
                _buildDetailRow(
                  'Alertas Generadas',
                  jornada.alertasGeneradas.toString(),
                  Icons.notifications_active,
                  jornada.alertasGeneradas > 0 ? AppTheme.warning : AppTheme.greyText,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDetailRow(String label, String value, IconData icon, Color color) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6.0),
      child: Row(
        children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(width: 12),
          Text(
            label,
            style: const TextStyle(fontSize: 14, color: AppTheme.greyText, fontWeight: FontWeight.w500),
          ),
          const Spacer(),
          Text(
            value,
            style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppTheme.primaryDark),
          ),
        ],
      ),
    );
  }
}
