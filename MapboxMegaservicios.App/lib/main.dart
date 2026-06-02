import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import 'providers/auth_provider.dart';
import 'providers/empleados_provider.dart';
import 'providers/ubicaciones_provider.dart';
import 'providers/asistencia_provider.dart';
import 'services/bg_location_service.dart';
import 'theme.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  Intl.defaultLocale = 'es';
  try {
    await BgLocationService.initialize();
  } catch (_) {
    // Background service initialization failed (e.g. missing plugins on first install).
    // The user can manually enable it from the Asistencia screen later.
  }

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProvider(create: (_) => EmpleadosProvider()),
        ChangeNotifierProvider(create: (_) => UbicacionesProvider()),
        ChangeNotifierProvider(create: (_) => AsistenciaProvider()),
      ],
      child: const SGEMobileApp(),
    ),
  );
}

class SGEMobileApp extends StatelessWidget {
  const SGEMobileApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'SGE - MegaServicios',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        DefaultMaterialLocalizations.delegate,
      ],
      supportedLocales: const [
        Locale('es', 'BO'),
        Locale('es'),
        Locale('en'),
      ],
      locale: const Locale('es'),
      home: const AuthGate(),
    );
  }
}

class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  bool _checking = true;

  @override
  void initState() {
    super.initState();
    _checkAuth();
  }

  Future<void> _checkAuth() async {
    await context.read<AuthProvider>().checkAuth();
    if (mounted) setState(() => _checking = false);
  }

  @override
  Widget build(BuildContext context) {
    if (_checking) {
      return const Scaffold(
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              CircularProgressIndicator(color: AppTheme.accentOrange),
              SizedBox(height: 16),
              Text('Cargando...', style: TextStyle(color: AppTheme.primaryDark)),
            ],
          ),
        ),
      );
    }

    return Consumer<AuthProvider>(
      builder: (context, auth, _) {
        if (auth.isLoggedIn) {
          return const HomeScreen();
        }
        return const LoginScreen();
      },
    );
  }
}
