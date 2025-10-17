using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.SensorRepo
{
    public class SensorRepository : GenericRepository<Sensor>, ISensorRepository
    {
        public SensorRepository(IOTShowroomContext context) : base(context)
        {
        }

        public async Task<List<Sensor>> GetByProjectIdAsync(int projectId)
        {
            return await _context.Sensors
                .Include(s => s.Project)
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Sensor?> GetByIdWithProjectAsync(int sensorId)
        {
            return await _context.Sensors
                .Include(s => s.Project)
                    .ThenInclude(p => p!.Group)
                        .ThenInclude(g => g!.Class)
                .FirstOrDefaultAsync(s => s.SensorId == sensorId);
        }

        public async Task<List<Sensor>> SearchSensorsAsync(int projectId, string? searchQuery, int page, int pageSize)
        {
            var query = _context.Sensors
                .Include(s => s.Project)
                .Where(s => s.ProjectId == projectId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.ToLower();
                query = query.Where(s => 
                    (s.Name != null && s.Name.ToLower().Contains(search)) ||
                    (s.Type != null && s.Type.ToLower().Contains(search)) ||
                    (s.Description != null && s.Description.ToLower().Contains(search)));
            }

            return await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByProjectAsync(int projectId, string? searchQuery)
        {
            var query = _context.Sensors
                .Where(s => s.ProjectId == projectId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.ToLower();
                query = query.Where(s => 
                    (s.Name != null && s.Name.ToLower().Contains(search)) ||
                    (s.Type != null && s.Type.ToLower().Contains(search)) ||
                    (s.Description != null && s.Description.ToLower().Contains(search)));
            }

            return await query.CountAsync();
        }

        public async Task<bool> SensorNameExistsAsync(int projectId, string name)
        {
            return await _context.Sensors
                .AnyAsync(s => s.ProjectId == projectId && 
                              s.Name != null && 
                              s.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> SensorNameExistsAsync(int projectId, string name, int excludeSensorId)
        {
            return await _context.Sensors
                .AnyAsync(s => s.ProjectId == projectId && 
                              s.Name != null && 
                              s.Name.ToLower() == name.ToLower() && 
                              s.SensorId != excludeSensorId);
        }

        public async Task<bool> BelongsToProjectAsync(int sensorId, int projectId)
        {
            return await _context.Sensors
                .AnyAsync(s => s.SensorId == sensorId && s.ProjectId == projectId);
        }

        public async Task<bool> HasSensorDataAsync(int sensorId)
        {
            return await _context.SensorData
                .AnyAsync(sd => sd.SensorId == sensorId);
        }

        public async Task<bool> HasLiveDemoRelationsAsync(int sensorId)
        {
            return await _context.LiveDemoSensors
                .AnyAsync(lds => lds.SensorId == sensorId);
        }
    }
}
