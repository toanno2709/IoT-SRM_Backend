using AppBackend.BusinessObjects.Models;

namespace AppBackend.Repositories.Repositories.SyllabusRepo;

public interface ISyllabusRepository
{
    Task<Syllabus?> GetByIdAsync(int syllabusId);
    Task<List<Syllabus>> GetByClassIdAsync(int classId);
    Task<List<Syllabus>> GetByInstructorIdAsync(int instructorId);
    Task<Syllabus> CreateAsync(Syllabus syllabus);
    Task UpdateAsync(Syllabus syllabus);
    Task DeleteAsync(int syllabusId);
    Task<bool> ExistsAsync(int syllabusId);
    Task<bool> IsInstructorOwnerAsync(int syllabusId, int instructorId);
    
    // File operations
    Task<SyllabusFile?> GetFileByIdAsync(int fileId);
    Task<List<SyllabusFile>> GetFilesBySyllabusIdAsync(int syllabusId);
    Task<SyllabusFile> AddFileAsync(SyllabusFile file);
    Task UpdateFileAsync(SyllabusFile file);
    Task DeleteFileAsync(int fileId);
    Task<bool> FileExistsAsync(int fileId);
}
