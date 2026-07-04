using Hikr.Application.Ports.Outbound;
using Hikr.Domain.Entities;
using Hikr.Infrastructure.Data;
using Hikr.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hikr.Infrastructure.Repositories;

public class WaypointRepository : IWaypointRepository
{
    private readonly HikrDbContext _context;

    public WaypointRepository(HikrDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Waypoint>> GetAllAsync()
    {
        var entities = await _context.Waypoints.ToListAsync();
        return entities.Select(MapToDomain);
    }

    public async Task<IEnumerable<Waypoint>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var entities = await _context.Waypoints
            .Where(w => ids.Contains(w.Id))
            .ToListAsync();
        return entities.Select(MapToDomain);
    }

    public async Task<Waypoint?> GetByIdAsync(int id)
    {
        var entity = await _context.Waypoints.FindAsync(id);
        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<Waypoint> AddAsync(Waypoint waypoint)
    {
        var entity = MapToEntity(waypoint);
        _context.Waypoints.Add(entity);
        await _context.SaveChangesAsync();
        waypoint.Id = entity.Id;
        return waypoint;
    }

    public async Task UpdateAsync(Waypoint waypoint)
    {
        var entity = await _context.Waypoints.FindAsync(waypoint.Id);
        if (entity != null)
        {
            entity.Name = waypoint.Name;
            entity.Type = waypoint.Type;
            entity.Geometry = waypoint.Geometry;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Waypoints.FindAsync(id);
        if (entity != null)
        {
            _context.Waypoints.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    private static Waypoint MapToDomain(WaypointEntity entity)
    {
        return new Waypoint
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Geometry = entity.Geometry
        };
    }

    private static WaypointEntity MapToEntity(Waypoint domain)
    {
        return new WaypointEntity
        {
            Id = domain.Id,
            Name = domain.Name,
            Type = domain.Type,
            Geometry = domain.Geometry
        };
    }
}
