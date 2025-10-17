using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ApiModels.Semester;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.Semester
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _semesterRepository;
        private readonly IMapper _mapper;

        public SemesterService(ISemesterRepository semesterRepository, IMapper mapper)
        {
            _semesterRepository = semesterRepository;
            _mapper = mapper;
        }

        public async Task<ResultModel<List<SemesterResponseDto>>> GetAllSemestersAsync()
        {
            var semesters = await _semesterRepository.GetAllAsync();
            
            var semesterDtos = semesters.Select(s => new SemesterResponseDto
            {
                SemesterId = s.SemesterId,
                Code = s.Code,
                Name = s.Name,
                Year = s.Year,
                Term = s.Term,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsActive = s.IsActive,
                TotalClasses = s.Classes?.Count ?? 0
            })
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Term)
            .ToList();

            return new ResultModel<List<SemesterResponseDto>>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Semesters retrieved successfully",
                Data = semesterDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<SemesterDetailDto>> GetSemesterByIdAsync(int semesterId)
        {
            var semester = await _semesterRepository.GetByIdAsync(semesterId);
            
            if (semester == null)
            {
                return new ResultModel<SemesterDetailDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Semester not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var semesterDetail = new SemesterDetailDto
            {
                SemesterId = semester.SemesterId,
                Code = semester.Code,
                Name = semester.Name,
                Year = semester.Year,
                Term = semester.Term,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                IsActive = semester.IsActive,
                TotalClasses = semester.Classes?.Count ?? 0,
                TotalProjects = semester.Classes?.SelectMany(c => c.Groups).SelectMany(g => g.Projects).Count() ?? 0,
                Classes = semester.Classes?.Select(c => new ClassBasicInfoDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    InstructorName = c.Instructor?.FullName,
                    TotalStudents = c.ClassEnrollments?.Count ?? 0
                }).ToList()
            };

            return new ResultModel<SemesterDetailDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Semester retrieved successfully",
                Data = semesterDetail,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<SemesterResponseDto>> GetActiveSemesterAsync()
        {
            var semester = await _semesterRepository.GetActiveAsync();
            
            if (semester == null)
            {
                return new ResultModel<SemesterResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "No active semester found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            var semesterDto = new SemesterResponseDto
            {
                SemesterId = semester.SemesterId,
                Code = semester.Code,
                Name = semester.Name,
                Year = semester.Year,
                Term = semester.Term,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                IsActive = semester.IsActive,
                TotalClasses = semester.Classes?.Count ?? 0
            };

            return new ResultModel<SemesterResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Active semester retrieved successfully",
                Data = semesterDto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<List<SemesterResponseDto>>> GetSemestersByYearAsync(int year)
        {
            var semesters = await _semesterRepository.GetByYearAsync(year);
            
            var semesterDtos = semesters.Select(s => new SemesterResponseDto
            {
                SemesterId = s.SemesterId,
                Code = s.Code,
                Name = s.Name,
                Year = s.Year,
                Term = s.Term,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsActive = s.IsActive,
                TotalClasses = s.Classes?.Count ?? 0
            }).ToList();

            return new ResultModel<List<SemesterResponseDto>>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Semesters retrieved successfully",
                Data = semesterDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<SemesterResponseDto>> CreateSemesterAsync(CreateSemesterRequestDto request)
        {
            // Check if code already exists
            var codeExists = await _semesterRepository.CodeExistsAsync(request.Code);
            if (codeExists)
            {
                return new ResultModel<SemesterResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "DUPLICATE_CODE",
                    Message = $"Semester with code '{request.Code}' already exists",
                    Data = null,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // Validate dates
            if (request.StartDate.HasValue && request.EndDate.HasValue && 
                request.StartDate.Value >= request.EndDate.Value)
            {
                return new ResultModel<SemesterResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.INVALID,
                    Message = "End date must be after start date",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // If setting this as active, deactivate other semesters
            if (request.IsActive == true)
            {
                var activeSemester = await _semesterRepository.GetActiveAsync();
                if (activeSemester != null)
                {
                    activeSemester.IsActive = false;
                    await _semesterRepository.UpdateAsync(activeSemester);
                }
            }

            var newSemester = new BusinessObjects.Models.Semester
            {
                Code = request.Code,
                Name = request.Name,
                Year = request.Year,
                Term = request.Term,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive ?? false
            };

            await _semesterRepository.AddAsync(newSemester);
            await _semesterRepository.SaveChangesAsync();

            var responseDto = new SemesterResponseDto
            {
                SemesterId = newSemester.SemesterId,
                Code = newSemester.Code,
                Name = newSemester.Name,
                Year = newSemester.Year,
                Term = newSemester.Term,
                StartDate = newSemester.StartDate,
                EndDate = newSemester.EndDate,
                IsActive = newSemester.IsActive,
                TotalClasses = 0
            };

            return new ResultModel<SemesterResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Semester created successfully",
                Data = responseDto,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel<SemesterResponseDto>> UpdateSemesterAsync(int semesterId, UpdateSemesterRequestDto request)
        {
            var semester = await _semesterRepository.GetByIdAsync(semesterId);
            
            if (semester == null)
            {
                return new ResultModel<SemesterResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Semester not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
                semester.Name = request.Name;

            if (request.Year.HasValue)
                semester.Year = request.Year;

            if (!string.IsNullOrWhiteSpace(request.Term))
                semester.Term = request.Term;

            if (request.StartDate.HasValue)
                semester.StartDate = request.StartDate;

            if (request.EndDate.HasValue)
                semester.EndDate = request.EndDate;

            // Validate dates
            if (semester.StartDate.HasValue && semester.EndDate.HasValue && 
                semester.StartDate.Value >= semester.EndDate.Value)
            {
                return new ResultModel<SemesterResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.INVALID,
                    Message = "End date must be after start date",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Handle IsActive status
            if (request.IsActive.HasValue)
            {
                if (request.IsActive == true && semester.IsActive != true)
                {
                    // Deactivate other semesters
                    var activeSemester = await _semesterRepository.GetActiveAsync();
                    if (activeSemester != null && activeSemester.SemesterId != semesterId)
                    {
                        activeSemester.IsActive = false;
                        await _semesterRepository.UpdateAsync(activeSemester);
                    }
                }
                semester.IsActive = request.IsActive;
            }

            await _semesterRepository.UpdateAsync(semester);
            await _semesterRepository.SaveChangesAsync();

            var responseDto = new SemesterResponseDto
            {
                SemesterId = semester.SemesterId,
                Code = semester.Code,
                Name = semester.Name,
                Year = semester.Year,
                Term = semester.Term,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                IsActive = semester.IsActive,
                TotalClasses = semester.Classes?.Count ?? 0
            };

            return new ResultModel<SemesterResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Semester updated successfully",
                Data = responseDto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<bool>> DeleteSemesterAsync(int semesterId)
        {
            var semester = await _semesterRepository.GetByIdAsync(semesterId);
            
            if (semester == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Semester not found",
                    Data = false,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check if semester has associated classes
            if (semester.Classes != null && semester.Classes.Any())
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "HAS_DEPENDENCIES",
                    Message = "Cannot delete semester that has associated classes",
                    Data = false,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            await _semesterRepository.DeleteAsync(semester);
            await _semesterRepository.SaveChangesAsync();

            return new ResultModel<bool>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Semester deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<bool>> SetActiveSemesterAsync(int semesterId)
        {
            var semester = await _semesterRepository.GetByIdAsync(semesterId);
            
            if (semester == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Semester not found",
                    Data = false,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Deactivate all other semesters
            var allSemesters = await _semesterRepository.GetAllAsync();
            foreach (var s in allSemesters)
            {
                if (s.SemesterId == semesterId)
                {
                    s.IsActive = true;
                }
                else if (s.IsActive == true)
                {
                    s.IsActive = false;
                    await _semesterRepository.UpdateAsync(s);
                }
            }

            semester.IsActive = true;
            await _semesterRepository.UpdateAsync(semester);
            await _semesterRepository.SaveChangesAsync();

            return new ResultModel<bool>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Semester set as active successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}
