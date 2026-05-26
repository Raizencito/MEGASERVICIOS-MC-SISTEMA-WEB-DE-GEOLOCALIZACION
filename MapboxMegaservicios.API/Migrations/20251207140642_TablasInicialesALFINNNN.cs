using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MapboxMegaservicios.API.Migrations
{
    /// <inheritdoc />
    public partial class TablasInicialesALFINNNN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstadosAlerta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosAlerta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LugaresTrabajo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Geocerca = table.Column<Polygon>(type: "geometry", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartamentoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LugaresTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LugaresTrabajo_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Paterno = table.Column<string>(type: "text", nullable: false),
                    Materno = table.Column<string>(type: "text", nullable: false),
                    Nombres = table.Column<string>(type: "text", nullable: false),
                    Ci = table.Column<string>(type: "text", nullable: false),
                    Usuario = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: false),
                    LugarTrabajoActualId = table.Column<int>(type: "integer", nullable: true),
                    IdRol = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleados_LugaresTrabajo_LugarTrabajoActualId",
                        column: x => x.LugarTrabajoActualId,
                        principalTable: "LugaresTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Empleados_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertasGeocerca",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    EstadoAlertaId = table.Column<int>(type: "integer", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasGeocerca", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasGeocerca_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertasGeocerca_EstadosAlerta_EstadoAlertaId",
                        column: x => x.EstadoAlertaId,
                        principalTable: "EstadosAlerta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialLugaresTrabajo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    LugarTrabajoId = table.Column<int>(type: "integer", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialLugaresTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialLugaresTrabajo_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialLugaresTrabajo_LugaresTrabajo_LugarTrabajoId",
                        column: x => x.LugarTrabajoId,
                        principalTable: "LugaresTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JornadasTrabajo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraEntrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HoraSalida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalHoras = table.Column<decimal>(type: "numeric", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TiempoFueraGeocerca = table.Column<int>(type: "integer", nullable: false),
                    AlertasGeneradas = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JornadasTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JornadasTrabajo_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ubicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    UbicacionEmp = table.Column<Point>(type: "geometry", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstaEnGeocerca = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ubicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ubicaciones_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAsistencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpleadoId = table.Column<int>(type: "integer", nullable: false),
                    TipoRegistro = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UbicacionId = table.Column<int>(type: "integer", nullable: true),
                    EsAutomatico = table.Column<bool>(type: "boolean", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Verificado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAsistencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAsistencia_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosAsistencia_Ubicaciones_UbicacionId",
                        column: x => x.UbicacionId,
                        principalTable: "Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasGeocerca_EmpleadoId",
                table: "AlertasGeocerca",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasGeocerca_EstadoAlertaId",
                table: "AlertasGeocerca",
                column: "EstadoAlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasGeocerca_FechaHora",
                table: "AlertasGeocerca",
                column: "FechaHora",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Ci",
                table: "Empleados",
                column: "Ci",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_IdRol",
                table: "Empleados",
                column: "IdRol");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_LugarTrabajoActualId",
                table: "Empleados",
                column: "LugarTrabajoActualId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Usuario",
                table: "Empleados",
                column: "Usuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialLugaresTrabajo_EmpleadoId",
                table: "HistorialLugaresTrabajo",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialLugaresTrabajo_FechaCambio",
                table: "HistorialLugaresTrabajo",
                column: "FechaCambio",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialLugaresTrabajo_LugarTrabajoId",
                table: "HistorialLugaresTrabajo",
                column: "LugarTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_JornadasTrabajo_EmpleadoId_Fecha",
                table: "JornadasTrabajo",
                columns: new[] { "EmpleadoId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JornadasTrabajo_Fecha",
                table: "JornadasTrabajo",
                column: "Fecha",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_LugaresTrabajo_DepartamentoId",
                table: "LugaresTrabajo",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_LugaresTrabajo_Geocerca",
                table: "LugaresTrabajo",
                column: "Geocerca")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistencia_EmpleadoId",
                table: "RegistrosAsistencia",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistencia_FechaHora",
                table: "RegistrosAsistencia",
                column: "FechaHora",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistencia_TipoRegistro",
                table: "RegistrosAsistencia",
                column: "TipoRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAsistencia_UbicacionId",
                table: "RegistrosAsistencia",
                column: "UbicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_EmpleadoId",
                table: "Ubicaciones",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_FechaHora",
                table: "Ubicaciones",
                column: "FechaHora",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_UbicacionEmp",
                table: "Ubicaciones",
                column: "UbicacionEmp")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasGeocerca");

            migrationBuilder.DropTable(
                name: "HistorialLugaresTrabajo");

            migrationBuilder.DropTable(
                name: "JornadasTrabajo");

            migrationBuilder.DropTable(
                name: "RegistrosAsistencia");

            migrationBuilder.DropTable(
                name: "EstadosAlerta");

            migrationBuilder.DropTable(
                name: "Ubicaciones");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "LugaresTrabajo");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Departamentos");
        }
    }
}
