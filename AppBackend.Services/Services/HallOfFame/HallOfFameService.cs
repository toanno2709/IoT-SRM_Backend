using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.HallOfFameRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Repositories.Repositories.SemesterRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.HallOfFame;

public class HallOfFameService : IHallOfFameService
{
    private readonly IHallOfFameRepository _hallOfFameRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISemesterRepository _semesterRepository;
    private readonly IUserRepository _userRepository;

    public HallOfFameService(
        IHallOfFameRepository hallOfFameRepository,
        IProjectRepository projectRepository,
        ISemesterRepository semesterRepository,
        IUserRepository userRepository)
    {
        _hallOfFameRepository = hallOfFameRepository;
        _projectRepository = projectRepository;
        _semesterRepository = semesterRepository;
        _userRepository = userRepository;
    }

    public async Task<ResultModel<List<HallOfFameResponseDto>>> GetAllHallOfFameAsync()
    {
        try
        {
            var hallOfFames = await _hallOfFameRepository.GetAllAsync();

            var responseDtos = hallOfFames.Select(MapToResponseDto).ToList();

            return new ResultModel<List<HallOfFameResponseDto>>
            {
                IsSuccess = true,
                Data = responseDtos,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<HallOfFameResponseDto>>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error retrieving Hall of Fame entries: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<HallOfFameResponseDto>> GetHallOfFameByIdAsync(int hofId)
    {
        try
        {
            var hallOfFame = await _hallOfFameRepository.GetByIdAsync(hofId);

            if (hallOfFame == null)
            {
                return new ResultModel<HallOfFameResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Hall of Fame entry not found"
                };
            }

            var responseDto = MapToResponseDto(hallOfFame);

            return new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = true,
                Data = responseDto,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error retrieving Hall of Fame entry: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<List<HallOfFameResponseDto>>> GetHallOfFameBySemesterAsync(int semesterId)
    {
        try
        {
            var semester = await _semesterRepository.GetByIdAsync(semesterId);
            if (semester == null)
            {
                return new ResultModel<List<HallOfFameResponseDto>>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Semester not found"
                };
            }

            var hallOfFames = await _hallOfFameRepository.GetBySemesterAsync(semesterId);

            var responseDtos = hallOfFames.Select(MapToResponseDto).ToList();

            return new ResultModel<List<HallOfFameResponseDto>>
            {
                IsSuccess = true,
                Data = responseDtos,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<List<HallOfFameResponseDto>>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error retrieving Hall of Fame entries: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<LeaderboardResponseDto>> GetLeaderboardBySemesterAsync(int semesterId)
    {
        try
        {
            var semester = await _semesterRepository.GetByIdAsync(semesterId);
            if (semester == null)
            {
                return new ResultModel<LeaderboardResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Semester not found"
                };
            }

            // Get all projects in this semester with their final scores
            var projects = await _projectRepository.GetProjectsBySemesterAsync(semesterId);
            var completedProjects = projects
                .Where(p => p.Status == "Completed" && p.FinalProjectSubmission != null && p.FinalProjectSubmission.Grade.HasValue)
                .OrderByDescending(p => p.FinalProjectSubmission!.Grade)
                .Take(10)
                .ToList();

            // Get Hall of Fame entries for this semester
            var hallOfFameEntries = await _hallOfFameRepository.GetBySemesterAsync(semesterId);
            var hallOfFameProjectIds = hallOfFameEntries.Select(h => h.ProjectId).ToHashSet();

            var leaderboardEntries = completedProjects.Select((project, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                ProjectId = project.ProjectId,
                ProjectName = project.Title,
                ProjectDescription = project.Description,
                GroupName = project.Group?.GroupName,
                FinalScore = project.FinalProjectSubmission?.Grade,
                SemesterName = semester.Name,
                CompletedDate = project.FinalProjectSubmission?.SubmittedAt,
                Note = hallOfFameEntries.FirstOrDefault(h => h.ProjectId == project.ProjectId)?.Note,
                IsInHallOfFame = hallOfFameProjectIds.Contains(project.ProjectId)
            }).ToList();

            var response = new LeaderboardResponseDto
            {
                SemesterId = semesterId,
                SemesterName = semester.Name,
                TotalProjects = projects.Count(),
                TopProjects = leaderboardEntries,
                GeneratedAt = DateTime.UtcNow
            };

            return new ResultModel<LeaderboardResponseDto>
            {
                IsSuccess = true,
                Data = response,
                Message = CommonMessageConstants.GET_SUCCESS
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<LeaderboardResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error generating leaderboard: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<HallOfFameResponseDto>> NominateProjectAsync(int adminId, HallOfFameNominateRequestDto request)
    {
        try
        {
            // Validate project exists
            var project = await _projectRepository.GetByIdWithDetailsAsync(request.ProjectId);
            if (project == null)
            {
                return new ResultModel<HallOfFameResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Project not found"
                };
            }

            // Validate semester exists
            var semester = await _semesterRepository.GetByIdAsync(request.SemesterId);
            if (semester == null)
            {
                return new ResultModel<HallOfFameResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Semester not found"
                };
            }

            // Check if already nominated
            var exists = await _hallOfFameRepository.ExistsAsync(request.ProjectId, request.SemesterId);
            if (exists)
            {
                return new ResultModel<HallOfFameResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = "Project is already nominated for this semester"
                };
            }

            // Validate project status and score
            if (project.Status != "Completed")
            {
                return new ResultModel<HallOfFameResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = "Only completed projects can be nominated to Hall of Fame"
                };
            }

            if (project.FinalProjectSubmission == null || 
                !project.FinalProjectSubmission.Grade.HasValue || 
                project.FinalProjectSubmission.Grade.Value < 80)
            {
                return new ResultModel<HallOfFameResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = "Project must have a final grade of at least 80 to be nominated"
                };
            }

            // If rank is not provided, calculate it based on current Hall of Fame entries
            int rank = request.Rank ?? 1;
            if (!request.Rank.HasValue)
            {
                var existingEntries = await _hallOfFameRepository.GetBySemesterAsync(request.SemesterId);
                rank = existingEntries.Count() + 1;
            }

            // Create Hall of Fame entry
            var hallOfFame = new BusinessObjects.Models.HallOfFame
            {
                ProjectId = request.ProjectId,
                SemesterId = request.SemesterId,
                NominatedBy = adminId,
                NominatedAt = DateTime.UtcNow,
                Rank = rank,
                Note = request.Note
            };

            await _hallOfFameRepository.AddAsync(hallOfFame);
            await _hallOfFameRepository.SaveChangesAsync();

            // Reload with includes
            var savedEntry = await _hallOfFameRepository.GetByIdAsync(hallOfFame.HofId);
            var responseDto = MapToResponseDto(savedEntry!);

            return new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = true,
                Data = responseDto,
                Message = "Project successfully nominated to Hall of Fame"
            };
        }
        catch (DbUpdateException ex)
        {
            return new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = $"Database error: {ex.InnerException?.Message ?? ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error nominating project: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<HallOfFameResponseDto>> UpdateHallOfFameAsync(int hofId, HallOfFameUpdateRequestDto request)
    {
        try
        {
            var hallOfFame = await _hallOfFameRepository.GetByIdAsync(hofId);
            if (hallOfFame == null)
            {
                return new ResultModel<HallOfFameResponseDto>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Hall of Fame entry not found"
                };
            }

            // Update fields
            if (request.Rank.HasValue)
            {
                hallOfFame.Rank = request.Rank.Value;
            }

            if (request.Note != null)
            {
                hallOfFame.Note = request.Note;
            }

            await _hallOfFameRepository.UpdateAsync(hallOfFame);
            await _hallOfFameRepository.SaveChangesAsync();

            // Reload with includes
            var updatedEntry = await _hallOfFameRepository.GetByIdAsync(hofId);
            var responseDto = MapToResponseDto(updatedEntry!);

            return new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = true,
                Data = responseDto,
                Message = "Hall of Fame entry updated successfully"
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<HallOfFameResponseDto>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error updating Hall of Fame entry: {ex.Message}"
            };
        }
    }

    public async Task<ResultModel<bool>> DeleteHallOfFameAsync(int hofId)
    {
        try
        {
            var hallOfFame = await _hallOfFameRepository.GetByIdAsync(hofId);
            if (hallOfFame == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Message = "Hall of Fame entry not found"
                };
            }

            await _hallOfFameRepository.DeleteAsync(hallOfFame);
            await _hallOfFameRepository.SaveChangesAsync();

            return new ResultModel<bool>
            {
                IsSuccess = true,
                Data = true,
                Message = "Hall of Fame entry deleted successfully"
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                StatusCode = 500,
                Message = $"Error deleting Hall of Fame entry: {ex.Message}"
            };
        }
    }

    #region Helper Methods

    private HallOfFameResponseDto MapToResponseDto(BusinessObjects.Models.HallOfFame hallOfFame)
    {
        return new HallOfFameResponseDto
        {
            HofId = hallOfFame.HofId,
            ProjectId = hallOfFame.ProjectId,
            ProjectName = hallOfFame.Project?.Title,
            GroupName = hallOfFame.Project?.Group?.GroupName,
            NominatedBy = hallOfFame.NominatedBy,
            NominatedByName = null, // Can be populated if needed
            NominatedAt = hallOfFame.NominatedAt,
            SemesterId = hallOfFame.SemesterId,
            SemesterName = hallOfFame.Semester?.Name,
            Rank = hallOfFame.Rank,
            Note = hallOfFame.Note,
            FinalScore = hallOfFame.Project?.FinalProjectSubmission?.Grade
        };
    }

    #endregion
}
