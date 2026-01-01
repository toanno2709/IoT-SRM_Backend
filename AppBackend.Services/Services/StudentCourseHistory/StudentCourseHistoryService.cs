using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.StudentCourseHistoryRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.Services.StudentCourseHistory;

public class StudentCourseHistoryService : IStudentCourseHistoryService
{
    private readonly IStudentCourseHistoryRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly IClassRepository _classRepository;
    private readonly IFinalProjectRepository _finalProjectRepository;

    public StudentCourseHistoryService(
        IStudentCourseHistoryRepository repository,
        IUserRepository userRepository,
        IClassRepository classRepository,
        IFinalProjectRepository finalProjectRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
        _classRepository = classRepository;
        _finalProjectRepository = finalProjectRepository;
    }

    public async Task<ResultModel<StudentCourseHistoryResponseDto>> GetByIdAsync(int historyId)
    {
        try
        {
            var history = await _repository.GetByIdWithIncludesAsync(historyId);
            if (history == null)
            {
                return new ResultModel<StudentCourseHistoryResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Student course history not found"
                };
            }

            var dto = MapToDto(history);

            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = true,
                Data = dto,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error retrieving student course history: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<StudentCourseHistoryResponseDto>> GetCurrentByStudentIdAsync(int studentId)
    {
        try
        {
            var history = await _repository.GetCurrentByStudentIdAsync(studentId);
            if (history == null)
            {
                return new ResultModel<StudentCourseHistoryResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "No current course history found for student"
                };
            }

            var dto = MapToDto(history);

            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = true,
                Data = dto,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error retrieving student course history: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<List<StudentCourseHistoryResponseDto>>> GetAllByStudentIdAsync(int studentId)
    {
        try
        {
            var histories = await _repository.GetAllByStudentIdAsync(studentId);
            var dtos = histories.Select(MapToDto).ToList();

            return new ResultModel<List<StudentCourseHistoryResponseDto>>
            {
                IsSuccess = true,
                Data = dtos,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<StudentCourseHistoryResponseDto>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error retrieving student course histories: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<List<StudentsByStatusResponseDto>>> GetStudentsByStatusAsync()
    {
        try
        {
            var statuses = new[] { "Not Started", "In Progress", "Pass", "Not Pass", "Withdrawn" };
            var result = new List<StudentsByStatusResponseDto>();

            foreach (var status in statuses)
            {
                var histories = await _repository.GetByStatusAsync(status);
                result.Add(new StudentsByStatusResponseDto
                {
                    Status = status,
                    Count = histories.Count,
                    Students = histories.Select(MapToDto).ToList()
                });
            }

            return new ResultModel<List<StudentsByStatusResponseDto>>
            {
                IsSuccess = true,
                Data = result,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<StudentsByStatusResponseDto>>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error retrieving students by status: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<StudentCourseHistoryResponseDto>> CreateAsync(StudentCourseHistoryCreateDto dto)
    {
        try
        {
            // Validate student
            var student = await _userRepository.GetByIdAsync(dto.StudentId);
            if (student == null || student.RoleId != 3)
            {
                return new ResultModel<StudentCourseHistoryResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Student not found"
                };
            }

            // Validate status
            var validStatuses = new[] { "Not Started", "In Progress", "Pass", "Not Pass", "Withdrawn" };
            if (!validStatuses.Contains(dto.Status))
            {
                return new ResultModel<StudentCourseHistoryResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Invalid status value"
                };
            }

            // Validate class if provided
            if (dto.ClassId.HasValue)
            {
                var classEntity = await _classRepository.GetByIdAsync(dto.ClassId.Value);
                if (classEntity == null)
                {
                    return new ResultModel<StudentCourseHistoryResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Class not found"
                    };
                }
            }

            // Validate final submission if provided
            if (dto.FinalSubmissionId.HasValue)
            {
                var submission = await _finalProjectRepository.GetByIdAsync(dto.FinalSubmissionId.Value);
                if (submission == null)
                {
                    return new ResultModel<StudentCourseHistoryResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Final submission not found"
                    };
                }
            }

            var history = new AppBackend.BusinessObjects.Models.StudentCourseHistory
            {
                StudentId = dto.StudentId,
                ClassId = dto.ClassId,
                Status = dto.Status,
                FinalSubmissionId = dto.FinalSubmissionId,
                FinalGrade = dto.FinalGrade,
                EvaluatedBy = dto.EvaluatedBy,
                Notes = dto.Notes,
                IsRetake = dto.IsRetake,
                EvaluatedAt = dto.FinalGrade.HasValue ? DateTime.UtcNow : null,
                CompletedAt = (dto.Status == "Pass" || dto.Status == "Not Pass") ? DateTime.UtcNow : null
            };

            var created = await _repository.CreateNewHistoryRecordAsync(history);
            
            // Reload with navigation properties
            var reloaded = await _repository.GetByIdWithIncludesAsync(created.HistoryId);
            var resultDto = MapToDto(reloaded!);

            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = true,
                Data = resultDto,
                Message = CommonMessageConstants.CREATE_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error creating student course history: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<StudentCourseHistoryResponseDto>> UpdateAsync(int historyId, StudentCourseHistoryUpdateDto dto)
    {
        try
        {
            var history = await _repository.GetByIdWithIncludesAsync(historyId);
            if (history == null)
            {
                return new ResultModel<StudentCourseHistoryResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Student course history not found"
                };
            }

            // Validate status if provided
            if (dto.Status != null)
            {
                var validStatuses = new[] { "Not Started", "In Progress", "Pass", "Not Pass", "Withdrawn" };
                if (!validStatuses.Contains(dto.Status))
                {
                    return new ResultModel<StudentCourseHistoryResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Invalid status value"
                    };
                }
                history.Status = dto.Status;
                
                // Set completed date if status changed to Pass or Not Pass
                if ((dto.Status == "Pass" || dto.Status == "Not Pass") && history.CompletedAt == null)
                {
                    history.CompletedAt = DateTime.UtcNow;
                }
            }

            if (dto.ClassId.HasValue)
            {
                var classEntity = await _classRepository.GetByIdAsync(dto.ClassId.Value);
                if (classEntity == null)
                {
                    return new ResultModel<StudentCourseHistoryResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Class not found"
                    };
                }
                history.ClassId = dto.ClassId.Value;
            }

            if (dto.FinalSubmissionId.HasValue)
            {
                var submission = await _finalProjectRepository.GetByIdAsync(dto.FinalSubmissionId.Value);
                if (submission == null)
                {
                    return new ResultModel<StudentCourseHistoryResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Final submission not found"
                    };
                }
                history.FinalSubmissionId = dto.FinalSubmissionId.Value;
            }

            if (dto.FinalGrade.HasValue)
            {
                history.FinalGrade = dto.FinalGrade.Value;
                history.EvaluatedAt = DateTime.UtcNow;
            }

            if (dto.EvaluatedBy.HasValue)
                history.EvaluatedBy = dto.EvaluatedBy.Value;

            if (dto.Notes != null)
                history.Notes = dto.Notes;

            history.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(history);
            
            // Reload with navigation properties
            var reloaded = await _repository.GetByIdWithIncludesAsync(historyId);
            var resultDto = MapToDto(reloaded!);

            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = true,
                Data = resultDto,
                Message = CommonMessageConstants.UPDATE_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error updating student course history: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<StudentCourseHistoryResponseDto>> UpdateStatusAsync(int studentId, UpdateStudentCourseStatusDto dto)
    {
        try
        {
            var history = await _repository.GetCurrentByStudentIdAsync(studentId);
            if (history == null)
            {
                return new ResultModel<StudentCourseHistoryResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "No current course history found for student"
                };
            }

            // Validate status
            var validStatuses = new[] { "Not Started", "In Progress", "Pass", "Not Pass", "Withdrawn" };
            if (!validStatuses.Contains(dto.Status))
            {
                return new ResultModel<StudentCourseHistoryResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Invalid status value"
                };
            }

            history.Status = dto.Status;
            history.Notes = dto.Notes;
            history.EvaluatedBy = dto.EvaluatedBy;
            history.EvaluatedAt = DateTime.UtcNow;
            history.UpdatedAt = DateTime.UtcNow;

            // Set completed date if status changed to Pass or Not Pass
            if ((dto.Status == "Pass" || dto.Status == "Not Pass") && history.CompletedAt == null)
            {
                history.CompletedAt = DateTime.UtcNow;
            }

            await _repository.UpdateAsync(history);
            var resultDto = MapToDto(history);

            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = true,
                Data = resultDto,
                Message = CommonMessageConstants.UPDATE_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<StudentCourseHistoryResponseDto>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error updating student course status: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<bool>> DeleteAsync(int historyId)
    {
        try
        {
            var history = await _repository.GetByIdWithIncludesAsync(historyId);
            if (history == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Student course history not found"
                };
            }

            await _repository.DeleteAsync(history);

            return new ResultModel<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = CommonMessageConstants.DELETE_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error deleting student course history: {ex.Message}"
            };
        }
    }

    private StudentCourseHistoryResponseDto MapToDto(AppBackend.BusinessObjects.Models.StudentCourseHistory history)
    {
        return new StudentCourseHistoryResponseDto
        {
            HistoryId = history.HistoryId,
            StudentId = history.StudentId,
            StudentName = history.Student?.FullName,
            StudentEmail = history.Student?.Email,
            ClassId = history.ClassId,
            ClassName = history.Class?.ClassName,
            Status = history.Status,
            FinalSubmissionId = history.FinalSubmissionId,
            FinalGrade = history.FinalGrade,
            EvaluatedAt = history.EvaluatedAt,
            EvaluatedBy = history.EvaluatedBy,
            EvaluatedByName = history.EvaluatedByUser?.FullName,
            Notes = history.Notes,
            CompletedAt = history.CompletedAt,
            IsRetake = history.IsRetake,
            IsCurrent = history.IsCurrent,
            CreatedAt = history.CreatedAt,
            UpdatedAt = history.UpdatedAt
        };
    }
}
