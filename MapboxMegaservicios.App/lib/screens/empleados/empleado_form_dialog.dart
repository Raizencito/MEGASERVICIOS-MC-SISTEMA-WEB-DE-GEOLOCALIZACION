import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../theme.dart';
import '../../models/empleado.dart';
import '../../providers/empleados_provider.dart';

class EmpleadoFormDialog extends StatefulWidget {
  final Empleado? empleado;
  final VoidCallback onSave;

  const EmpleadoFormDialog({
    super.key,
    this.empleado,
    required this.onSave,
  });

  @override
  State<EmpleadoFormDialog> createState() => _EmpleadoFormDialogState();
}

class _EmpleadoFormDialogState extends State<EmpleadoFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _paternoController;
  late TextEditingController _maternoController;
  late TextEditingController _nombresController;
  late TextEditingController _ciController;
  late TextEditingController _telefonoController;
  int? _selectedLugarId;
  bool _saving = false;

  bool get _editMode => widget.empleado != null;

  @override
  void initState() {
    super.initState();
    _paternoController = TextEditingController(text: widget.empleado?.paterno ?? '');
    _maternoController = TextEditingController(text: widget.empleado?.materno ?? '');
    _nombresController = TextEditingController(text: widget.empleado?.nombres ?? '');
    _ciController = TextEditingController(text: widget.empleado?.ci ?? '');
    _telefonoController = TextEditingController(text: widget.empleado?.telefono ?? '');
    _selectedLugarId = widget.empleado?.idLugarTrabajo;
  }

  @override
  void dispose() {
    _paternoController.dispose();
    _maternoController.dispose();
    _nombresController.dispose();
    _ciController.dispose();
    _telefonoController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _saving = true);

    final data = {
      'paterno': _paternoController.text.trim(),
      'materno': _maternoController.text.trim(),
      'nombres': _nombresController.text.trim(),
      'telefono': _telefonoController.text.trim(),
    };

    final provider = context.read<EmpleadosProvider>();
    bool success;

    if (_editMode) {
      success = await provider.updateEmpleado(widget.empleado!.id, data);
    } else {
      data.addAll({
        'ci': _ciController.text.trim(),
        'idRol': '2',
      });
      if (_selectedLugarId != null) {
        data['idLugarTrabajo'] = _selectedLugarId.toString();
      }
      success = await provider.createEmpleado(data);
    }

    if (mounted) {
      setState(() => _saving = false);
      if (success) {
        widget.onSave();
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(_editMode ? 'Empleado actualizado' : 'Empleado creado'),
            backgroundColor: AppTheme.success,
          ),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(provider.error ?? 'Error al guardar'),
            backgroundColor: AppTheme.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final lugares = context.watch<EmpleadosProvider>().lugares;

    return Dialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _editMode ? 'Editar Empleado' : 'Nuevo Empleado',
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                    color: AppTheme.primaryDark,
                  ),
                ),
                const Divider(),
                const SizedBox(height: 8),

                // Apellido Paterno
                TextFormField(
                  controller: _paternoController,
                  decoration: const InputDecoration(
                    labelText: 'Apellido Paterno *',
                  ),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'Requerido' : null,
                ),
                const SizedBox(height: 12),

                // Apellido Materno
                TextFormField(
                  controller: _maternoController,
                  decoration: const InputDecoration(
                    labelText: 'Apellido Materno',
                  ),
                ),
                const SizedBox(height: 12),

                // Nombres
                TextFormField(
                  controller: _nombresController,
                  decoration: const InputDecoration(
                    labelText: 'Nombres *',
                  ),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'Requerido' : null,
                ),
                const SizedBox(height: 12),

                // CI
                TextFormField(
                  controller: _ciController,
                  decoration: InputDecoration(
                    labelText: 'CI *',
                    enabled: !_editMode,
                  ),
                  validator: (v) =>
                      v == null || v.trim().isEmpty ? 'Requerido' : null,
                ),
                const SizedBox(height: 12),

                // Teléfono
                TextFormField(
                  controller: _telefonoController,
                  decoration: const InputDecoration(
                    labelText: 'Teléfono',
                  ),
                ),
                const SizedBox(height: 12),

                // Lugar de trabajo (solo en creación)
                if (!_editMode) ...[
                  DropdownButtonFormField<int?>(
                    initialValue: _selectedLugarId,
                    decoration: const InputDecoration(
                      labelText: 'Lugar de Trabajo',
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
                    onChanged: (v) => setState(() => _selectedLugarId = v),
                  ),
                  const SizedBox(height: 16),
                ],

                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(
                      onPressed: () => Navigator.pop(context),
                      child: const Text('Cancelar'),
                    ),
                    const SizedBox(width: 12),
                    ElevatedButton(
                      onPressed: _saving ? null : _save,
                      child: _saving
                          ? const SizedBox(
                              width: 20,
                              height: 20,
                              child: CircularProgressIndicator(
                                  strokeWidth: 2, color: Colors.white),
                            )
                          : Text(_editMode ? 'Actualizar' : 'Crear'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
