import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../theme.dart';
import '../../providers/empleados_provider.dart';
import '../../models/empleado.dart';

class LugaresScreen extends StatefulWidget {
  const LugaresScreen({super.key});

  @override
  State<LugaresScreen> createState() => _LugaresScreenState();
}

class _LugaresScreenState extends State<LugaresScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<EmpleadosProvider>().loadLugares();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<EmpleadosProvider>(
      builder: (context, provider, _) {
        final lugares = provider.lugares;

        if (provider.isLoading && lugares.isEmpty) {
          return const Center(child: CircularProgressIndicator());
        }

        return RefreshIndicator(
          onRefresh: () => provider.loadLugares(),
          child: lugares.isEmpty
              ? ListView(
                  children: const [
                    SizedBox(height: 80),
                    Center(
                      child: Column(
                        children: [
                          Icon(Icons.location_off, size: 64, color: Colors.grey),
                          SizedBox(height: 16),
                          Text('No hay lugares registrados',
                              style: TextStyle(color: Colors.grey, fontSize: 16)),
                        ],
                      ),
                    ),
                  ],
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(12),
                  itemCount: lugares.length,
                  itemBuilder: (context, index) {
                    return _LugarCard(lugar: lugares[index]);
                  },
                ),
        );
      },
    );
  }
}

class _LugarCard extends StatelessWidget {
  final LugarTrabajo lugar;

  const _LugarCard({required this.lugar});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: AppTheme.accentOrange.withValues(alpha: 0.15),
          child: const Icon(Icons.location_on, color: AppTheme.accentOrange),
        ),
        title: Text(lugar.nombre,
            style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(lugar.direccion,
                style: const TextStyle(fontSize: 12, color: Colors.grey)),
            const SizedBox(height: 4),
            Row(
              children: [
                Chip(
                  label: Text(
                    '${lugar.totalEmpleados} empleados',
                    style: const TextStyle(fontSize: 10),
                  ),
                  backgroundColor: AppTheme.primaryDark.withValues(alpha: 0.1),
                  side: BorderSide.none,
                  visualDensity: VisualDensity.compact,
                  padding: EdgeInsets.zero,
                ),
                const SizedBox(width: 8),
                Chip(
                  label: Text(
                    lugar.activo ? 'Activo' : 'Inactivo',
                    style: TextStyle(
                      fontSize: 10,
                      color:
                          lugar.activo ? AppTheme.success : AppTheme.error,
                    ),
                  ),
                  backgroundColor: (lugar.activo
                          ? AppTheme.success
                          : AppTheme.error)
                      .withValues(alpha: 0.1),
                  side: BorderSide.none,
                  visualDensity: VisualDensity.compact,
                  padding: EdgeInsets.zero,
                ),
              ],
            ),
          ],
        ),
        isThreeLine: true,
        trailing: const Icon(Icons.chevron_right, color: Colors.grey),
        onTap: () => _showLugarDetail(context, lugar),
      ),
    );
  }
}

void _showLugarDetail(BuildContext context, LugarTrabajo lugar) {
  showModalBottomSheet(
    context: context,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (ctx) => Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Center(
            child: Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: Colors.grey[300],
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              const Icon(Icons.location_on, color: AppTheme.accentOrange, size: 28),
              const SizedBox(width: 12),
              Expanded(
                child: Text(lugar.nombre,
                    style: const TextStyle(
                        fontSize: 20, fontWeight: FontWeight.bold, color: AppTheme.primaryDark)),
              ),
            ],
          ),
          const Divider(),
          _DetailRow(icon: Icons.location_on, label: 'Dirección', value: lugar.direccion),
          if (lugar.descripcion != null && lugar.descripcion!.isNotEmpty)
            _DetailRow(icon: Icons.description, label: 'Descripción', value: lugar.descripcion!),
          _DetailRow(icon: Icons.people, label: 'Total Empleados', value: '${lugar.totalEmpleados}'),
          const SizedBox(height: 8),
          const SizedBox(height: 16),
        ],
      ),
    ),
  );
}

class _DetailRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _DetailRow({required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: Colors.grey[600]),
          const SizedBox(width: 12),
          SizedBox(
            width: 100,
            child: Text(label,
                style: const TextStyle(color: Colors.grey, fontSize: 14)),
          ),
          Expanded(
            child: Text(value,
                style: const TextStyle(fontWeight: FontWeight.w500, fontSize: 14)),
          ),
        ],
      ),
    );
  }
}
