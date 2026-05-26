import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../theme.dart';
import '../../providers/empleados_provider.dart';
import '../../models/empleado.dart';
import 'empleado_form_dialog.dart';

class EmpleadosScreen extends StatefulWidget {
  const EmpleadosScreen({super.key});

  @override
  State<EmpleadosScreen> createState() => _EmpleadosScreenState();
}

class _EmpleadosScreenState extends State<EmpleadosScreen> {
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<EmpleadosProvider>();
      provider.loadEmpleados();
      provider.loadLugares();
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<EmpleadosProvider>(
      builder: (context, provider, _) {
        return Column(
          children: [
            // Header con búsqueda
            Container(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _searchController,
                      decoration: InputDecoration(
                        hintText: 'Buscar empleado...',
                        prefixIcon: const Icon(Icons.search),
                        filled: true,
                        fillColor: Colors.white,
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(8),
                          borderSide: BorderSide.none,
                        ),
                        contentPadding: const EdgeInsets.symmetric(vertical: 0),
                      ),
                      onChanged: (v) => provider.setSearchQuery(v),
                    ),
                  ),
                  const SizedBox(width: 12),
                  FloatingActionButton.small(
                    onPressed: () => _showFormDialog(context),
                    backgroundColor: AppTheme.accentOrange,
                    child: const Icon(Icons.add, color: Colors.white),
                  ),
                ],
              ),
            ),

            // Lista
            Expanded(
              child: provider.isLoading && provider.empleados.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : provider.empleadosFiltrados.isEmpty
                      ? const Center(
                          child: Text('No se encontraron empleados',
                              style: TextStyle(color: Colors.grey)),
                        )
                      : RefreshIndicator(
                          onRefresh: () => provider.loadEmpleados(),
                          child: ListView.builder(
                            padding: const EdgeInsets.symmetric(horizontal: 12),
                            itemCount: provider.empleadosFiltrados.length,
                            itemBuilder: (context, index) {
                              return _EmpleadoCard(
                                empleado: provider.empleadosFiltrados[index],
                                onEdit: () => _showFormDialog(
                                  context,
                                  empleado: provider.empleadosFiltrados[index],
                                ),
                                onToggleActivo: () =>
                                    _toggleActivo(context, provider.empleadosFiltrados[index]),
                                onCambiarLugar: () => _showLugarDialog(
                                  context,
                                  provider.empleadosFiltrados[index],
                                ),
                              );
                            },
                          ),
                        ),
            ),
          ],
        );
      },
    );
  }

  void _showFormDialog(BuildContext context, {Empleado? empleado}) {
    showDialog(
      context: context,
      builder: (ctx) => EmpleadoFormDialog(
        empleado: empleado,
        onSave: () async {
          Navigator.pop(ctx);
        },
      ),
    );
  }

  Future<void> _toggleActivo(
      BuildContext context, Empleado empleado) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(empleado.activo ? 'Desactivar Empleado' : 'Activar Empleado'),
        content: Text(
            '¿${empleado.activo ? 'Desactivar' : 'Activar'} a ${empleado.nombreDisplay}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancelar'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: TextButton.styleFrom(
              foregroundColor: empleado.activo ? AppTheme.error : AppTheme.success,
            ),
            child: Text(empleado.activo ? 'Desactivar' : 'Activar'),
          ),
        ],
      ),
    );

    if (confirm == true && context.mounted) {
      await context.read<EmpleadosProvider>().toggleActivo(empleado.id);
    }
  }

  void _showLugarDialog(BuildContext context, Empleado empleado) {
    final lugares =
        context.read<EmpleadosProvider>().lugares;
    int? selectedId = empleado.idLugarTrabajo;
    final observacionesController = TextEditingController();

    showDialog(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          title: const Text('Cambiar Lugar de Trabajo'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('Empleado: ${empleado.nombreDisplay}',
                  style: const TextStyle(fontWeight: FontWeight.w500)),
              const SizedBox(height: 16),
              DropdownButtonFormField<int?>(
                initialValue: selectedId,
                decoration: const InputDecoration(
                  labelText: 'Seleccionar Lugar',
                  border: OutlineInputBorder(),
                ),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Sin asignar'),
                  ),
                  ...lugares.map((l) => DropdownMenuItem(
                        value: l.id,
                        child: Text(l.nombre),
                      )),
                ],
                onChanged: (v) => setDialogState(() => selectedId = v),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: observacionesController,
                decoration: const InputDecoration(
                  labelText: 'Observaciones (opcional)',
                  border: OutlineInputBorder(),
                ),
                maxLines: 2,
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Cancelar'),
            ),
            ElevatedButton(
              onPressed: () async {
                final provider = context.read<EmpleadosProvider>();
                final success = await provider.cambiarLugar(
                  empleado.id,
                  selectedId,
                  observaciones: observacionesController.text,
                );
                if (ctx.mounted) Navigator.pop(ctx);
                if (success && context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Lugar actualizado'),
                      backgroundColor: AppTheme.success,
                    ),
                  );
                }
              },
              child: const Text('Guardar'),
            ),
          ],
        ),
      ),
    );
  }
}

class _EmpleadoCard extends StatelessWidget {
  final Empleado empleado;
  final VoidCallback onEdit;
  final VoidCallback onToggleActivo;
  final VoidCallback onCambiarLugar;

  const _EmpleadoCard({
    required this.empleado,
    required this.onEdit,
    required this.onToggleActivo,
    required this.onCambiarLugar,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4),
      child: Opacity(
        opacity: empleado.activo ? 1.0 : 0.5,
        child: ExpansionTile(
          leading: CircleAvatar(
            backgroundColor: AppTheme.primaryDark.withValues(alpha: 0.15),
            child: Text(
              '${empleado.nombres.isNotEmpty ? empleado.nombres[0] : '?'}${empleado.paterno.isNotEmpty ? empleado.paterno[0] : ''}',
              style: const TextStyle(
                  color: AppTheme.primaryDark, fontWeight: FontWeight.bold),
            ),
          ),
          title: Text(empleado.nombreDisplay,
              style: const TextStyle(fontWeight: FontWeight.w600)),
          subtitle: Text('${empleado.ci} · ${empleado.usuario ?? ''}',
              style: const TextStyle(fontSize: 12)),
          children: [
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Column(
                children: [
                  _InfoRow(
                      icon: Icons.badge_outlined,
                      label: 'CI',
                      value: empleado.ci),
                  _InfoRow(
                      icon: Icons.phone_outlined,
                      label: 'Teléfono',
                      value: empleado.telefono ?? '—'),
                  _InfoRow(
                      icon: Icons.work_outline,
                      label: 'Rol',
                      value: empleado.rol ?? 'Empleado'),
                  _InfoRow(
                      icon: Icons.location_on_outlined,
                      label: 'Lugar',
                      value: empleado.lugarActual ?? 'Sin asignar'),
                  Row(
                    children: [
                      const Icon(Icons.circle, size: 12, color: Colors.grey),
                      const SizedBox(width: 8),
                      Chip(
                        label: Text(
                          empleado.activo ? 'Activo' : 'Inactivo',
                          style: TextStyle(
                            fontSize: 12,
                            color: empleado.activo
                                ? AppTheme.success
                                : AppTheme.error,
                          ),
                        ),
                        backgroundColor: (empleado.activo
                                ? AppTheme.success
                                : AppTheme.error)
                            .withValues(alpha: 0.1),
                        side: BorderSide.none,
                        visualDensity: VisualDensity.compact,
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      IconButton(
                        icon: const Icon(Icons.edit_outlined, size: 20),
                        onPressed: empleado.activo ? onEdit : null,
                        color: AppTheme.primaryDark,
                        tooltip: 'Editar',
                      ),
                      IconButton(
                        icon: const Icon(Icons.location_on_outlined, size: 20),
                        onPressed:
                            empleado.activo ? onCambiarLugar : null,
                        color: AppTheme.primaryLight,
                        tooltip: 'Cambiar lugar',
                      ),
                      IconButton(
                        icon: Icon(
                          empleado.activo
                              ? Icons.person_off_outlined
                              : Icons.person_outline,
                          size: 20,
                        ),
                        onPressed: onToggleActivo,
                        color: empleado.activo
                            ? AppTheme.error
                            : AppTheme.success,
                        tooltip:
                            empleado.activo ? 'Desactivar' : 'Activar',
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Icon(icon, size: 16, color: Colors.grey),
          const SizedBox(width: 8),
          Text('$label: ',
              style: const TextStyle(color: Colors.grey, fontSize: 13)),
          Expanded(
            child: Text(value,
                style: const TextStyle(fontWeight: FontWeight.w500, fontSize: 13)),
          ),
        ],
      ),
    );
  }
}
