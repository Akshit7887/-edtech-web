using EdTechApi.DTOs;
using EdTechApi.Middleware;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/departments")]
[RequireRole("admin")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _deptService;

    public DepartmentController(IDepartmentService deptService)
    {
        _deptService = deptService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var depts = await _deptService.GetAllAsync();
        return Ok(new { success = true, data = depts });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dept = await _deptService.GetByIdAsync(id);
        if (dept == null) return NotFound(new { success = false, error = "Department not found" });
        return Ok(new { success = true, data = dept });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        if (string.IsNullOrEmpty(request.Name))
            return BadRequest(new { success = false, error = "Department name is required" });
        var dept = await _deptService.CreateAsync(request);
        return Created(string.Empty, new { success = true, data = dept });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var dept = await _deptService.UpdateAsync(id, request);
        if (dept == null) return NotFound(new { success = false, error = "Department not found" });
        return Ok(new { success = true, data = dept });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _deptService.DeleteAsync(id);
        if (!ok) return NotFound(new { success = false, error = "Department not found" });
        return Ok(new { success = true, message = "Department deleted" });
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignUser([FromBody] AssignDepartmentRequest request)
    {
        await _deptService.AssignUserToDepartmentAsync(request.UserId, request.DepartmentId);
        return Ok(new { success = true });
    }

    [HttpPost("remove-user/{userId:int}")]
    public async Task<IActionResult> RemoveUser(int userId)
    {
        await _deptService.RemoveUserFromDepartmentAsync(userId);
        return Ok(new { success = true });
    }

    [HttpGet("{id:int}/users")]
    public async Task<IActionResult> GetUsers(int id, [FromQuery] string? role)
    {
        var users = await _deptService.GetDepartmentUsersAsync(id, role);
        return Ok(new { success = true, data = users });
    }
}
