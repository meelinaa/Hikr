using Hikr.Api.Data;
using Hikr.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hikr.Api.Repositories;

public class WaypointRepository : IWaypointRepository
{
    private readonly HikrDbContext _context;

    public WaypointRepository(HikrDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Waypoints>> GetAllAsync()
    {
        return await _context.Waypoints.ToListAsync();
    }

    public async Task<IEnumerable<Waypoints>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await _context.Waypoints
            .Where(w => ids.Contains(w.Id))
            .ToListAsync();
    }

    public async Task<Waypoints?> GetByIdAsync(int id)
    {
        return await _context.Waypoints.FindAsync(id);
    }

    public async Task<Waypoints> AddAsync(Waypoints waypoint)
    {
        _context.Waypoints.Add(waypoint);
        await _context.SaveChangesAsync();
        return waypoint;
    }

    public async Task UpdateAsync(Waypoints waypoint)
    {
        _context.Entry(waypoint).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var waypoint = await _context.Waypoints.FindAsync(id);
        if (waypoint != null)
        {
            _context.Waypoints.Remove(waypoint);
            await _context.SaveChangesAsync();
        }
    }
}
