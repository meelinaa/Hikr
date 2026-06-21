using Hikr.Api.Data;
using Hikr.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hikr.Api.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly HikrDbContext _context;

    public RouteRepository(HikrDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Routes>> GetAllAsync()
    {
        return await _context.Routes
            .ToListAsync();
    }

    public async Task<Routes?> GetByIdAsync(int id)
    {
        return await _context.Routes
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Routes> AddAsync(Routes route)
    {
        _context.Routes.Add(route);
        await _context.SaveChangesAsync();
        return route;
    }

    public async Task UpdateAsync(Routes route)
    {
        _context.Entry(route).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var route = await _context.Routes.FindAsync(id);
        if (route != null)
        {
            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();
        }
    }
}
