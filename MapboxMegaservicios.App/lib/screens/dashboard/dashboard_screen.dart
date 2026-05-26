import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../theme.dart';
import '../../providers/empleados_provider.dart';
import '../../models/empleado.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<EmpleadosProvider>().loadDashboard();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<EmpleadosProvider>(
      builder: (context, provider, _) {
        if (provider.isLoading && provider.stats.totalEmpleados == 0) {
          return const Center(child: CircularProgressIndicator());
        }

        final stats = provider.stats;

        return RefreshIndicator(
          onRefresh: () => provider.loadDashboard(),
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              // Stats cards
              _buildStatsGrid(stats),
              const SizedBox(height: 16),

              // Alertas recientes
              _buildAlertasSection(stats.ultimasAlertas),
            ],
          ),
        );
      },
    );
  }

  Widget _buildStatsGrid(DashboardStats stats) {
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 1.6,
      children: [
        _StatCard(
          title: 'Empleados',
          value: stats.totalEmpleados.toString(),
          color: AppTheme.primaryDark,
          icon: Icons.people,
        ),
        _StatCard(
          title: 'En Geocerca',
          value: stats.empleadosEnGeocerca.toString(),
          color: AppTheme.success,
          icon: Icons.check_circle_outline,
        ),
        _StatCard(
          title: 'Fuera Geocerca',
          value: stats.empleadosFueraGeocerca.toString(),
          color: AppTheme.error,
          icon: Icons.warning_amber,
        ),
        _StatCard(
          title: 'Alertas Hoy',
          value: stats.alertasHoy.toString(),
          color: AppTheme.warning,
          icon: Icons.notifications_active,
        ),
      ],
    );
  }

  Widget _buildAlertasSection(List<Alerta> alertas) {
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.notifications, color: AppTheme.primaryDark),
                const SizedBox(width: 8),
                const Text(
                  'Últimas Alertas',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.primaryDark,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            if (alertas.isEmpty)
              const Padding(
                padding: EdgeInsets.all(24),
                child: Center(
                  child: Text(
                    'No hay alertas recientes',
                    style: TextStyle(color: Colors.grey),
                  ),
                ),
              )
            else
              ...alertas.map((alerta) => _AlertaTile(alerta: alerta)),
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String title;
  final String value;
  final Color color;
  final IconData icon;

  const _StatCard({
    required this.title,
    required this.value,
    required this.color,
    required this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.zero,
      elevation: 3,
      child: Container(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(12),
          color: color.withValues(alpha: 0.1),
        ),
        padding: const EdgeInsets.all(12),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: color, size: 28),
            const SizedBox(height: 4),
            Text(
              value,
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.bold,
                color: color,
              ),
            ),
            Text(
              title,
              style: TextStyle(
                fontSize: 12,
                color: color.withValues(alpha: 0.8),
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _AlertaTile extends StatelessWidget {
  final Alerta alerta;

  const _AlertaTile({required this.alerta});

  @override
  Widget build(BuildContext context) {
    final isDentro = alerta.alerta.toLowerCase().contains('dentro');
    final color = isDentro ? AppTheme.success : AppTheme.error;

    String formattedDate = alerta.fechaHora;
    try {
      final date = DateTime.parse(alerta.fechaHora);
      formattedDate = DateFormat('dd/MM/yy HH:mm').format(date);
    } catch (_) {}

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      elevation: 1,
      child: ListTile(
        dense: true,
        leading: CircleAvatar(
          backgroundColor: color.withValues(alpha: 0.15),
          child: Icon(
            isDentro ? Icons.check_circle : Icons.error,
            color: color,
            size: 20,
          ),
        ),
        title: Text(alerta.empleado,
            style: const TextStyle(fontWeight: FontWeight.w500, fontSize: 14)),
        subtitle: Text(
          '${alerta.alerta} · $formattedDate',
          style: TextStyle(fontSize: 12, color: Colors.grey[600]),
        ),
        trailing: alerta.lugar != null
            ? Text(alerta.lugar!,
                style: const TextStyle(fontSize: 11, color: Colors.grey))
            : null,
      ),
    );
  }
}
