using EdTechApi.DTOs;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface IDepartmentService
{
    Task<List<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(int id);
    Task<Department> CreateAsync(CreateDepartmentRequest request);
    Task<Department?> UpdateAsync(int id, UpdateDepartmentRequest request);
    Task<bool> DeleteAsync(int id);
    Task AssignUserToDepartmentAsync(int userId, int departmentId);
    Task RemoveUserFromDepartmentAsync(int userId);
    Task<List<Models.User>> GetDepartmentUsersAsync(int departmentId, string? role = null);
}
