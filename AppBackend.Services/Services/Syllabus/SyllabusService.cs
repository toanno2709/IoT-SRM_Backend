using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.SyllabusRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AppBackend.Services.Services.Syllabus;

public class SyllabusService : ISyllabusService
{
    private readonly ISyllabusRepository _syllabusRepository;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<SyllabusService> _logger;

    public SyllabusService(
        ISyllabusRepository syllabusRepository,
        ICloudinaryService cloudinaryService,
        ILogger<SyllabusService> logger)
    {
        _syllabusRepository = syllabusRepository;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    public async Task<ResultModel<SyllabusResponseDto>> CreateSyllabusAsync(SyllabusCreateRequestDto request, int instructorId)
    {
        try
        {
            // Truncate datetime to seconds precision to match database column [Precision(0)]
            var createdAt = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                DateTime.UtcNow.Day,
                DateTime.UtcNow.Hour,
                DateTime.UtcNow.Minute,
                DateTime.UtcNow.Second,
                DateTimeKind.Utc
            );

            var syllabus = new BusinessObjects.Models.Syllabus
            {
                ClassId = request.ClassId,
                Title = request.Title,
                Description = request.Description,
                Version = request.Version,
                AcademicYear = request.AcademicYear,
                CreatedBy = instructorId,
                CreatedAt = createdAt,
                IsActive = true
            };

            var created = await _syllabusRepository.CreateAsync(syllabus);
            var result = await _syllabusRepository.GetByIdAsync(created.SyllabusId);

            return new ResultModel<SyllabusResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Syllabus created successfully",
                Data = MapToResponseDto(result!),
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SyllabusResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error creating syllabus: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SyllabusResponseDto>> GetSyllabusByIdAsync(int syllabusId)
    {
        try
        {
            var syllabus = await _syllabusRepository.GetByIdAsync(syllabusId);

            if (syllabus == null)
            {
                return new ResultModel<SyllabusResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Syllabus not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            return new ResultModel<SyllabusResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Syllabus retrieved successfully",
                Data = MapToResponseDto(syllabus),
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SyllabusResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error retrieving syllabus: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<List<SyllabusListItemDto>>> GetSyllabusesByClassIdAsync(int classId)
    {
        try
        {
            var syllabuses = await _syllabusRepository.GetByClassIdAsync(classId);

            var result = syllabuses.Select(s => new SyllabusListItemDto
            {
                SyllabusId = s.SyllabusId,
                ClassId = s.ClassId,
                ClassName = s.Class?.ClassName,
                Title = s.Title,
                Version = s.Version,
                AcademicYear = s.AcademicYear,
                CreatedByName = s.Creator?.FullName,
                CreatedAt = s.CreatedAt,
                IsActive = s.IsActive,
                FileCount = s.SyllabusFiles?.Count ?? 0
            }).ToList();

            return new ResultModel<List<SyllabusListItemDto>>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Found {result.Count} syllabuses",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<SyllabusListItemDto>>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error retrieving syllabuses: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<List<SyllabusListItemDto>>> GetSyllabusesByInstructorIdAsync(int instructorId)
    {
        try
        {
            var syllabuses = await _syllabusRepository.GetByInstructorIdAsync(instructorId);

            var result = syllabuses.Select(s => new SyllabusListItemDto
            {
                SyllabusId = s.SyllabusId,
                ClassId = s.ClassId,
                ClassName = s.Class?.ClassName,
                Title = s.Title,
                Version = s.Version,
                AcademicYear = s.AcademicYear,
                CreatedByName = s.Creator?.FullName,
                CreatedAt = s.CreatedAt,
                IsActive = s.IsActive,
                FileCount = s.SyllabusFiles?.Count ?? 0
            }).ToList();

            return new ResultModel<List<SyllabusListItemDto>>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Found {result.Count} syllabuses",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<SyllabusListItemDto>>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error retrieving syllabuses: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SyllabusResponseDto>> UpdateSyllabusAsync(int syllabusId, SyllabusUpdateRequestDto request, int instructorId)
    {
        try
        {
            if (!await _syllabusRepository.IsInstructorOwnerAsync(syllabusId, instructorId))
            {
                return new ResultModel<SyllabusResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "You don't have permission to update this syllabus",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var syllabus = await _syllabusRepository.GetByIdAsync(syllabusId);
            if (syllabus == null)
            {
                return new ResultModel<SyllabusResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Syllabus not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (request.Title != null) syllabus.Title = request.Title;
            if (request.Description != null) syllabus.Description = request.Description;
            if (request.Version != null) syllabus.Version = request.Version;
            if (request.AcademicYear != null) syllabus.AcademicYear = request.AcademicYear;
            if (request.IsActive.HasValue) syllabus.IsActive = request.IsActive;
            
            // Truncate datetime to seconds precision to match database column [Precision(0)]
            syllabus.UpdatedAt = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                DateTime.UtcNow.Day,
                DateTime.UtcNow.Hour,
                DateTime.UtcNow.Minute,
                DateTime.UtcNow.Second,
                DateTimeKind.Utc
            );

            await _syllabusRepository.UpdateAsync(syllabus);

            var updated = await _syllabusRepository.GetByIdAsync(syllabusId);

            return new ResultModel<SyllabusResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Syllabus updated successfully",
                Data = MapToResponseDto(updated!),
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SyllabusResponseDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error updating syllabus: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<bool>> DeleteSyllabusAsync(int syllabusId, int instructorId)
    {
        try
        {
            if (!await _syllabusRepository.IsInstructorOwnerAsync(syllabusId, instructorId))
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "You don't have permission to delete this syllabus",
                    Data = false,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            await _syllabusRepository.DeleteAsync(syllabusId);

            return new ResultModel<bool>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Syllabus deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error deleting syllabus: {ex.Message}",
                Data = false,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SyllabusFileDto>> UploadFileAsync(
        int syllabusId, 
        IFormFile file, 
        string? description, 
        int? displayOrder, 
        int instructorId)
    {
        try
        {
            _logger.LogInformation("Starting file upload for syllabus {SyllabusId} by instructor {InstructorId}", 
                syllabusId, instructorId);

            // Check permission
            if (!await _syllabusRepository.IsInstructorOwnerAsync(syllabusId, instructorId))
            {
                _logger.LogWarning("Instructor {InstructorId} does not have permission to upload files to syllabus {SyllabusId}", 
                    instructorId, syllabusId);
                
                return new ResultModel<SyllabusFileDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "You don't have permission to upload files to this syllabus",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload attempt with empty file for syllabus {SyllabusId}", syllabusId);
                
                return new ResultModel<SyllabusFileDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.BAD_REQUEST,
                    Message = "File is required",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // Validate file size (100MB max)
            const long maxFileSize = 104857600; // 100MB
            if (file.Length > maxFileSize)
            {
                _logger.LogWarning("File size {FileSize} exceeds maximum allowed size for syllabus {SyllabusId}", 
                    file.Length, syllabusId);
                
                return new ResultModel<SyllabusFileDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.BAD_REQUEST,
                    Message = $"File size exceeds maximum allowed size of 100MB. Uploaded: {file.Length / (1024 * 1024)}MB",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            _logger.LogInformation("Uploading file {FileName} (Size: {FileSize} bytes, Type: {ContentType}) to Cloudinary", 
                file.FileName, file.Length, file.ContentType);

            // Upload file to Cloudinary
            var uploadResult = await _cloudinaryService.UploadFileAsync(file, "syllabuses");
            if (uploadResult == null)
            {
                _logger.LogError("Cloudinary upload failed for file {FileName} in syllabus {SyllabusId}", 
                    file.FileName, syllabusId);
                
                return new ResultModel<SyllabusFileDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.ERROR,
                    Message = "Failed to upload file to cloud storage",
                    Data = null,
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            _logger.LogInformation("File uploaded to Cloudinary successfully. URL: {FileUrl}", uploadResult.SecureUrl);

            // Truncate datetime to seconds precision to match database column [Precision(0)]
            var uploadedAt = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                DateTime.UtcNow.Day,
                DateTime.UtcNow.Hour,
                DateTime.UtcNow.Minute,
                DateTime.UtcNow.Second,
                DateTimeKind.Utc
            );

            // Create file record in database
            var syllabusFile = new SyllabusFile
            {
                SyllabusId = syllabusId,
                FileName = file.FileName,
                FileUrl = uploadResult.SecureUrl,
                FileType = file.ContentType,
                FileSize = file.Length,
                Description = description,
                UploadedBy = instructorId,
                UploadedAt = uploadedAt,
                DisplayOrder = displayOrder ?? 0
            };

            var created = await _syllabusRepository.AddFileAsync(syllabusFile);
            var result = await _syllabusRepository.GetFileByIdAsync(created.FileId);

            _logger.LogInformation("File record created in database with FileId: {FileId}", result!.FileId);

            return new ResultModel<SyllabusFileDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "File uploaded successfully",
                Data = MapToFileDto(result),
                StatusCode = StatusCodes.Status201Created
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to syllabus {SyllabusId}: {ErrorMessage}. Inner exception: {InnerException}", 
                syllabusId, ex.Message, ex.InnerException?.Message);
            
            return new ResultModel<SyllabusFileDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error uploading file: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<List<SyllabusFileDto>>> GetFilesBySyllabusIdAsync(int syllabusId)
    {
        try
        {
            var files = await _syllabusRepository.GetFilesBySyllabusIdAsync(syllabusId);

            var result = files.Select(MapToFileDto).ToList();

            return new ResultModel<List<SyllabusFileDto>>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Found {result.Count} files",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<SyllabusFileDto>>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error retrieving files: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<SyllabusFileDto>> UpdateFileAsync(int fileId, SyllabusFileUpdateRequestDto request, int instructorId)
    {
        try
        {
            var file = await _syllabusRepository.GetFileByIdAsync(fileId);
            if (file == null)
            {
                return new ResultModel<SyllabusFileDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "File not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (!await _syllabusRepository.IsInstructorOwnerAsync(file.SyllabusId, instructorId))
            {
                return new ResultModel<SyllabusFileDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "You don't have permission to update this file",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (request.FileName != null) file.FileName = request.FileName;
            if (request.Description != null) file.Description = request.Description;
            if (request.DisplayOrder.HasValue) file.DisplayOrder = request.DisplayOrder;

            await _syllabusRepository.UpdateFileAsync(file);

            var updated = await _syllabusRepository.GetFileByIdAsync(fileId);

            return new ResultModel<SyllabusFileDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "File updated successfully",
                Data = MapToFileDto(updated!),
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<SyllabusFileDto>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error updating file: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<bool>> DeleteFileAsync(int fileId, int instructorId)
    {
        try
        {
            var file = await _syllabusRepository.GetFileByIdAsync(fileId);
            if (file == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "File not found",
                    Data = false,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (!await _syllabusRepository.IsInstructorOwnerAsync(file.SyllabusId, instructorId))
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.FORBIDDEN,
                    Message = "You don't have permission to delete this file",
                    Data = false,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            await _syllabusRepository.DeleteFileAsync(fileId);

            return new ResultModel<bool>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "File deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = CommonMessageConstants.ERROR,
                Message = $"Error deleting file: {ex.Message}",
                Data = false,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    #region Private Helpers

    private SyllabusResponseDto MapToResponseDto(BusinessObjects.Models.Syllabus syllabus)
    {
        return new SyllabusResponseDto
        {
            SyllabusId = syllabus.SyllabusId,
            ClassId = syllabus.ClassId,
            ClassName = syllabus.Class?.ClassName,
            Title = syllabus.Title,
            Description = syllabus.Description,
            Version = syllabus.Version,
            AcademicYear = syllabus.AcademicYear,
            CreatedBy = syllabus.CreatedBy,
            CreatedByName = syllabus.Creator?.FullName,
            CreatedAt = syllabus.CreatedAt,
            UpdatedAt = syllabus.UpdatedAt,
            IsActive = syllabus.IsActive,
            FileCount = syllabus.SyllabusFiles?.Count ?? 0,
            Files = syllabus.SyllabusFiles?.Select(MapToFileDto).ToList() ?? new List<SyllabusFileDto>()
        };
    }

    private SyllabusFileDto MapToFileDto(SyllabusFile file)
    {
        return new SyllabusFileDto
        {
            FileId = file.FileId,
            SyllabusId = file.SyllabusId,
            FileName = file.FileName,
            FileUrl = file.FileUrl,
            FileType = file.FileType,
            FileSize = file.FileSize,
            Description = file.Description,
            UploadedBy = file.UploadedBy,
            UploadedByName = file.Uploader?.FullName,
            UploadedAt = file.UploadedAt,
            DisplayOrder = file.DisplayOrder
        };
    }

    #endregion
}
