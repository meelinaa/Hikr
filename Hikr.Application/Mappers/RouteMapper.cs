using Hikr.Application.DTOs;
using Hikr.Domain.Entities;

namespace Hikr.Application.Mappers;

public static class RouteMapper
{
    public static RouteDto ToDto(this Routes route)
    {
        return new RouteDto
        {
            Id = route.Id,
            Name = route.Name,
            Transportation = route.Transportation,
            CreatedAt = route.CreatedAt,
            GeoJson = route.GeoJson,
            WaypointIds = route.WaypointIds
        };
    }

    public static Routes ToEntity(this CreateRouteDto dto)
    {
        return new Routes
        {
            Name = dto.Name,
            Transportation = dto.Transportation,
            GeoJson = dto.GeoJson,
            WaypointIds = dto.WaypointIds
        };
    }

    public static void UpdateEntity(this UpdateRouteDto dto, Routes route)
    {
        route.Name = dto.Name;
        route.Transportation = dto.Transportation;
        route.GeoJson = dto.GeoJson;
        route.WaypointIds = dto.WaypointIds;
    }
}
