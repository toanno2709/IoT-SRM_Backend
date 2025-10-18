using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;

namespace AppBackend.Repositories.Repositories.ClassRepo;

public interface IClassRepository : IGenericRepository<Class>
{
    Task<List<Class>> GetAssignedClassesAsync(int instructorId);
    Task<Class?> GetClassWithDetailsAsync(int classId);
    Task<List<Class>> GetBySemesterAsync(int semesterId);
    Task<List<Class>> SearchClassesAsync(int? semesterId, string? searchQuery);
    Task<bool> ClassNameExistsAsync(string className, int semesterId);
    Task<bool> ClassNameExistsAsync(string className, int semesterId, int excludeClassId);
    Task<bool> HasGroupsAsync(int classId);
    Task<bool> HasEnrollmentsAsync(int classId);
}




