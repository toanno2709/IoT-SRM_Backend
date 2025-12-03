using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.Services.Services.Syllabus;

public interface ISyllabusService
{
    // Syllabus CRUD
    Task<ResultModel<SyllabusResponseDto>> CreateSyllabusAsync(SyllabusCreateRequestDto request, int instructorId);
    Task<ResultModel<SyllabusResponseDto>> GetSyllabusByIdAsync(int syllabusId);
    Task<ResultModel<List<SyllabusListItemDto>>> GetSyllabusesByClassIdAsync(int classId);
    Task<ResultModel<List<SyllabusListItemDto>>> GetSyllabusesByInstructorIdAsync(int instructorId);
    Task<ResultModel<SyllabusResponseDto>> UpdateSyllabusAsync(int syllabusId, SyllabusUpdateRequestDto request, int instructorId);
    Task<ResultModel<bool>> DeleteSyllabusAsync(int syllabusId, int instructorId);

    // File management
    Task<ResultModel<SyllabusFileDto>> UploadFileAsync(SyllabusFileUploadRequestDto request, int instructorId);
    Task<ResultModel<List<SyllabusFileDto>>> GetFilesBySyllabusIdAsync(int syllabusId);
    Task<ResultModel<SyllabusFileDto>> UpdateFileAsync(int fileId, SyllabusFileUpdateRequestDto request, int instructorId);
    Task<ResultModel<bool>> DeleteFileAsync(int fileId, int instructorId);
}
