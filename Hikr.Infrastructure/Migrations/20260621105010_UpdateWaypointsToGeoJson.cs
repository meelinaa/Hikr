using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Hikr.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWaypointsToGeoJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Waypoints");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Waypoints");

            migrationBuilder.AddColumn<Geometry>(
                name: "GeoJson",
                table: "Waypoints",
                type: "geometry",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Waypoints",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeoJson",
                table: "Waypoints");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Waypoints");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Waypoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Waypoints",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
