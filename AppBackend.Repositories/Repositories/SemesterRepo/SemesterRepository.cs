using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.SemesterRepo
{
    public class SemesterRepository : GenericRepository<Semester>, ISemesterRepository
    {
        private readonly IOTShowroomContext _context;

        public SemesterRepository(IOTShowroomContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Semester?> GetByCodeAsync(string code)
        {
            return await _context.Semesters
                .FirstOrDefaultAsync(s => s.Code == code);
        }

        public async Task<bool> CodeExistsAsync(string code)
        {
            return await _context.Semesters
                .AnyAsync(s => s.Code == code);
        }

        public async Task<bool> CodeExistsAsync(string code, int excludeSemesterId)
        {
            return await _context.Semesters
                .AnyAsync(s => s.Code == code && s.SemesterId != excludeSemesterId);
        }

        public async Task<Semester?> GetActiveAsync()
        {
            return await _context.Semesters
                .FirstOrDefaultAsync(s => s.IsActive == true);
        }

        public async Task<IEnumerable<Semester>> GetByYearAsync(int year)
        {
            return await _context.Semesters
                .Where(s => s.Year == year)
                .OrderBy(s => s.Term)
                .ToListAsync();
        }
    }
}
