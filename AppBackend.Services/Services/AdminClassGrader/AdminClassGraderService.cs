using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClassGraderEntity = AppBackend.BusinessObjects.Models.ClassGrader;
using NotificationEntity = AppBackend.BusinessObjects.Models.Notification;

namespace AppBackend.Services.Services.AdminClassGrader;

public class AdminClassGraderService : IAdminClassGraderService
{
    private readonly IotShowroomContext _context;
    private readonly ILogger<AdminClassGraderService> _logger;

    public AdminClassGraderService(
        IotShowroomContext context,
        ILogger<AdminClassGraderService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResultModel<List<ClassGraderDetailDto>>> GetClassGradersAsync(int classId)
    {
        try
        {
            _logger.LogInformation("Getting graders for class {ClassId}", classId);

            // Verify class exists
            var classExists = await _context.Classes.AnyAsync(c => c.ClassId == classId);
            if (!classExists)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Class not found",
                    StatusCodes.Status404NotFound
                );
            }

            var graders = await _context.ClassGraders
                .Where(cg => cg.ClassId == classId)
                .Include(cg => cg.Class)
                .Include(cg => cg.Instructor)
                .Include(cg => cg.AssignedByNavigation)
                .Select(cg => new
                {
                    ClassGrader = cg,
                    FinalSubmissions = _context.FinalProjectSubmissions
                        .Where(fps => fps.Project.Group!.ClassId == classId)
                        .Select(fps => new
                        {
                            fps.FinalSubmissionId,
                            HasGrade = fps.FinalSubmissionGrades.Any(fsg => fsg.InstructorId == cg.InstructorId)
                        })
                        .ToList()
                })
                .ToListAsync();

            var result = graders.Select(g =>
            {
                var totalSubmissions = g.FinalSubmissions.Count;
                var gradedCount = g.FinalSubmissions.Count(fs => fs.HasGrade);
                var pendingCount = totalSubmissions - gradedCount;

                return new ClassGraderDetailDto
                {
                    GraderId = g.ClassGrader.GraderId,
                    ClassId = g.ClassGrader.ClassId,
                    ClassName = g.ClassGrader.Class.ClassName,
                    ClassDescription = g.ClassGrader.Class.Description,
                    InstructorId = g.ClassGrader.InstructorId,
                    InstructorName = g.ClassGrader.Instructor.FullName,
                    InstructorEmail = g.ClassGrader.Instructor.Email,
                    AssignedAt = g.ClassGrader.AssignedAt,
                    AssignedBy = g.ClassGrader.AssignedBy,
                    AssignedByName = g.ClassGrader.AssignedByNavigation?.FullName,
                    IsActive = g.ClassGrader.IsActive,
                    TotalFinalSubmissions = totalSubmissions,
                    GradedByThisInstructor = gradedCount,
                    PendingGrades = pendingCount
                };
            }).ToList();

            return new ResultModel<List<ClassGraderDetailDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} grader(s) for class",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting graders for class {ClassId}", classId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting class graders: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ClassGraderDetailDto>> AssignGraderAsync(
        AssignClassGraderRequestDto request, 
        int adminId)
    {
        try
        {
            _logger.LogInformation(
                "Admin {AdminId} assigning instructor {InstructorId} to grade class {ClassId}",
                adminId, request.InstructorId, request.ClassId);

            // Verify class exists
            var classEntity = await _context.Classes
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.ClassId == request.ClassId);

            if (classEntity == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Class not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Verify instructor exists and has instructor role
            var instructor = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == request.InstructorId && u.RoleId == 2);

            if (instructor == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Instructor not found or user is not an instructor",
                    StatusCodes.Status404NotFound
                );
            }

            // Check if already assigned
            var existingAssignment = await _context.ClassGraders
                .FirstOrDefaultAsync(cg => cg.ClassId == request.ClassId && 
                                           cg.InstructorId == request.InstructorId);

            if (existingAssignment != null)
            {
                // If exists but inactive, reactivate it
                if (!existingAssignment.IsActive)
                {
                    existingAssignment.IsActive = true;
                    existingAssignment.AssignedAt = DateTime.UtcNow;
                    existingAssignment.AssignedBy = adminId;
                    await _context.SaveChangesAsync();

                    return await GetGraderDetailAsync(existingAssignment.GraderId);
                }

                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    "Instructor is already assigned to grade this class",
                    StatusCodes.Status409Conflict
                );
            }

            // Create new assignment
            var newGrader = new ClassGraderEntity
            {
                ClassId = request.ClassId,
                InstructorId = request.InstructorId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = adminId,
                IsActive = true
            };

            _context.ClassGraders.Add(newGrader);
            await _context.SaveChangesAsync();

            // Send notification to instructor
            var notification = new NotificationEntity
            {
                UserId = request.InstructorId,
                Title = "Assigned as Grader",
                Message = $"You have been assigned to grade final projects in class '{classEntity.ClassName}'",
                Type = "grader_assigned",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully assigned instructor {InstructorId} to grade class {ClassId}",
                request.InstructorId, request.ClassId);

            return await GetGraderDetailAsync(newGrader.GraderId);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning grader to class");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error assigning grader: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<bool>> RemoveGraderAsync(int graderId, int adminId)
    {
        try
        {
            _logger.LogInformation("Admin {AdminId} removing grader {GraderId}", adminId, graderId);

            var grader = await _context.ClassGraders
                .Include(cg => cg.Instructor)
                .Include(cg => cg.Class)
                .FirstOrDefaultAsync(cg => cg.GraderId == graderId);

            if (grader == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Grader assignment not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Check if instructor has submitted any grades
            var hasGrades = await _context.FinalSubmissionGrades
                .AnyAsync(fsg => fsg.InstructorId == grader.InstructorId &&
                                 fsg.FinalSubmission.Project.Group!.ClassId == grader.ClassId);

            if (hasGrades)
            {
                // Don't delete, just deactivate
                grader.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Deactivated grader {GraderId} instead of deleting (has existing grades)",
                    graderId);

                return new ResultModel<bool>
                {
                    IsSuccess = true,
                    Message = "Grader assignment deactivated successfully (has existing grades)",
                    Data = true,
                    StatusCode = StatusCodes.Status200OK
                };
            }

            // No grades submitted, safe to delete
            _context.ClassGraders.Remove(grader);
            await _context.SaveChangesAsync();

            // Send notification to instructor
            var notification = new NotificationEntity
            {
                UserId = grader.InstructorId,
                Title = "Grader Assignment Removed",
                Message = $"You have been removed as a grader for class '{grader.Class.ClassName}'",
                Type = "grader_removed",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully removed grader {GraderId}", graderId);

            return new ResultModel<bool>
            {
                IsSuccess = true,
                Message = "Grader assignment removed successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing grader {GraderId}", graderId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error removing grader: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ClassGraderDetailDto>> UpdateGraderStatusAsync(
        int graderId, 
        bool isActive, 
        int adminId)
    {
        try
        {
            _logger.LogInformation(
                "Admin {AdminId} updating grader {GraderId} status to {Status}",
                adminId, graderId, isActive);

            var grader = await _context.ClassGraders
                .Include(cg => cg.Instructor)
                .Include(cg => cg.Class)
                .FirstOrDefaultAsync(cg => cg.GraderId == graderId);

            if (grader == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Grader assignment not found",
                    StatusCodes.Status404NotFound
                );
            }

            grader.IsActive = isActive;
            await _context.SaveChangesAsync();

            // Send notification to instructor
            var notification = new NotificationEntity
            {
                UserId = grader.InstructorId,
                Title = isActive ? "Grader Assignment Activated" : "Grader Assignment Deactivated",
                Message = $"Your grader assignment for class '{grader.Class.ClassName}' has been {(isActive ? "activated" : "deactivated")}",
                Type = isActive ? "grader_activated" : "grader_deactivated",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully updated grader {GraderId} status to {Status}",
                graderId, isActive);

            return await GetGraderDetailAsync(graderId);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating grader {GraderId} status", graderId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error updating grader status: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<List<ClassGraderSummaryDto>>> GetAllGraderAssignmentsAsync(
        int? instructorId = null,
        int? classId = null,
        bool? isActive = null)
    {
        try
        {
            _logger.LogInformation(
                "Getting all grader assignments (InstructorId: {InstructorId}, ClassId: {ClassId}, IsActive: {IsActive})",
                instructorId, classId, isActive);

            var query = _context.ClassGraders
                .Include(cg => cg.Class)
                    .ThenInclude(c => c.Semester)
                .Include(cg => cg.Instructor)
                .AsQueryable();

            if (instructorId.HasValue)
            {
                query = query.Where(cg => cg.InstructorId == instructorId.Value);
            }

            if (classId.HasValue)
            {
                query = query.Where(cg => cg.ClassId == classId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(cg => cg.IsActive == isActive.Value);
            }

            var graders = await query
                .Select(cg => new
                {
                    ClassGrader = cg,
                    ApprovedProjects = _context.Projects
                        .Count(p => p.Group!.ClassId == cg.ClassId && 
                                   p.Status != null && 
                                   p.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)),
                    FinalSubmissions = _context.FinalProjectSubmissions
                        .Where(fps => fps.Project.Group!.ClassId == cg.ClassId)
                        .Select(fps => new
                        {
                            fps.FinalSubmissionId,
                            HasGrade = fps.FinalSubmissionGrades.Any(fsg => fsg.InstructorId == cg.InstructorId)
                        })
                        .ToList()
                })
                .ToListAsync();

            var result = graders.Select(g =>
            {
                var totalSubmissions = g.FinalSubmissions.Count;
                var gradedCount = g.FinalSubmissions.Count(fs => fs.HasGrade);
                var pendingCount = totalSubmissions - gradedCount;
                var completionPercentage = totalSubmissions > 0 
                    ? Math.Round((decimal)gradedCount / totalSubmissions * 100, 2) 
                    : 0;

                return new ClassGraderSummaryDto
                {
                    GraderId = g.ClassGrader.GraderId,
                    ClassId = g.ClassGrader.ClassId,
                    ClassName = g.ClassGrader.Class.ClassName,
                    SemesterId = g.ClassGrader.Class.SemesterId,
                    SemesterName = g.ClassGrader.Class.Semester?.Name,
                    InstructorId = g.ClassGrader.InstructorId,
                    InstructorName = g.ClassGrader.Instructor.FullName,
                    InstructorEmail = g.ClassGrader.Instructor.Email,
                    IsActive = g.ClassGrader.IsActive,
                    AssignedAt = g.ClassGrader.AssignedAt,
                    TotalApprovedProjects = g.ApprovedProjects,
                    TotalFinalSubmissions = totalSubmissions,
                    GradedCount = gradedCount,
                    PendingCount = pendingCount,
                    CompletionPercentage = completionPercentage
                };
            })
            .OrderByDescending(g => g.IsActive)
            .ThenBy(g => g.ClassName)
            .ThenBy(g => g.InstructorName)
            .ToList();

            return new ResultModel<List<ClassGraderSummaryDto>>
            {
                IsSuccess = true,
                Message = $"Found {result.Count} grader assignment(s)",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all grader assignments");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting grader assignments: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<ClassGradingStatisticsDto>> GetClassGradingStatisticsAsync(int classId)
    {
        try
        {
            _logger.LogInformation("Getting grading statistics for class {ClassId}", classId);

            var classEntity = await _context.Classes
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (classEntity == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Class not found",
                    StatusCodes.Status404NotFound
                );
            }

            // Get all graders
            var graders = await _context.ClassGraders
                .Where(cg => cg.ClassId == classId)
                .Include(cg => cg.Instructor)
                .ToListAsync();

            var totalGraders = graders.Count;
            var activeGraders = graders.Count(g => g.IsActive);

            // Get project statistics
            var projects = await _context.Projects
                .Where(p => p.Group!.ClassId == classId)
                .Include(p => p.FinalProjectSubmission)
                    .ThenInclude(fps => fps!.FinalSubmissionGrades)
                .ToListAsync();

            var totalProjects = projects.Count;
            var approvedProjects = projects.Count(p => 
                p.Status != null && p.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
            var projectsWithSubmission = projects.Count(p => p.FinalProjectSubmission != null);

            // Calculate grading statistics
            var submittedProjects = projects.Where(p => p.FinalProjectSubmission != null).ToList();
            var totalGradesSubmitted = submittedProjects
                .Sum(p => p.FinalProjectSubmission!.FinalSubmissionGrades.Count);

            var fullyGraded = submittedProjects.Count(p => 
                activeGraders > 0 && 
                p.FinalProjectSubmission!.FinalSubmissionGrades.Count == activeGraders);
            
            var partiallyGraded = submittedProjects.Count(p => 
                p.FinalProjectSubmission!.FinalSubmissionGrades.Count > 0 && 
                p.FinalProjectSubmission.FinalSubmissionGrades.Count < activeGraders);
            
            var ungraded = submittedProjects.Count(p => 
                p.FinalProjectSubmission!.FinalSubmissionGrades.Count == 0);

            var allGrades = submittedProjects
                .SelectMany(p => p.FinalProjectSubmission!.FinalSubmissionGrades)
                .Select(fsg => fsg.Grade)
                .ToList();

            var averageGrade = allGrades.Any() ? allGrades.Average() : 0;
            var highestGrade = allGrades.Any() ? allGrades.Max() : (decimal?)null;
            var lowestGrade = allGrades.Any() ? allGrades.Min() : (decimal?)null;

            var expectedTotalGrades = projectsWithSubmission * activeGraders;
            var gradingCompletion = expectedTotalGrades > 0
                ? Math.Round((decimal)totalGradesSubmitted / expectedTotalGrades * 100, 2)
                : 0;

            // Get grader workloads
            var graderWorkloads = new List<GraderWorkloadDto>();
            foreach (var grader in graders)
            {
                var instructorGrades = submittedProjects
                    .Select(p => p.FinalProjectSubmission!.FinalSubmissionGrades
                        .FirstOrDefault(fsg => fsg.InstructorId == grader.InstructorId))
                    .Where(g => g != null)
                    .ToList();

                var gradedCount = instructorGrades.Count;
                var pendingCount = projectsWithSubmission - gradedCount;
                var avgGrade = instructorGrades.Any() 
                    ? instructorGrades.Average(g => g!.Grade) 
                    : (decimal?)null;
                var completion = projectsWithSubmission > 0
                    ? Math.Round((decimal)gradedCount / projectsWithSubmission * 100, 2)
                    : 0;

                graderWorkloads.Add(new GraderWorkloadDto
                {
                    InstructorId = grader.InstructorId,
                    InstructorName = grader.Instructor.FullName,
                    InstructorEmail = grader.Instructor.Email,
                    IsActive = grader.IsActive,
                    TotalAssignedSubmissions = projectsWithSubmission,
                    GradedCount = gradedCount,
                    PendingCount = pendingCount,
                    CompletionPercentage = completion,
                    AverageGradeGiven = avgGrade
                });
            }

            var result = new ClassGradingStatisticsDto
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                SemesterId = classEntity.SemesterId,
                SemesterName = classEntity.Semester?.Name,
                TotalAssignedGraders = totalGraders,
                ActiveGraders = activeGraders,
                GraderWorkloads = graderWorkloads.OrderByDescending(g => g.IsActive)
                    .ThenBy(g => g.InstructorName).ToList(),
                TotalProjects = totalProjects,
                ApprovedProjects = approvedProjects,
                ProjectsWithFinalSubmission = projectsWithSubmission,
                TotalGradesSubmitted = totalGradesSubmitted,
                FullyGradedProjects = fullyGraded,
                PartiallyGradedProjects = partiallyGraded,
                UngradedProjects = ungraded,
                AverageGrade = Math.Round(averageGrade, 2),
                HighestGrade = highestGrade,
                LowestGrade = lowestGrade,
                GradingCompletionPercentage = gradingCompletion
            };

            return new ResultModel<ClassGradingStatisticsDto>
            {
                IsSuccess = true,
                Message = "Grading statistics retrieved successfully",
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting grading statistics for class {ClassId}", classId);
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error getting grading statistics: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    public async Task<ResultModel<BulkAssignGradersResponseDto>> BulkAssignGradersAsync(
        BulkAssignGradersRequestDto request,
        int adminId)
    {
        try
        {
            _logger.LogInformation(
                "Admin {AdminId} bulk assigning {Count} graders to class {ClassId}",
                adminId, request.InstructorIds.Count, request.ClassId);

            // Verify class exists
            var classEntity = await _context.Classes
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.ClassId == request.ClassId);

            if (classEntity == null)
            {
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    "Class not found",
                    StatusCodes.Status404NotFound
                );
            }

            var results = new List<BulkAssignResultDto>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var instructorId in request.InstructorIds.Distinct())
            {
                try
                {
                    // Verify instructor
                    var instructor = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == instructorId && u.RoleId == 2);

                    if (instructor == null)
                    {
                        results.Add(new BulkAssignResultDto
                        {
                            InstructorId = instructorId,
                            InstructorName = null,
                            Success = false,
                            Message = "Instructor not found or user is not an instructor"
                        });
                        failureCount++;
                        continue;
                    }

                    // Check if already assigned
                    var existing = await _context.ClassGraders
                        .FirstOrDefaultAsync(cg => cg.ClassId == request.ClassId && 
                                                   cg.InstructorId == instructorId);

                    if (existing != null)
                    {
                        if (!existing.IsActive)
                        {
                            // Reactivate
                            existing.IsActive = true;
                            existing.AssignedAt = DateTime.UtcNow;
                            existing.AssignedBy = adminId;
                            
                            results.Add(new BulkAssignResultDto
                            {
                                InstructorId = instructorId,
                                InstructorName = instructor.FullName,
                                Success = true,
                                Message = "Reactivated existing assignment",
                                GraderId = existing.GraderId
                            });
                            successCount++;
                        }
                        else
                        {
                            results.Add(new BulkAssignResultDto
                            {
                                InstructorId = instructorId,
                                InstructorName = instructor.FullName,
                                Success = false,
                                Message = "Already assigned as active grader"
                            });
                            failureCount++;
                        }
                        continue;
                    }

                    // Create new assignment
                    var newGrader = new ClassGraderEntity
                    {
                        ClassId = request.ClassId,
                        InstructorId = instructorId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = adminId,
                        IsActive = true
                    };

                    _context.ClassGraders.Add(newGrader);
                    await _context.SaveChangesAsync();

                    // Send notification
                    var notification = new NotificationEntity
                    {
                        UserId = instructorId,
                        Title = "Assigned as Grader",
                        Message = $"You have been assigned to grade final projects in class '{classEntity.ClassName}'",
                        Type = "grader_assigned",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notification);

                    results.Add(new BulkAssignResultDto
                    {
                        InstructorId = instructorId,
                        InstructorName = instructor.FullName,
                        Success = true,
                        Message = "Successfully assigned",
                        GraderId = newGrader.GraderId
                    });
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "Failed to assign instructor {InstructorId} to class {ClassId}",
                        instructorId, request.ClassId);

                    results.Add(new BulkAssignResultDto
                    {
                        InstructorId = instructorId,
                        InstructorName = null,
                        Success = false,
                        Message = $"Error: {ex.Message}"
                    });
                    failureCount++;
                }
            }

            await _context.SaveChangesAsync();

            var response = new BulkAssignGradersResponseDto
            {
                ClassId = request.ClassId,
                ClassName = classEntity.ClassName,
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalAttempted = request.InstructorIds.Count,
                Results = results
            };

            return new ResultModel<BulkAssignGradersResponseDto>
            {
                IsSuccess = true,
                Message = $"Bulk assign completed: {successCount} success, {failureCount} failed",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk assign graders");
            throw new AppException(
                CommonMessageConstants.ERROR,
                $"Error in bulk assign: {ex.Message}",
                StatusCodes.Status500InternalServerError
            );
        }
    }

    #region Private Helper Methods

    private async Task<ResultModel<ClassGraderDetailDto>> GetGraderDetailAsync(int graderId)
    {
        var grader = await _context.ClassGraders
            .Where(cg => cg.GraderId == graderId)
            .Include(cg => cg.Class)
            .Include(cg => cg.Instructor)
            .Include(cg => cg.AssignedByNavigation)
            .Select(cg => new
            {
                ClassGrader = cg,
                FinalSubmissions = _context.FinalProjectSubmissions
                    .Where(fps => fps.Project.Group!.ClassId == cg.ClassId)
                    .Select(fps => new
                    {
                        fps.FinalSubmissionId,
                        HasGrade = fps.FinalSubmissionGrades.Any(fsg => fsg.InstructorId == cg.InstructorId)
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (grader == null)
        {
            throw new AppException(
                CommonMessageConstants.NOT_FOUND,
                "Grader assignment not found",
                StatusCodes.Status404NotFound
            );
        }

        var totalSubmissions = grader.FinalSubmissions.Count;
        var gradedCount = grader.FinalSubmissions.Count(fs => fs.HasGrade);
        var pendingCount = totalSubmissions - gradedCount;

        var result = new ClassGraderDetailDto
        {
            GraderId = grader.ClassGrader.GraderId,
            ClassId = grader.ClassGrader.ClassId,
            ClassName = grader.ClassGrader.Class.ClassName,
            ClassDescription = grader.ClassGrader.Class.Description,
            InstructorId = grader.ClassGrader.InstructorId,
            InstructorName = grader.ClassGrader.Instructor.FullName,
            InstructorEmail = grader.ClassGrader.Instructor.Email,
            AssignedAt = grader.ClassGrader.AssignedAt,
            AssignedBy = grader.ClassGrader.AssignedBy,
            AssignedByName = grader.ClassGrader.AssignedByNavigation?.FullName,
            IsActive = grader.ClassGrader.IsActive,
            TotalFinalSubmissions = totalSubmissions,
            GradedByThisInstructor = gradedCount,
            PendingGrades = pendingCount
        };

        return new ResultModel<ClassGraderDetailDto>
        {
            IsSuccess = true,
            Message = "Grader assignment retrieved successfully",
            Data = result,
            StatusCode = StatusCodes.Status200OK
        };
    }

    #endregion
}
