using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hikr.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoutesAndWaypoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaypointIds",
                table: "Routes");

            migrationBuilder.RenameColumn(
                name: "GeoJson",
                table: "Waypoints",
                newName: "Geometry");

            migrationBuilder.RenameColumn(
                name: "GeoJson",
                table: "Routes",
                newName: "Geometry");

            migrationBuilder.CreateTable(
                name: "RouteWaypoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteId = table.Column<int>(type: "integer", nullable: false),
                    WaypointId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteWaypoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteWaypoints_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteWaypoints_Waypoints_WaypointId",
                        column: x => x.WaypointId,
                        principalTable: "Waypoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteWaypoints_RouteId",
                table: "RouteWaypoints",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteWaypoints_WaypointId",
                table: "RouteWaypoints",
                column: "WaypointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteWaypoints");

            migrationBuilder.RenameColumn(
                name: "Geometry",
                table: "Waypoints",
                newName: "GeoJson");

            migrationBuilder.RenameColumn(
                name: "Geometry",
                table: "Routes",
                newName: "GeoJson");

            migrationBuilder.AddColumn<List<int>>(
                name: "WaypointIds",
                table: "Routes",
                type: "integer[]",
                nullable: false);
        }
    }
}
