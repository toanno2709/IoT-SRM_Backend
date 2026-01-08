using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.StudentCourseHistoryRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Repositories.Repositories.FinalProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;

namespace AppBackend.Services.Services.StudentCourseHistory;

public class StudentCourseHistoryService : IStudentCourseHistoryService
{
    private readonly IStudentCourseHistoryRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IFinalProjectRepository _finalProjectRepository;
    private readonly IotShowroomContext _context;

    public StudentCourseHistoryService(
        IStudentCourseHistoryRepository repository,
        IUserRepository userRepository,
        ISemesterRepository semesterRepository,
        IFinalProjectRepository finalProjectRepository,
        IotShowroomContext context)
    {
        _repository = repository;
        _userRepository = userRepository;
        _semesterRepository = semesterRepository;
        _finalProjectRepository = finalProjectRepository;
        _context = context;
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
            }

            // Validate semester if provided
            if (dto.SemesterId.HasValue)
            {
                var semester = await _semesterRepository.GetByIdAsync(dto.SemesterId.Value);
                if (semester == null)
                {
                    return new ResultModel<StudentCourseHistoryResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Semester not found"
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
                SemesterId = dto.SemesterId,
                Status = dto.Status ?? "Not Started",
                FinalSubmissionId = dto.FinalSubmissionId,
                FinalGrade = dto.FinalGrade,
                AverageGradeFromOtherInstructors = dto.AverageGradeFromOtherInstructors,
                Notes = dto.Notes,
                IsRetake = dto.IsRetake ?? false,
                IsCurrent = false, // Default to false, will be set by background service
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

            if (dto.SemesterId.HasValue)
            {
                var semester = await _semesterRepository.GetByIdAsync(dto.SemesterId.Value);
                if (semester == null)
                {
                    return new ResultModel<StudentCourseHistoryResponseDto>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Semester not found"
                    };
                }
                history.SemesterId = dto.SemesterId.Value;
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
                // Only auto-set EvaluatedAt if not explicitly provided
                if (!dto.EvaluatedAt.HasValue)
                {
                    history.EvaluatedAt = DateTime.UtcNow;
                }
            }

            if (dto.AverageGradeFromOtherInstructors.HasValue)
                history.AverageGradeFromOtherInstructors = dto.AverageGradeFromOtherInstructors.Value;

            if (dto.Notes != null)
                history.Notes = dto.Notes;

            if (dto.IsRetake.HasValue)
                history.IsRetake = dto.IsRetake.Value;

            if (dto.IsCurrent.HasValue)
                history.IsCurrent = dto.IsCurrent.Value;

            if (dto.CompletedAt.HasValue)
                history.CompletedAt = dto.CompletedAt.Value;

            if (dto.EvaluatedAt.HasValue)
                history.EvaluatedAt = dto.EvaluatedAt.Value;

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

    public async Task<ResultModel<UpdateCurrentFlagsResultDto>> UpdateAllCurrentFlagsAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var executedAt = DateTime.UtcNow;

            // Get all StudentCourseHistory records with semester info
            var allHistories = await _context.StudentCourseHistories
                .Include(sch => sch.Semester)
                .Include(sch => sch.Student)
                .Where(sch => sch.SemesterId != null)
                .ToListAsync();

            var updatedCount = 0;
            var unchangedCount = 0;
            var updateDetails = new List<CurrentFlagUpdateDetailDto>();

            foreach (var history in allHistories)
            {
                if (history.Semester == null)
                {
                    unchangedCount++;
                    continue;
                }

                var semester = history.Semester;
                var previousIsCurrent = history.IsCurrent ?? false;

                // Determine if this history should be current
                bool shouldBeCurrent = false;
                string reason = "";

                if (semester.StartDate.HasValue && semester.EndDate.HasValue)
                {
                    shouldBeCurrent = today >= semester.StartDate.Value && today <= semester.EndDate.Value;
                    
                    if (shouldBeCurrent)
                    {
                        reason = $"Current date ({today}) is within semester date range ({semester.StartDate.Value} to {semester.EndDate.Value})";
                    }
                    else if (today < semester.StartDate.Value)
                    {
                        reason = $"Current date ({today}) is before semester start date ({semester.StartDate.Value})";
                    }
                    else
                    {
                        reason = $"Current date ({today}) is after semester end date ({semester.EndDate.Value})";
                    }
                }
                else
                {
                    reason = "Semester does not have start/end dates";
                }

                // Update if different from current state
                if (previousIsCurrent != shouldBeCurrent)
                {
                    history.IsCurrent = shouldBeCurrent;
                    history.UpdatedAt = DateTime.UtcNow;
                    updatedCount++;

                    updateDetails.Add(new CurrentFlagUpdateDetailDto
                    {
                        HistoryId = history.HistoryId,
                        StudentId = history.StudentId,
                        StudentName = history.Student?.FullName,
                        SemesterId = history.SemesterId,
                        SemesterName = semester.Name,
                        PreviousIsCurrent = previousIsCurrent,
                        NewIsCurrent = shouldBeCurrent,
                        Reason = reason
                    });
                }
                else
                {
                    unchangedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            var result = new UpdateCurrentFlagsResultDto
            {
                TotalRecordsChecked = allHistories.Count,
                RecordsUpdated = updatedCount,
                RecordsUnchanged = unchangedCount,
                ExecutedAt = executedAt,
                Message = $"Successfully updated {updatedCount} out of {allHistories.Count} records. {unchangedCount} records unchanged.",
                UpdateDetails = updateDetails
            };

            return new ResultModel<UpdateCurrentFlagsResultDto>
            {
                IsSuccess = true,
                Data = result,
                Message = CommonMessageConstants.SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<UpdateCurrentFlagsResultDto>
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = $"Error updating IsCurrent flags: {ex.Message}"
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
            SemesterId = history.SemesterId,
            SemesterName = history.Semester?.Name,
            Status = history.Status,
            FinalSubmissionId = history.FinalSubmissionId,
            FinalGrade = history.FinalGrade,
            AverageGradeFromOtherInstructors = history.AverageGradeFromOtherInstructors,
            EvaluatedAt = history.EvaluatedAt,
            Notes = history.Notes,
            CompletedAt = history.CompletedAt,
            IsRetake = history.IsRetake,
            IsCurrent = history.IsCurrent,
            CreatedAt = history.CreatedAt,
            UpdatedAt = history.UpdatedAt
        };
    }
}
