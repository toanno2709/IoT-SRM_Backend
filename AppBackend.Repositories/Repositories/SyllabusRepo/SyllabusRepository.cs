using AppBackend.BusinessObjects.Data;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace AppBackend.Repositories.Repositories.SyllabusRepo;

public class SyllabusRepository : GenericRepository<Syllabus>, ISyllabusRepository
{
    public SyllabusRepository(IotShowroomContext context) : base(context)
    {
    }

    public async Task<Syllabus?> GetByIdAsync(int syllabusId)
    {
        return await _context.Syllabi
            .Include(s => s.Class)
            .Include(s => s.Creator)
            .Include(s => s.SyllabusFiles.OrderBy(f => f.DisplayOrder))
                .ThenInclude(f => f.Uploader)
            .FirstOrDefaultAsync(s => s.SyllabusId == syllabusId);
    }

    public async Task<List<Syllabus>> GetByClassIdAsync(int classId)
    {
        return await _context.Syllabi
            .Include(s => s.Class)
            .Include(s => s.Creator)
            .Include(s => s.SyllabusFiles.OrderBy(f => f.DisplayOrder))
            .Where(s => s.ClassId == classId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Syllabus>> GetByInstructorIdAsync(int instructorId)
    {
        return await _context.Syllabi
            .Include(s => s.Class)
            .Include(s => s.Creator)
            .Include(s => s.SyllabusFiles)
            .Where(s => s.Class.InstructorId == instructorId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Syllabus> CreateAsync(Syllabus syllabus)
    {
        await _context.Syllabi.AddAsync(syllabus);
        await _context.SaveChangesAsync();
        return syllabus;
    }

    public async Task UpdateAsync(Syllabus syllabus)
    {
        _context.Syllabi.Update(syllabus);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int syllabusId)
    {
        var syllabus = await _context.Syllabi.FindAsync(syllabusId);
        if (syllabus != null)
        {
            _context.Syllabi.Remove(syllabus);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int syllabusId)
    {
        return await _context.Syllabi.AnyAsync(s => s.SyllabusId == syllabusId);
    }

    public async Task<bool> IsInstructorOwnerAsync(int syllabusId, int instructorId)
    {
        return await _context.Syllabi
            .AnyAsync(s => s.SyllabusId == syllabusId && s.Class.InstructorId == instructorId);
    }

    // File operations
    public async Task<SyllabusFile?> GetFileByIdAsync(int fileId)
    {
        return await _context.SyllabusFiles
            .Include(f => f.Syllabus)
            .Include(f => f.Uploader)
            .FirstOrDefaultAsync(f => f.FileId == fileId);
    }

    public async Task<List<SyllabusFile>> GetFilesBySyllabusIdAsync(int syllabusId)
    {
        return await _context.SyllabusFiles
            .Include(f => f.Uploader)
            .Where(f => f.SyllabusId == syllabusId)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();
    }

    public async Task<SyllabusFile> AddFileAsync(SyllabusFile file)
    {
        await _context.SyllabusFiles.AddAsync(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task UpdateFileAsync(SyllabusFile file)
    {
        _context.SyllabusFiles.Update(file);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteFileAsync(int fileId)
    {
        var file = await _context.SyllabusFiles.FindAsync(fileId);
        if (file != null)
        {
            _context.SyllabusFiles.Remove(file);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> FileExistsAsync(int fileId)
    {
        return await _context.SyllabusFiles.AnyAsync(f => f.FileId == fileId);
    }
}
