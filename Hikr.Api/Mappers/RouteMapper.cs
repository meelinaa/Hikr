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
            GeoJson = route.Geometry,
            WaypointIds = route.RouteWaypoints.OrderBy(rw => rw.Order).Select(rw => rw.WaypointId).ToList()
        };
    }

    public static Routes ToEntity(this CreateRouteDto dto)
    {
        return new Routes
        {
            Name = dto.Name,
            Transportation = dto.Transportation,
            RouteWaypoints = dto.WaypointIds
                .Select((id, index) => new RouteWaypoint 
                { 
                    WaypointId = id, 
                    Order = index + 1 
                })
                .ToList()
        };
    }

    public static void UpdateEntity(this UpdateRouteDto dto, Routes route)
    {
        route.Name = dto.Name;
        route.Transportation = dto.Transportation;
        route.RouteWaypoints = dto.WaypointIds
            .Select((id, index) => new RouteWaypoint 
            { 
                WaypointId = id, 
                Order = index + 1 
            })
            .ToList();
    }
}
