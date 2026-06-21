using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hikr.Api.Migrations
{
    /// <inheritdoc />
    public partial class RouteWaypointArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Waypoints_Routes_RouteId",
                table: "Waypoints");

            migrationBuilder.DropIndex(
                name: "IX_Waypoints_RouteId",
                table: "Waypoints");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Waypoints");

            migrationBuilder.AddColumn<List<int>>(
                name: "WaypointIds",
                table: "Routes",
                type: "integer[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaypointIds",
                table: "Routes");

            migrationBuilder.AddColumn<int>(
                name: "RouteId",
                table: "Waypoints",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Waypoints_RouteId",
                table: "Waypoints",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Waypoints_Routes_RouteId",
                table: "Waypoints",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
