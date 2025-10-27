using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.SensorRepo;
using AppBackend.Repositories.Repositories.ProjectRepo;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ApiModels.Sensor;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Services.Services.Sensor
{
    public class SensorService : ISensorService
    {
        private readonly ISensorRepository _sensorRepo;
        private readonly IProjectRepository _projectRepo;

        public SensorService(
            ISensorRepository sensorRepo,
            IProjectRepository projectRepo)
        {
            _sensorRepo = sensorRepo;
            _projectRepo = projectRepo;
        }

        public async Task<ResultModel<SensorResponseDto>> CreateSensorAsync(
            int projectId, 
            int currentUserId,
            string userRole,
            CreateSensorRequestDto request)
        {
            // Check if project exists
            var project = await _projectRepo.GetByIdWithDetailsAsync(projectId);
            if (project == null)
            {
                return new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "PROJECT_NOT_FOUND",
                    Message = "Project not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check ownership/permission
            var canAccess = await CanAccessProjectAsync(projectId, currentUserId, userRole);
            if (!canAccess)
            {
                return new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "FORBIDDEN",
                    Message = "You do not have permission to add sensors to this project",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Check if sensor name already exists in this project (case-insensitive)
            var nameExists = await _sensorRepo.SensorNameExistsAsync(projectId, request.Name);
            if (nameExists)
            {
                return new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "DUPLICATE_SENSOR_NAME",
                    Message = $"Sensor with name '{request.Name}' already exists in this project",
                    Data = null,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // Create new sensor
            var newSensor = new BusinessObjects.Models.Sensor
            {
                ProjectId = projectId,
                Name = request.Name,
                Type = request.Type,
                Unit = request.Unit,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _sensorRepo.AddAsync(newSensor);
            await _sensorRepo.SaveChangesAsync();

            // Reload with navigation properties
            var createdSensor = await _sensorRepo.GetByIdWithProjectAsync(newSensor.SensorId);

            var dto = new SensorResponseDto
            {
                SensorId = createdSensor!.SensorId,
                ProjectId = createdSensor.ProjectId,
                ProjectTitle = createdSensor.Project?.Title,
                Name = createdSensor.Name,
                Type = createdSensor.Type,
                Unit = createdSensor.Unit,
                Description = createdSensor.Description,
                CreatedAt = createdSensor.CreatedAt
            };

            return new ResultModel<SensorResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Sensor created successfully",
                Data = dto,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel<SensorListResponseDto>> GetSensorsByProjectAsync(
            int projectId, 
            int currentUserId,
            string userRole,
            string? searchQuery, 
            int page, 
            int pageSize)
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 200) pageSize = 200;

            // Check if project exists
            var project = await _projectRepo.GetByIdAsync(projectId);
            if (project == null)
            {
                return new ResultModel<SensorListResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "PROJECT_NOT_FOUND",
                    Message = "Project not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check ownership/permission
            var canAccess = await CanAccessProjectAsync(projectId, currentUserId, userRole);
            if (!canAccess)
            {
                return new ResultModel<SensorListResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "FORBIDDEN",
                    Message = "You do not have permission to view sensors of this project",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            // Get sensors with search and pagination
            var sensors = await _sensorRepo.SearchSensorsAsync(projectId, searchQuery, page, pageSize);
            var total = await _sensorRepo.CountByProjectAsync(projectId, searchQuery);

            var items = sensors.Select(s => new SensorResponseDto
            {
                SensorId = s.SensorId,
                ProjectId = s.ProjectId,
                ProjectTitle = s.Project?.Title,
                Name = s.Name,
                Type = s.Type,
                Unit = s.Unit,
                Description = s.Description,
                CreatedAt = s.CreatedAt
            }).ToList();

            var response = new SensorListResponseDto
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResultModel<SensorListResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Found {total} sensors",
                Data = response,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<SensorDetailDto>> GetSensorByIdAsync(
            int projectId, 
            int sensorId, 
            int currentUserId,
            string userRole)
        {
            // Check if sensor belongs to project
            var belongsToProject = await _sensorRepo.BelongsToProjectAsync(sensorId, projectId);
            if (!belongsToProject)
            {
                return new ResultModel<SensorDetailDto>
                {
                    IsSuccess = false,
                    ResponseCode = "NOT_FOUND",
                    Message = "Sensor not found in this project",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check ownership/permission
            var canAccess = await CanAccessProjectAsync(projectId, currentUserId, userRole);
            if (!canAccess)
            {
                return new ResultModel<SensorDetailDto>
                {
                    IsSuccess = false,
                    ResponseCode = "FORBIDDEN",
                    Message = "You do not have permission to view this sensor",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var sensor = await _sensorRepo.GetByIdWithProjectAsync(sensorId);
            if (sensor == null)
            {
                return new ResultModel<SensorDetailDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Sensor not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Get statistics
            var totalDataPoints = sensor.SensorData?.Count ?? 0;
            var latestData = sensor.SensorData?.OrderByDescending(sd => sd.Timestamp).FirstOrDefault();

            var dto = new SensorDetailDto
            {
                SensorId = sensor.SensorId,
                ProjectId = sensor.ProjectId,
                ProjectTitle = sensor.Project?.Title,
                Name = sensor.Name,
                Type = sensor.Type,
                Unit = sensor.Unit,
                Description = sensor.Description,
                CreatedAt = sensor.CreatedAt,
                TotalDataPoints = totalDataPoints,
                LatestDataTimestamp = latestData?.Timestamp
            };

            return new ResultModel<SensorDetailDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Sensor retrieved successfully",
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<SensorResponseDto>> UpdateSensorAsync(
            int projectId, 
            int sensorId, 
            int currentUserId,
            string userRole,
            UpdateSensorRequestDto request)
        {
            // Check if sensor belongs to project
            var belongsToProject = await _sensorRepo.BelongsToProjectAsync(sensorId, projectId);
            if (!belongsToProject)
            {
                return new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "NOT_FOUND",
                    Message = "Sensor not found in this project",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check ownership/permission
            var canAccess = await CanAccessProjectAsync(projectId, currentUserId, userRole);
            if (!canAccess)
            {
                return new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "FORBIDDEN",
                    Message = "You do not have permission to update this sensor",
                    Data = null,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var sensor = await _sensorRepo.GetByIdAsync(sensorId);
            if (sensor == null)
            {
                return new ResultModel<SensorResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Sensor not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Update name if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                // Check if new name already exists (excluding current sensor)
                var nameExists = await _sensorRepo.SensorNameExistsAsync(projectId, request.Name, sensorId);
                if (nameExists)
                {
                    return new ResultModel<SensorResponseDto>
                    {
                        IsSuccess = false,
                        ResponseCode = "DUPLICATE_SENSOR_NAME",
                        Message = $"Sensor with name '{request.Name}' already exists in this project",
                        Data = null,
                        StatusCode = StatusCodes.Status409Conflict
                    };
                }
                sensor.Name = request.Name;
            }

            // Update other fields if provided
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                sensor.Type = request.Type;
            }

            if (request.Unit != null)
            {
                sensor.Unit = request.Unit;
            }

            if (request.Description != null)
            {
                sensor.Description = request.Description;
            }

            await _sensorRepo.UpdateAsync(sensor);
            await _sensorRepo.SaveChangesAsync();

            // Reload with navigation properties
            var updatedSensor = await _sensorRepo.GetByIdWithProjectAsync(sensorId);

            var dto = new SensorResponseDto
            {
                SensorId = updatedSensor!.SensorId,
                ProjectId = updatedSensor.ProjectId,
                ProjectTitle = updatedSensor.Project?.Title,
                Name = updatedSensor.Name,
                Type = updatedSensor.Type,
                Unit = updatedSensor.Unit,
                Description = updatedSensor.Description,
                CreatedAt = updatedSensor.CreatedAt
            };

            return new ResultModel<SensorResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Sensor updated successfully",
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<bool>> DeleteSensorAsync(
            int projectId, 
            int sensorId, 
            int currentUserId,
            string userRole)
        {
            // Check if sensor belongs to project
            var belongsToProject = await _sensorRepo.BelongsToProjectAsync(sensorId, projectId);
            if (!belongsToProject)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "NOT_FOUND",
                    Message = "Sensor not found in this project",
                    Data = false,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check ownership/permission
            var canAccess = await CanAccessProjectAsync(projectId, currentUserId, userRole);
            if (!canAccess)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "FORBIDDEN",
                    Message = "You do not have permission to delete this sensor",
                    Data = false,
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var sensor = await _sensorRepo.GetByIdAsync(sensorId);
            if (sensor == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = CommonMessageConstants.NOT_FOUND,
                    Message = "Sensor not found",
                    Data = false,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // Check if sensor has related data
            var hasSensorData = await _sensorRepo.HasSensorDataAsync(sensorId);
            if (hasSensorData)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "HAS_SENSOR_DATA",
                    Message = "Cannot delete sensor that has sensor data records",
                    Data = false,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // Check if sensor is used in live demos
            var hasLiveDemoRelations = await _sensorRepo.HasLiveDemoRelationsAsync(sensorId);
            if (hasLiveDemoRelations)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "HAS_LIVE_DEMO",
                    Message = "Cannot delete sensor that is used in live demos",
                    Data = false,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            await _sensorRepo.DeleteAsync(sensor);
            await _sensorRepo.SaveChangesAsync();

            return new ResultModel<bool>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Sensor deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<bool> CanAccessProjectAsync(int projectId, int userId, string userRole)
        {
            var project = await _projectRepo.GetByIdWithDetailsAsync(projectId);
            if (project == null) return false;

            // Admin always has access
            if (userRole == "Admin") return true;

            // Instructor: check if they are the instructor of the class
            if (userRole == "Instructor")
            {
                return project.Group?.Class?.InstructorId == userId;
            }

            // Student: check if they are a member of the group
            if (userRole == "Student")
            {
                return project.Group?.GroupMembers?.Any(gm => gm.UserId == userId) ?? false;
            }

            return false;
        }
    }
}
