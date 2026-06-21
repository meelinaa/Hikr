using Hikr.Application.DTOs;
using Hikr.Domain.Entities;

namespace Hikr.Application.Mappers;

public static class WaypointMapper
{
    public static WaypointDto ToDto(this Waypoints waypoint)
    {
        return new WaypointDto
        {
            Id = waypoint.Id,
            GeoJson = waypoint.GeoJson,
            Name = waypoint.Name,
            Type = waypoint.Type
        };
    }

    public static Waypoints ToEntity(this CreateWaypointDto dto)
    {
        return new Waypoints
        {
            GeoJson = dto.GeoJson,
            Name = dto.Name,
            Type = dto.Type
        };
    }

    public static void UpdateEntity(this UpdateWaypointDto dto, Waypoints waypoint)
    {
        waypoint.GeoJson = dto.GeoJson;
        waypoint.Name = dto.Name;
        waypoint.Type = dto.Type;
    }
}
