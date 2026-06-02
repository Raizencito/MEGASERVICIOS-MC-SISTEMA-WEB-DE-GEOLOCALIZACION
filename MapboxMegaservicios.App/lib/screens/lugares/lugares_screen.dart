import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:mapbox_maps_flutter/mapbox_maps_flutter.dart';
import 'package:geolocator/geolocator.dart' as geo;
import '../../theme.dart';
import '../../providers/ubicaciones_provider.dart';
import '../../models/lugar_geocerca.dart';
import '../../config/app_config.dart';

class LugaresScreen extends StatefulWidget {
  const LugaresScreen({super.key});

  @override
  State<LugaresScreen> createState() => _LugaresScreenState();
}

class _LugaresScreenState extends State<LugaresScreen> {
  bool _isMapView = true;
  MapboxMap? _mapboxMap;
  PolygonAnnotationManager? _polygonAnnotationManager;
  PointAnnotationManager? _pointAnnotationManager;
  bool _mapLoading = true;
  LugarConGeocerca? _selectedLugar;
  String _searchQuery = '';
  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    MapboxOptions.setAccessToken(AppConfig.mapboxAccessToken);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<UbicacionesProvider>().loadLugaresConGeocercas();
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _onMapCreated(MapboxMap mapboxMap) async {
    _mapboxMap = mapboxMap;
    _polygonAnnotationManager =
        await mapboxMap.annotations.createPolygonAnnotationManager();
    _pointAnnotationManager =
        await mapboxMap.annotations.createPointAnnotationManager();

    if (mounted) {
      setState(() {
        _mapLoading = false;
      });
      _drawGeocercas();
    }
  }

  Future<void> _drawGeocercas() async {
    if (_mapboxMap == null ||
        _polygonAnnotationManager == null ||
        _pointAnnotationManager == null) return;

    // Clear existing annotations
    await _polygonAnnotationManager!.deleteAll();
    await _pointAnnotationManager!.deleteAll();

    final lugares = context.read<UbicacionesProvider>().lugaresConGeocerca;

    for (final lugar in lugares) {
      if (!lugar.tieneGeocerca) continue;

      // 1. Draw Polygon Ring
      final ring = lugar.coordenadas.map((c) => Position(c[0], c[1])).toList();
      final polygonGeometry = Polygon(coordinates: [ring]);

      final polygonOptions = PolygonAnnotationOptions(
        geometry: polygonGeometry,
        fillColor: AppTheme.primaryDark.toARGB32(),
        fillOpacity: 0.2,
        fillOutlineColor: AppTheme.accentOrange.toARGB32(),
      );
      await _polygonAnnotationManager!.create(polygonOptions);

      // 2. Draw Center Point with Text
      if (lugar.tieneCentro) {
        final pointGeometry = Point(
            coordinates: Position(lugar.centroLongitud!, lugar.centroLatitud!));
        final pointOptions = PointAnnotationOptions(
          geometry: pointGeometry,
          textField: lugar.nombre,
          textColor: AppTheme.primaryDark.toARGB32(),
          textSize: 10,
          textOffset: [0.0, 1.2],
        );
        await _pointAnnotationManager!.create(pointOptions);
      }
    }

    // Centering the camera
    _centerCameraOnDefault();
  }

  void _centerCameraOnDefault() {
    if (_mapboxMap == null) return;
    final lugares = context.read<UbicacionesProvider>().lugaresConGeocerca;
    if (lugares.isNotEmpty) {
      final firstWithCenter =
          lugares.firstWhere((l) => l.tieneCentro, orElse: () => lugares.first);
      if (firstWithCenter.tieneCentro) {
        _mapboxMap!.setCamera(CameraOptions(
          center: Point(
              coordinates: Position(firstWithCenter.centroLongitud!,
                  firstWithCenter.centroLatitud!)),
          zoom: 14.5,
        ));
      }
    }
  }

  Future<void> _focusLugarOnMap(LugarConGeocerca lugar) async {
    if (!lugar.tieneCentro || _mapboxMap == null) return;

    setState(() {
      _selectedLugar = lugar;
      _isMapView = true;
    });

    _mapboxMap!.setCamera(CameraOptions(
      center: Point(
          coordinates:
              Position(lugar.centroLongitud!, lugar.centroLatitud!)),
      zoom: 16.0,
    ));
  }

  Future<void> _centerOnUserLocation() async {
    if (_mapboxMap == null) return;

    try {
      final isEnabled = await geo.Geolocator.isLocationServiceEnabled();
      if (!isEnabled) return;

      final permission = await geo.Geolocator.checkPermission();
      if (permission == geo.LocationPermission.denied ||
          permission == geo.LocationPermission.deniedForever) {
        return;
      }

      final pos = await geo.Geolocator.getCurrentPosition(
        locationSettings: const geo.LocationSettings(
          accuracy: geo.LocationAccuracy.high,
        ),
      );

      _mapboxMap!.setCamera(CameraOptions(
        center: Point(coordinates: Position(pos.longitude, pos.latitude)),
        zoom: 15.5,
      ));

      _mapboxMap!.location.updateSettings(LocationComponentSettings(
        enabled: true,
        pulsingEnabled: true,
      ));
    } catch (_) {}
  }

  List<LugarConGeocerca> _filterLugares(List<LugarConGeocerca> list) {
    if (_searchQuery.isEmpty) return list;
    return list
        .where((l) =>
            l.nombre.toLowerCase().contains(_searchQuery.toLowerCase()) ||
            l.direccion.toLowerCase().contains(_searchQuery.toLowerCase()))
        .toList();
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<UbicacionesProvider>(
      builder: (context, provider, _) {
        final filteredList = _filterLugares(provider.lugares);

        return Scaffold(
          body: Column(
            children: [
              // Search and View Toggle Header
              _buildHeaderActions(provider.isLoading),

              // Main View Area (Map or List)
              Expanded(
                child: _isMapView
                    ? _buildMapBody(provider.isLoading, provider.lugares)
                    : _buildListBody(provider.isLoading, filteredList),
              ),
            ],
          ),
          floatingActionButton: _isMapView && !_mapLoading
              ? Column(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    FloatingActionButton.small(
                      heroTag: 'zoom_in',
                      backgroundColor: Colors.white,
                      foregroundColor: AppTheme.primaryDark,
                      child: const Icon(Icons.add),
                      onPressed: () {
                        _mapboxMap?.getCameraState().then((state) {
                          _mapboxMap?.setCamera(CameraOptions(
                            zoom: (state.zoom + 1.0),
                          ));
                        });
                      },
                    ),
                    const SizedBox(height: 8),
                    FloatingActionButton.small(
                      heroTag: 'zoom_out',
                      backgroundColor: Colors.white,
                      foregroundColor: AppTheme.primaryDark,
                      child: const Icon(Icons.remove),
                      onPressed: () {
                        _mapboxMap?.getCameraState().then((state) {
                          _mapboxMap?.setCamera(CameraOptions(
                            zoom: (state.zoom - 1.0),
                          ));
                        });
                      },
                    ),
                    const SizedBox(height: 8),
                    FloatingActionButton(
                      heroTag: 'user_location',
                      backgroundColor: AppTheme.accentOrange,
                      foregroundColor: Colors.white,
                      child: const Icon(Icons.my_location),
                      onPressed: _centerOnUserLocation,
                    ),
                  ],
                )
              : null,
        );
      },
    );
  }

  Widget _buildHeaderActions(bool isLoading) {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
      child: Row(
        children: [
          // Search Field
          Expanded(
            child: SizedBox(
              height: 44,
              child: TextField(
                controller: _searchController,
                onChanged: (val) {
                  setState(() {
                    _searchQuery = val;
                  });
                },
                decoration: InputDecoration(
                  hintText: 'Buscar lugar...',
                  prefixIcon: const Icon(Icons.search, size: 20),
                  suffixIcon: _searchQuery.isNotEmpty
                      ? IconButton(
                          icon: const Icon(Icons.clear, size: 18),
                          onPressed: () {
                            _searchController.clear();
                            setState(() {
                              _searchQuery = '';
                            });
                          },
                        )
                      : null,
                  filled: true,
                  fillColor: AppTheme.bgLight,
                  contentPadding: EdgeInsets.zero,
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(24),
                    borderSide: BorderSide.none,
                  ),
                ),
              ),
            ),
          ),
          const SizedBox(width: 12),

          // View Toggle Buttons (Premium pills)
          Container(
            height: 44,
            decoration: BoxDecoration(
              color: AppTheme.bgLight,
              borderRadius: BorderRadius.circular(24),
            ),
            padding: const EdgeInsets.all(4),
            child: Row(
              children: [
                GestureDetector(
                  onTap: () {
                    setState(() {
                      _isMapView = true;
                    });
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                    decoration: BoxDecoration(
                      color: _isMapView ? Colors.white : Colors.transparent,
                      borderRadius: BorderRadius.circular(20),
                      boxShadow: _isMapView
                          ? [
                              BoxShadow(
                                color: Colors.black.withValues(alpha: 0.1),
                                blurRadius: 4,
                                offset: const Offset(0, 2),
                              )
                            ]
                          : null,
                    ),
                    child: Icon(
                      Icons.map,
                      size: 20,
                      color: _isMapView ? AppTheme.accentOrange : AppTheme.greyText,
                    ),
                  ),
                ),
                GestureDetector(
                  onTap: () {
                    setState(() {
                      _isMapView = false;
                    });
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                    decoration: BoxDecoration(
                      color: !_isMapView ? Colors.white : Colors.transparent,
                      borderRadius: BorderRadius.circular(20),
                      boxShadow: !_isMapView
                          ? [
                              BoxShadow(
                                color: Colors.black.withValues(alpha: 0.1),
                                blurRadius: 4,
                                offset: const Offset(0, 2),
                              )
                            ]
                          : null,
                    ),
                    child: Icon(
                      Icons.list,
                      size: 20,
                      color: !_isMapView ? AppTheme.accentOrange : AppTheme.greyText,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMapBody(bool isLoading, List<LugarConGeocerca> lugares) {
    if (isLoading && lugares.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    return Stack(
      children: [
        // MapWidget
        MapWidget(
          key: const ValueKey("mapboxMap"),
          onMapCreated: _onMapCreated,
        ),

        if (_mapLoading)
          Container(
            color: Colors.white,
            child: const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  CircularProgressIndicator(),
                  SizedBox(height: 12),
                  Text('Iniciando mapa interactivo...',
                      style: TextStyle(color: AppTheme.greyText)),
                ],
              ),
            ),
          ),

        // Quick Selected Place Banner on bottom
        if (_selectedLugar != null)
          Positioned(
            left: 16,
            right: 16,
            bottom: 20,
            child: _buildSelectedLugarCard(_selectedLugar!),
          ),
      ],
    );
  }

  Widget _buildSelectedLugarCard(LugarConGeocerca lugar) {
    return Card(
      elevation: 6,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: AppTheme.accentOrange, width: 1.5),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const CircleAvatar(
                  backgroundColor: AppTheme.primaryDark,
                  radius: 16,
                  child: Icon(Icons.location_city, color: Colors.white, size: 16),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        lugar.nombre,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 16,
                          color: AppTheme.primaryDark,
                        ),
                      ),
                      Text(
                        lugar.direccion,
                        style: const TextStyle(fontSize: 12, color: AppTheme.greyText),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, size: 20),
                  onPressed: () {
                    setState(() {
                      _selectedLugar = null;
                    });
                  },
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    const Icon(Icons.people, size: 16, color: AppTheme.accentOrange),
                    const SizedBox(width: 6),
                    Text(
                      '${lugar.totalEmpleados} Empleados registrados',
                      style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
                    ),
                  ],
                ),
                Text(
                  lugar.tieneGeocerca ? 'Geocerca Lista' : 'Sin Geocerca',
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: lugar.tieneGeocerca ? AppTheme.success : AppTheme.error,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildListBody(bool isLoading, List<LugarConGeocerca> list) {
    if (isLoading && list.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (list.isEmpty) {
      return RefreshIndicator(
        onRefresh: () => context.read<UbicacionesProvider>().loadLugaresConGeocercas(),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          children: const [
            SizedBox(height: 80),
            Center(
              child: Column(
                children: [
                  Icon(Icons.location_off, size: 64, color: Colors.grey),
                  SizedBox(height: 16),
                  Text('No se encontraron lugares',
                      style: TextStyle(color: Colors.grey, fontSize: 16)),
                ],
              ),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => context.read<UbicacionesProvider>().loadLugaresConGeocercas(),
      child: ListView.builder(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        itemCount: list.length,
        itemBuilder: (context, index) {
          final lugar = list[index];
          return Card(
            elevation: 2,
            margin: const EdgeInsets.symmetric(vertical: 6),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: ListTile(
              contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              leading: Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: AppTheme.primaryDark.withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.location_on, color: AppTheme.primaryDark),
              ),
              title: Text(
                lugar.nombre,
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
              ),
              subtitle: Padding(
                padding: const EdgeInsets.only(top: 4.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      lugar.direccion,
                      style: const TextStyle(fontSize: 12, color: AppTheme.greyText),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        _buildBadge(
                          '${lugar.totalEmpleados} empleados',
                          AppTheme.primaryDark.withValues(alpha: 0.1),
                          AppTheme.primaryDark,
                        ),
                        const SizedBox(width: 8),
                        _buildBadge(
                          lugar.tieneGeocerca ? 'Geocerca activa' : 'Sin geocerca',
                          (lugar.tieneGeocerca ? AppTheme.success : AppTheme.error)
                              .withValues(alpha: 0.1),
                          lugar.tieneGeocerca ? AppTheme.success : AppTheme.error,
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              trailing: const Icon(Icons.chevron_right, color: Colors.grey),
              onTap: () => _showLugarDetailsBottomSheet(lugar),
            ),
          );
        },
      ),
    );
  }

  Widget _buildBadge(String label, Color bgColor, Color textColor) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.bold,
          color: textColor,
        ),
      ),
    );
  }

  void _showLugarDetailsBottomSheet(LugarConGeocerca lugar) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 38,
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
                const CircleAvatar(
                  backgroundColor: AppTheme.accentOrange,
                  radius: 20,
                  child: Icon(Icons.business, color: Colors.white, size: 20),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        lugar.nombre,
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.primaryDark,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        lugar.activo ? 'Establecimiento Activo' : 'Inactivo',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                          color: lugar.activo ? AppTheme.success : AppTheme.error,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            _buildDetailRow(Icons.pin_drop, 'Dirección', lugar.direccion),
            if (lugar.descripcion != null && lugar.descripcion!.isNotEmpty)
              _buildDetailRow(Icons.info_outline, 'Detalles', lugar.descripcion!),
            _buildDetailRow(Icons.group, 'Empleados Asignados', '${lugar.totalEmpleados} colaboradores'),
            _buildDetailRow(Icons.radar, 'Estado de Geocerca', lugar.tieneGeocerca ? 'Habilitada y dibujada' : 'No disponible'),
            const SizedBox(height: 24),
            if (lugar.tieneGeocerca)
              ElevatedButton.icon(
                icon: const Icon(Icons.map),
                label: const Text('VER GEOCERCA EN EL MAPA'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.accentOrange,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                onPressed: () {
                  Navigator.pop(ctx);
                  _focusLugarOnMap(lugar);
                },
              )
            else
              OutlinedButton(
                onPressed: null,
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                child: const Text('MAPA NO DISPONIBLE SIN GEOCERCA'),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildDetailRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: AppTheme.greyText),
          const SizedBox(width: 12),
          SizedBox(
            width: 120,
            child: Text(
              label,
              style: const TextStyle(fontSize: 13, color: AppTheme.greyText, fontWeight: FontWeight.w500),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(fontSize: 13, color: AppTheme.primaryDark, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }
}
