import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../theme.dart';
import '../../providers/empleados_provider.dart';
import '../../services/api_service.dart';


class ReportesScreen extends StatefulWidget {
  const ReportesScreen({super.key});

  @override
  State<ReportesScreen> createState() => _ReportesScreenState();
}

class _ReportesScreenState extends State<ReportesScreen> {
  String _tipoReporte = 'general';
  DateTime _desde = DateTime.now();
  DateTime _hasta = DateTime.now();
  bool _generando = false;
  String _resultadoTexto = '';

  Future<void> _generarReporte() async {
    setState(() {
      _generando = true;
      _resultadoTexto = '';
    });

    try {
      final data = await apiService.get(
          '/admin/reportes/asistencia?desde=${DateFormat('yyyy-MM-dd').format(_desde)}&hasta=${DateFormat('yyyy-MM-dd').format(_hasta)}');

      if (data is List) {
        final buffer = StringBuffer();
        buffer.writeln('=== REPORTE DE ASISTENCIA ===');
        buffer.writeln(
            'Período: ${DateFormat('dd/MM/yyyy').format(_desde)} - ${DateFormat('dd/MM/yyyy').format(_hasta)}');
        buffer.writeln('Total registros: ${data.length}');
        buffer.writeln('');

        int completados = 0;
        int pendientes = 0;
        for (var item in data) {
          final estado = item['estado'] ?? item['Estado'] ?? 'Sin estado';
          final empleado =
              item['empleadoNombre'] ?? item['EmpleadoNombre'] ?? 'Desconocido';
          buffer.writeln('• $empleado: $estado');
          if (estado.toString().toLowerCase().contains('comple') ||
              estado.toString().toLowerCase().contains('entrada')) {
            completados++;
          } else {
            pendientes++;
          }
        }

        buffer.writeln('');
        buffer.writeln('Completados: $completados');
        buffer.writeln('Pendientes: $pendientes');

        setState(() => _resultadoTexto = buffer.toString());
      } else if (data is Map) {
        setState(() => _resultadoTexto = 'Reporte generado: ${data.toString()}');
      } else {
        setState(() => _resultadoTexto = 'No se encontraron datos para este período.');
      }
    } on ApiException catch (e) {
      setState(() => _resultadoTexto = 'Error: ${e.message}');
    } catch (e) {
      setState(() => _resultadoTexto = 'Error de conexión: $e');
    } finally {
      setState(() => _generando = false);
    }
  }

  Future<void> _selectDate(BuildContext context, bool esDesde) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: esDesde ? _desde : _hasta,
      firstDate: DateTime(2024),
      lastDate: DateTime(2030),
      locale: const Locale('es'),
    );
    if (picked != null) {
      setState(() {
        if (esDesde) {
          _desde = picked;
        } else {
          _hasta = picked;
        }
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    context.watch<EmpleadosProvider>();

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Header
        const Text(
          'Centro de Reportes',
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.bold,
            color: AppTheme.primaryDark,
          ),
        ),
        const SizedBox(height: 4),
        const Text(
          'Generación de informes de asistencia',
          style: TextStyle(color: Colors.grey),
        ),
        const SizedBox(height: 16),

        // Card de filtros
        Card(
          margin: EdgeInsets.zero,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Tipo de Reporte',
                    style: TextStyle(fontWeight: FontWeight.w600)),
                const SizedBox(height: 8),

                // Tipo selector
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(value: 'general', label: Text('General')),
                    ButtonSegment(
                        value: 'individual', label: Text('Individual')),
                    ButtonSegment(
                        value: 'descuentos', label: Text('Descuentos')),
                  ],
                  selected: {_tipoReporte},
                  onSelectionChanged: (v) =>
                      setState(() => _tipoReporte = v.first),
                  style: ButtonStyle(
                    backgroundColor: WidgetStateProperty.resolveWith((states) {
                      if (states.contains(WidgetState.selected)) {
                        return AppTheme.primaryDark;
                      }
                      return Colors.grey[200];
                    }),
                    foregroundColor: WidgetStateProperty.resolveWith((states) {
                      if (states.contains(WidgetState.selected)) {
                        return Colors.white;
                      }
                      return Colors.grey[700];
                    }),
                  ),
                ),
                const SizedBox(height: 16),

                // Fechas
                Row(
                  children: [
                    Expanded(
                      child: _DateField(
                        label: 'Desde',
                        date: _desde,
                        onTap: () => _selectDate(context, true),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _DateField(
                        label: 'Hasta',
                        date: _hasta,
                        onTap: () => _selectDate(context, false),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),

                // Botón generar
                SizedBox(
                  width: double.infinity,
                  child: ElevatedButton.icon(
                    onPressed: _generando ? null : _generarReporte,
                    icon: _generando
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                                strokeWidth: 2, color: Colors.white),
                          )
                        : const Icon(Icons.description),
                    label: Text(
                        _generando ? 'Generando...' : 'Generar Reporte'),
                  ),
                ),
              ],
            ),
          ),
        ),

        // Resultado
        if (_resultadoTexto.isNotEmpty) ...[
          const SizedBox(height: 16),
          Card(
            margin: EdgeInsets.zero,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.article, color: AppTheme.primaryDark),
                      const SizedBox(width: 8),
                      const Text(
                        'Resultado',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                          color: AppTheme.primaryDark,
                        ),
                      ),
                      const Spacer(),
                      IconButton(
                        icon: const Icon(Icons.close, size: 20),
                        onPressed: () => setState(() => _resultadoTexto = ''),
                      ),
                    ],
                  ),
                  const Divider(),
                  SelectableText(
                    _resultadoTexto,
                    style: const TextStyle(
                      fontFamily: 'monospace',
                      fontSize: 12,
                      height: 1.5,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ],
    );
  }
}

class _DateField extends StatelessWidget {
  final String label;
  final DateTime date;
  final VoidCallback onTap;

  const _DateField({
    required this.label,
    required this.date,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          prefixIcon: const Icon(Icons.calendar_today, size: 18),
          border: const OutlineInputBorder(),
        ),
        child: Text(
          DateFormat('dd/MM/yyyy').format(date),
          style: const TextStyle(fontWeight: FontWeight.w500),
        ),
      ),
    );
  }
}
