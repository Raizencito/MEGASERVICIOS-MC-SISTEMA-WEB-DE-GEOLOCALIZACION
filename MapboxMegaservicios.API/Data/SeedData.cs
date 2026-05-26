using System.Security.Cryptography;
using System.Text;
using MapboxMegaservicios.API.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapboxMegaservicios.API.Data
{
    public static class SeedData
    {
        public static async Task Inicializar(ApplicationDbContext context)
        {
            Console.WriteLine("🎯 Iniciando SeedData...");

            try
            {
                // 1. VERIFICAR SI YA HAY DATOS VÁLIDOS
                if (await TieneDatosValidos(context))
                {
                    Console.WriteLine("✅ Ya existen datos válidos, omitiendo SeedData");
                    return;
                }

                Console.WriteLine("🆕 Creando datos iniciales...");

                // 2. DEPARTAMENTOS (PRIMERO - SON REQUERIDOS)
                Console.WriteLine("📝 Insertando departamentos...");
                var departamentos = await CrearDepartamentos(context);

                // 3. ROLES
                Console.WriteLine("📝 Insertando roles...");
                var (adminRol, empleadoRol) = await CrearRoles(context);

                // 4. ESTADOS DE ALERTA
                Console.WriteLine("📝 Insertando estados de alerta...");
                await CrearEstadosAlerta(context);

                // 5. LUGARES DE TRABAJO (CON [Column] o renombrando)
                Console.WriteLine("📝 Insertando lugares de trabajo...");
                var lugares = await CrearLugaresTrabajo(context, departamentos);

                // 6. USUARIO ADMIN
                Console.WriteLine("📝 Insertando usuario admin...");
                await CrearAdmin(context, adminRol, lugares);

                // 7. EMPLEADOS DE PRUEBA
                Console.WriteLine("📝 Insertando empleados de prueba...");
                await CrearEmpleadosPrueba(context, empleadoRol, lugares);

                Console.WriteLine("\n🎉 SeedData completado exitosamente!");
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("👤 Usuario admin: admin / admin123");
                Console.WriteLine("👥 Empleados prueba: usuario: juan.perez / password: 123456");
                Console.WriteLine("📍 Lugares creados: 3 (Colegio, Universidad, Hospital)");
                Console.WriteLine("═══════════════════════════════════════\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en SeedData: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                // NO relanzar - dejar que la aplicación continúe
            }
        }

        private static async Task<bool> TieneDatosValidos(ApplicationDbContext context)
        {
            try
            {
                var tieneDepartamentos = await context.Departamentos.AnyAsync();
                var tieneRoles = await context.Roles.AnyAsync();
                var tieneAdmin = await context.Empleados
                    .AnyAsync(e => e.Usuario == "admin" && e.Activo);

                return tieneDepartamentos && tieneRoles && tieneAdmin;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<List<Departamento>> CrearDepartamentos(ApplicationDbContext context)
        {
            var departamentos = new[]
            {
                new Departamento { Nombre = "La Paz" },
                new Departamento { Nombre = "Cochabamba" },
                new Departamento { Nombre = "Santa Cruz" },
                new Departamento { Nombre = "Oruro" },
                new Departamento { Nombre = "Potosí" },
                new Departamento { Nombre = "Chuquisaca" },
                new Departamento { Nombre = "Tarija" },
                new Departamento { Nombre = "Beni" },
                new Departamento { Nombre = "Pando" }
            };

            foreach (var depto in departamentos)
            {
                if (!await context.Departamentos.AnyAsync(d => d.Nombre == depto.Nombre))
                {
                    context.Departamentos.Add(depto);
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"✅ {departamentos.Length} departamentos creados");

            return await context.Departamentos.ToListAsync();
        }

        private static async Task<(Rol adminRol, Rol empleadoRol)> CrearRoles(ApplicationDbContext context)
        {
            var adminRol = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Administrador");
            var empleadoRol = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Empleado");

            if (adminRol == null)
            {
                adminRol = new Rol { Nombre = "Administrador" };
                context.Roles.Add(adminRol);
            }

            if (empleadoRol == null)
            {
                empleadoRol = new Rol { Nombre = "Empleado" };
                context.Roles.Add(empleadoRol);
            }

            await context.SaveChangesAsync();
            Console.WriteLine("✅ Roles creados");

            return (adminRol, empleadoRol);
        }

        private static async Task CrearEstadosAlerta(ApplicationDbContext context)
        {
            if (!await context.EstadosAlerta.AnyAsync())
            {
                context.EstadosAlerta.AddRange(
                    new EstadoAlerta { Codigo = "DENTRO", Descripcion = "Dentro del área de trabajo" },
                    new EstadoAlerta { Codigo = "FUERA", Descripcion = "Fuera del área de trabajo" }
                );

                await context.SaveChangesAsync();
                Console.WriteLine("✅ Estados de alerta creados");
            }
        }

        private static async Task<Dictionary<string, LugarTrabajo>> CrearLugaresTrabajo(
            ApplicationDbContext context, List<Departamento> departamentos)
        {
            var lugaresData = new[]
            {
                new {
                    Nombre = "Colegio La Paz Centro",
                    Direccion = "Av. Arce #1234, La Paz",
                    Descripcion = "Colegio público en el centro de La Paz",
                    Departamento = "La Paz",
                    CenterLng = -68.1193,
                    CenterLat = -16.4897,
                    Radius = 0.002 // Radio pequeño en grados
                },
                new {
                    Nombre = "Universidad UMSS",
                    Direccion = "Calle Jordan #456, Cochabamba",
                    Descripcion = "Universidad Mayor de San Simón",
                    Departamento = "Cochabamba",
                    CenterLng = -66.1568,
                    CenterLat = -17.3895,
                    Radius = 0.002
                },
                new {
                    Nombre = "Hospital Santa Cruz",
                    Direccion = "Av. Cristo Redentor #789, Santa Cruz",
                    Descripcion = "Hospital público regional",
                    Departamento = "Santa Cruz",
                    CenterLng = -63.1812,
                    CenterLat = -17.7833,
                    Radius = 0.002
                }
            };

            var lugaresCreados = new Dictionary<string, LugarTrabajo>();

            foreach (var lugarData in lugaresData)
            {
                var lugarExistente = await context.LugaresTrabajo
                    .FirstOrDefaultAsync(l => l.Nombre == lugarData.Nombre);

                if (lugarExistente == null)
                {
                    var departamento = departamentos.First(d => d.Nombre == lugarData.Departamento);

                    lugarExistente = new LugarTrabajo
                    {
                        Nombre = lugarData.Nombre,
                        Direccion = lugarData.Direccion,
                        Descripcion = lugarData.Descripcion,
                        DepartamentoId = departamento.Id,
                        Geocerca = CreateSquarePolygon(lugarData.CenterLng, lugarData.CenterLat, lugarData.Radius),
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow
                    };

                    context.LugaresTrabajo.Add(lugarExistente);
                    lugaresCreados[lugarData.Nombre] = lugarExistente;
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"✅ {lugaresCreados.Count} lugares de trabajo creados");
            return lugaresCreados;
        }

        private static async Task CrearAdmin(
            ApplicationDbContext context,
            Rol adminRol,
            Dictionary<string, LugarTrabajo> lugares)
        {
            if (!await context.Empleados.AnyAsync(e => e.Usuario == "admin"))
            {
                var admin = new Empleado
                {
                    Paterno = "Administrador",
                    Materno = "Sistema",
                    Nombres = "Admin",
                    Ci = "0000000",
                    Usuario = "admin",
                    PasswordHash = HashPassword("admin123"),
                    IdRol = adminRol.Id,
                    // OJO: Aquí está el problema - dependiendo de cómo configures:
                    LugarTrabajoActualId = lugares["Colegio La Paz Centro"].Id, // ← Si renombraste
                    // IdLugarTrabajoActual = lugares["Colegio La Paz Centro"].Id, // ← Si mantienes nombre
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                context.Empleados.Add(admin);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Admin creado: admin / admin123");
            }
        }

        private static async Task CrearEmpleadosPrueba(
            ApplicationDbContext context,
            Rol empleadoRol,
            Dictionary<string, LugarTrabajo> lugares)
        {
            var empleadosData = new[]
            {
                new {
                    Paterno = "Perez",
                    Materno = "Gomez",
                    Nombres = "Juan Carlos",
                    Ci = "1234567",
                    Usuario = "juan.perez",
                    Lugar = "Colegio La Paz Centro"
                },
                new {
                    Paterno = "Rodriguez",
                    Materno = "Lopez",
                    Nombres = "Maria Elena",
                    Ci = "7654321",
                    Usuario = "maria.rodriguez",
                    Lugar = "Universidad UMSS"
                },
                new {
                    Paterno = "Garcia",
                    Materno = "Martinez",
                    Nombres = "Carlos Alberto",
                    Ci = "1122334",
                    Usuario = "carlos.garcia",
                    Lugar = "Hospital Santa Cruz"
                }
            };

            var nuevos = 0;
            foreach (var empData in empleadosData)
            {
                if (!await context.Empleados.AnyAsync(e => e.Usuario == empData.Usuario))
                {
                    var empleado = new Empleado
                    {
                        Paterno = empData.Paterno,
                        Materno = empData.Materno,
                        Nombres = empData.Nombres,
                        Ci = empData.Ci,
                        Usuario = empData.Usuario,
                        PasswordHash = HashPassword("123456"),
                        IdRol = empleadoRol.Id,
                        LugarTrabajoActualId = lugares[empData.Lugar].Id, 
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow
                    };

                    context.Empleados.Add(empleado);
                    nuevos++;
                }
            }

            if (nuevos > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ {nuevos} empleados de prueba creados (password: 123456)");
            }
        }

        private static Polygon CreateSquarePolygon(double centerLng, double centerLat, double offset)
        {
            // Crear un cuadrado simple
            var coordinates = new[]
            {
                new Coordinate(centerLng - offset, centerLat - offset),
                new Coordinate(centerLng + offset, centerLat - offset),
                new Coordinate(centerLng + offset, centerLat + offset),
                new Coordinate(centerLng - offset, centerLat + offset),
                new Coordinate(centerLng - offset, centerLat - offset) // Cerrar el polígono
            };

            return new Polygon(new LinearRing(coordinates)) { SRID = 4326 };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}