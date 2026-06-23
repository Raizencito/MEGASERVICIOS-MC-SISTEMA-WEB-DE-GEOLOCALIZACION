import 'package:sqflite/sqflite.dart';
import 'package:path/path.dart';

class DatabaseHelper {
  static final DatabaseHelper instance = DatabaseHelper._init();
  static Database? _database;

  DatabaseHelper._init();

  Future<Database> get database async {
    if (_database != null) return _database!;
    _database = await _initDB('ubicaciones_offline.db');
    return _database!;
  }

  Future<Database> _initDB(String filePath) async {
    final dbPath = await getDatabasesPath();
    final path = join(dbPath, filePath);

    return await openDatabase(
      path,
      version: 1,
      onCreate: _createDB,
    );
  }

  Future _createDB(Database db, int version) async {
    const idType = 'INTEGER PRIMARY KEY AUTOINCREMENT';
    const realType = 'REAL NOT NULL';
    const textType = 'TEXT NOT NULL';

    await db.execute('''
CREATE TABLE ubicaciones_offline (
  id $idType,
  latitud $realType,
  longitud $realType,
  fecha_hora $textType
)
''');
  }

  Future<void> insertarUbicacion(double latitud, double longitud, DateTime fechaHora) async {
    final db = await instance.database;
    await db.insert('ubicaciones_offline', {
      'latitud': latitud,
      'longitud': longitud,
      'fecha_hora': fechaHora.toIso8601String(),
    });
  }

  Future<List<Map<String, dynamic>>> obtenerUbicacionesPendientes() async {
    final db = await instance.database;
    return await db.query('ubicaciones_offline', orderBy: 'fecha_hora ASC');
  }

  Future<void> eliminarUbicacionesSincronizadas(List<int> ids) async {
    if (ids.isEmpty) return;
    
    final db = await instance.database;
    final idPlaceholders = List.filled(ids.length, '?').join(',');
    
    await db.delete(
      'ubicaciones_offline',
      where: 'id IN ($idPlaceholders)',
      whereArgs: ids,
    );
  }

  Future<void> vaciarTabla() async {
    final db = await instance.database;
    await db.delete('ubicaciones_offline');
  }
}
