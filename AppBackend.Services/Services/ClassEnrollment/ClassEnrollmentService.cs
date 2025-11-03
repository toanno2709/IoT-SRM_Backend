using AppBackend.BusinessObjects.Constants;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
using ClassEnrollmentModel = AppBackend.BusinessObjects.Models.ClassEnrollment;
using UserModel = AppBackend.BusinessObjects.Models.User;

namespace AppBackend.Services.Services.ClassEnrollment;

public class ClassEnrollmentService : IClassEnrollmentService
{
    private readonly IClassRepository _classRepo;
    private readonly IUserRepository _userRepo;
    private readonly IotShowroomContext _context;

    public ClassEnrollmentService(
        IClassRepository classRepo,
        IUserRepository userRepo,
        IotShowroomContext context)
    {
        _classRepo = classRepo;
        _userRepo = userRepo;
        _context = context;
    }

    public async Task<ResultModel<BulkAddStudentsResponseDto>> BulkAddStudentsAsync(BulkAddStudentsRequestDto request)
    {
        try
        {
            // 1. Ki?m tra class có t?n t?i không
            var classEntity = await _classRepo.GetClassWithDetailsAsync(request.ClassId);
            if (classEntity == null)
            {
                return new ResultModel<BulkAddStudentsResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "CLASS_NOT_FOUND",
                    Message = "Class not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. ??m s? h?c sinh hi?n t?i trong class
            var currentStudentCount = await _context.ClassEnrollments
                .Where(ce => ce.ClassId == request.ClassId)
                .CountAsync();

            // 3. Tính s? h?c sinh c?n thêm
            var studentsToAdd = request.MaxMembers - currentStudentCount;

            if (studentsToAdd <= 0)
            {
                return new ResultModel<BulkAddStudentsResponseDto>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "Class already has maximum or more students",
                    Data = new BulkAddStudentsResponseDto
                    {
                        ClassId = request.ClassId,
                        ClassName = classEntity.ClassName,
                        PreviousStudentCount = currentStudentCount,
                        NewStudentsAdded = 0,
                        TotalStudentsNow = currentStudentCount,
                        StudentsNotAdded = 0,
                        Message = $"Class already has {currentStudentCount} students (limit: {request.MaxMembers})",
                        Warnings = new List<string> { "No students added because class is at or above capacity" }
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }

            // 4. L?y danh sách student IDs ?ã có trong class
            var existingStudentIds = await _context.ClassEnrollments
                .Where(ce => ce.ClassId == request.ClassId)
                .Select(ce => ce.StudentId)
                .ToListAsync();

            // 5. Tìm students available (role_id = 3, ch?a có trong class)
            var availableStudents = await _context.Users
                .Where(u => u.RoleId == 3 && !existingStudentIds.Contains(u.UserId))
                .Take(studentsToAdd)
                .ToListAsync();

            if (!availableStudents.Any())
            {
                return new ResultModel<BulkAddStudentsResponseDto>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "No available students to add",
                    Data = new BulkAddStudentsResponseDto
                    {
                        ClassId = request.ClassId,
                        ClassName = classEntity.ClassName,
                        PreviousStudentCount = currentStudentCount,
                        NewStudentsAdded = 0,
                        TotalStudentsNow = currentStudentCount,
                        StudentsNotAdded = studentsToAdd,
                        Message = $"No available students found to add to class",
                        Warnings = new List<string> { "All students are already enrolled in this class or no students exist" }
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }

            // 6. T?o ClassEnrollment records
            var enrollments = new List<ClassEnrollmentModel>();
            var addedStudentDtos = new List<StudentEnrollmentDto>();
            var enrolledAt = DateTime.UtcNow;

            foreach (var student in availableStudents)
            {
                var enrollment = new ClassEnrollmentModel
                {
                    ClassId = request.ClassId,
                    StudentId = student.UserId,
                    EnrolledAt = enrolledAt
                };

                enrollments.Add(enrollment);

                addedStudentDtos.Add(new StudentEnrollmentDto
                {
                    UserId = student.UserId,
                    FullName = student.FullName,
                    Email = student.Email,
                    EnrolledAt = enrolledAt
                });
            }

            // 7. Bulk insert vào database
            await _context.ClassEnrollments.AddRangeAsync(enrollments);
            await _context.SaveChangesAsync();

            // 8. T?o response
            var newTotalCount = currentStudentCount + availableStudents.Count;
            var warnings = new List<string>();

            if (availableStudents.Count < studentsToAdd)
            {
                warnings.Add($"Only {availableStudents.Count} students were available to add (requested {studentsToAdd})");
            }

            return new ResultModel<BulkAddStudentsResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Successfully added {availableStudents.Count} students to class",
                Data = new BulkAddStudentsResponseDto
                {
                    ClassId = request.ClassId,
                    ClassName = classEntity.ClassName,
                    PreviousStudentCount = currentStudentCount,
                    NewStudentsAdded = availableStudents.Count,
                    TotalStudentsNow = newTotalCount,
                    StudentsNotAdded = studentsToAdd - availableStudents.Count,
                    Message = $"Added {availableStudents.Count} out of {studentsToAdd} requested students",
                    AddedStudents = addedStudentDtos,
                    Warnings = warnings
                },
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<BulkAddStudentsResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "INTERNAL_ERROR",
                Message = $"Error adding students to class: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<AddStudentToClassResponseDto>> AddStudentToClassAsync(int classId, int studentId)
    {
        try
        {
            // 1. Ki?m tra class có t?n t?i
            var classEntity = await _classRepo.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<AddStudentToClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "CLASS_NOT_FOUND",
                    Message = "Class not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. Ki?m tra student có t?n t?i và có role_id = 3
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == studentId && u.RoleId == 3);

            if (student == null)
            {
                return new ResultModel<AddStudentToClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "STUDENT_NOT_FOUND",
                    Message = "Student not found or user is not a student",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 3. Ki?m tra student ?ã có trong class ch?a
            var existingEnrollment = await _context.ClassEnrollments
                .FirstOrDefaultAsync(ce => ce.ClassId == classId && ce.StudentId == studentId);

            if (existingEnrollment != null)
            {
                return new ResultModel<AddStudentToClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "ALREADY_ENROLLED",
                    Message = "Student is already enrolled in this class",
                    Data = null,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // 4. Thêm student vào class
            var enrollment = new ClassEnrollmentModel
            {
                ClassId = classId,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            };

            await _context.ClassEnrollments.AddAsync(enrollment);
            await _context.SaveChangesAsync();

            return new ResultModel<AddStudentToClassResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Student added to class successfully",
                Data = new AddStudentToClassResponseDto
                {
                    ClassId = classId,
                    ClassName = classEntity.ClassName,
                    StudentId = studentId,
                    StudentName = student.FullName,
                    Email = student.Email,
                    EnrolledAt = enrollment.EnrolledAt ?? DateTime.UtcNow,
                    Message = "Student enrolled successfully"
                },
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<AddStudentToClassResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "INTERNAL_ERROR",
                Message = $"Error adding student to class: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<bool>> RemoveStudentFromClassAsync(int classId, int studentId)
    {
        try
        {
            // 1. Tìm enrollment
            var enrollment = await _context.ClassEnrollments
                .FirstOrDefaultAsync(ce => ce.ClassId == classId && ce.StudentId == studentId);

            if (enrollment == null)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "ENROLLMENT_NOT_FOUND",
                    Message = "Student is not enrolled in this class",
                    Data = false,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. Ki?m tra student có trong group nào không
            var hasGroupMembership = await _context.GroupMembers
                .Include(gm => gm.Group)
                .AnyAsync(gm => gm.UserId == studentId && gm.Group!.ClassId == classId);

            if (hasGroupMembership)
            {
                return new ResultModel<bool>
                {
                    IsSuccess = false,
                    ResponseCode = "STUDENT_IN_GROUP",
                    Message = "Cannot remove student who is a member of a group. Remove from group first.",
                    Data = false,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // 3. Xóa enrollment
            _context.ClassEnrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return new ResultModel<bool>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Student removed from class successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<bool>
            {
                IsSuccess = false,
                ResponseCode = "INTERNAL_ERROR",
                Message = $"Error removing student from class: {ex.Message}",
                Data = false,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ClassStudentsResponseDto>> GetClassStudentsAsync(int classId)
    {
        try
        {
            // 1. Ki?m tra class có t?n t?i
            var classEntity = await _classRepo.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassStudentsResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "CLASS_NOT_FOUND",
                    Message = "Class not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. L?y danh sách students
            var enrollments = await _context.ClassEnrollments
                .Where(ce => ce.ClassId == classId)
                .Include(ce => ce.Student)
                .OrderBy(ce => ce.Student!.FullName)
                .ToListAsync();

            var studentDtos = enrollments
                .Where(ce => ce.Student != null)
                .Select(ce => new StudentEnrollmentDto
                {
                    UserId = ce.Student!.UserId,
                    FullName = ce.Student.FullName,
                    Email = ce.Student.Email,
                    EnrolledAt = ce.EnrolledAt
                })
                .ToList();

            return new ResultModel<ClassStudentsResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Retrieved {studentDtos.Count} students",
                Data = new ClassStudentsResponseDto
                {
                    ClassId = classId,
                    ClassName = classEntity.ClassName,
                    TotalStudents = studentDtos.Count,
                    Students = studentDtos
                },
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassStudentsResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "INTERNAL_ERROR",
                Message = $"Error retrieving class students: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
