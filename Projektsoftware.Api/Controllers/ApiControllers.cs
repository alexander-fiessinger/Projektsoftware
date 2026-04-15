using Microsoft.AspNetCore.Mvc;
using Projektsoftware.Api.Models;
using Projektsoftware.Api.Services;

namespace Projektsoftware.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ApiDatabaseService db) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await db.AuthenticateAsync(request.Username, request.Password);
        if (!result.Success)
            return Unauthorized(result);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class DashboardController(ApiDatabaseService db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        return Ok(await db.GetDashboardAsync());
    }
}

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(ApiDatabaseService db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        return Ok(await db.GetProjectsAsync());
    }
}

[ApiController]
[Route("api/[controller]")]
public class TasksController(ApiDatabaseService db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll([FromQuery] int? projectId)
    {
        return Ok(await db.GetTasksAsync(projectId));
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] TaskCreateRequest request)
    {
        var id = await db.CreateTaskAsync(request);
        return Created($"/api/tasks/{id}", new { Id = id });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TaskStatusUpdateRequest request)
    {
        var validStatuses = new[] { "Offen", "In Arbeit", "Blockiert", "Erledigt" };
        if (!validStatuses.Contains(request.Status))
            return BadRequest("Ungültiger Status");

        await db.UpdateTaskStatusAsync(id, request.Status);
        return NoContent();
    }
}
