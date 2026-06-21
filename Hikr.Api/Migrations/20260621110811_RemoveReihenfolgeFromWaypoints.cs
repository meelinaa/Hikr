using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hikr.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReihenfolgeFromWaypoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reihenfolge",
                table: "Waypoints");

            migrationBuilder.RenameColumn(
                name: "Transportmittel",
                table: "Routes",
                newName: "Transportation");

            migrationBuilder.RenameColumn(
                name: "ErstelltAm",
                table: "Routes",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Transportation",
                table: "Routes",
                newName: "Transportmittel");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Routes",
                newName: "ErstelltAm");

            migrationBuilder.AddColumn<int>(
                name: "Reihenfolge",
                table: "Waypoints",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
