using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapboxMegaservicios.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPossibleSpoofing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPossibleSpoofing",
                table: "Ubicaciones",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPossibleSpoofing",
                table: "Ubicaciones");
        }
    }
}
