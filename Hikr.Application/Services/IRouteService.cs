using Hikr.Application.DTOs;

namespace Hikr.Application.Services;

public interface IRouteService
{
    Task<IEnumerable<RouteDto>> GetAllRoutesAsync();
    Task<RouteDto?> GetRouteByIdAsync(int id);
    Task<RouteDto> CreateRouteAsync(CreateRouteDto createDto);
    Task UpdateRouteAsync(UpdateRouteDto updateDto);
    Task DeleteRouteAsync(int id);
}
