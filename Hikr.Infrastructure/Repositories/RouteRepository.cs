using Hikr.Application.Ports.Outbound;
using Hikr.Domain.Entities;
using Hikr.Infrastructure.Data;
using Hikr.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hikr.Infrastructure.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly HikrDbContext _context;

    public RouteRepository(HikrDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Route>> GetAllAsync()
    {
        var entities = await _context.Routes
            .Include(r => r.RouteWaypoints)
            .ToListAsync();
            
        return entities.Select(MapToDomain);
    }

    public async Task<Route?> GetByIdAsync(int id)
    {
        var entity = await _context.Routes
            .Include(r => r.RouteWaypoints)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<Route> AddAsync(Route route)
    {
        var entity = MapToEntity(route);
        _context.Routes.Add(entity);
        await _context.SaveChangesAsync();
        route.Id = entity.Id;
        return route;
    }

    public async Task UpdateAsync(Route route)
    {
        var entity = await _context.Routes
            .Include(r => r.RouteWaypoints)
            .FirstOrDefaultAsync(r => r.Id == route.Id);
            
        if (entity != null)
        {
            entity.Name = route.Name;
            entity.Transportation = route.Transportation;
            entity.Geometry = route.Geometry;
            // Update Waypoints if necessary... (simplified for this refactoring)
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Routes.FindAsync(id);
        if (entity != null)
        {
            _context.Routes.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    private static Route MapToDomain(RouteEntity entity)
    {
        return new Route
        {
            Id = entity.Id,
            Name = entity.Name,
            Transportation = entity.Transportation,
            CreatedAt = entity.CreatedAt,
            Geometry = entity.Geometry,
            RouteWaypoints = entity.RouteWaypoints.Select(rw => new RouteWaypoint
            {
                Id = rw.Id,
                RouteId = rw.RouteId,
                WaypointId = rw.WaypointId,
                Order = rw.Order
            }).ToList()
        };
    }

    private static RouteEntity MapToEntity(Route domain)
    {
        return new RouteEntity
        {
            Id = domain.Id,
            Name = domain.Name,
            Transportation = domain.Transportation,
            CreatedAt = domain.CreatedAt,
            Geometry = domain.Geometry,
            RouteWaypoints = domain.RouteWaypoints.Select(rw => new RouteWaypointEntity
            {
                Id = rw.Id,
                RouteId = rw.RouteId,
                WaypointId = rw.WaypointId,
                Order = rw.Order
            }).ToList()
        };
    }
}
