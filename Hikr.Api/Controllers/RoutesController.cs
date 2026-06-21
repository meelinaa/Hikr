using Microsoft.AspNetCore.Mvc;

namespace Hikr.Api.Controllers;

[ApiController]
[Route("routes")]
public class RoutesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAllRoutes()
    {
        return Ok();
    }

    [HttpGet("{id}")]
    public IActionResult GetRouteById(int id)
    {
        return Ok();
    }

    [HttpPost]
    public IActionResult CreateRoute()
    {
        return Ok();
    }

    [HttpPut("{id}")]
    public IActionResult UpdateRoute(int id)
    {
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteRoute(int id)
    {
        return Ok();
    }
}
