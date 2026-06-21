using Hikr.Application.DTOs;

namespace Hikr.Application.Services;

public interface IWaypointService
{
    Task<IEnumerable<WaypointDto>> GetAllWaypointsAsync();
    Task<IEnumerable<WaypointDto>> GetWaypointsByIdsAsync(IEnumerable<int> ids);
    Task<WaypointDto?> GetWaypointByIdAsync(int id);
    Task<WaypointDto> CreateWaypointAsync(CreateWaypointDto createDto);
    Task UpdateWaypointAsync(UpdateWaypointDto updateDto);
    Task DeleteWaypointAsync(int id);
}
