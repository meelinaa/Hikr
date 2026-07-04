using Hikr.Api.DTOs.Waypoints;
using Hikr.Domain.Entities;

namespace Hikr.Api.Mappers;

public static class WaypointMapper
{
    public static WaypointDto ToDto(this Waypoint waypoint)
    {
        return new WaypointDto
        {
            Id = waypoint.Id,
            Geometry = waypoint.Geometry,
            Name = waypoint.Name,
            Type = waypoint.Type
        };
    }

    public static Waypoint ToEntity(this CreateWaypointDto dto)
    {
        return new Waypoint
        {
            Geometry = dto.Geometry,
            Name = dto.Name,
            Type = dto.Type
        };
    }

    public static void UpdateEntity(this UpdateWaypointDto dto, Waypoint waypoint)
    {
        waypoint.Geometry = dto.Geometry;
        waypoint.Name = dto.Name;
        waypoint.Type = dto.Type;
    }
}
