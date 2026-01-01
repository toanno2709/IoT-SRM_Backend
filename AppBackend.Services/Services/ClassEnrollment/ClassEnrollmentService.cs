using AppBackend.BusinessObjects.Constants;
using AppBackend.Repositories.Repositories.ClassRepo;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Repositories.Repositories.StudentCourseHistoryRepo;
using AppBackend.Services.ApiModels.Commons;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AppBackend.BusinessObjects.Data;
using OfficeOpenXml;
using ClassEnrollmentModel = AppBackend.BusinessObjects.Models.ClassEnrollment;
using UserModel = AppBackend.BusinessObjects.Models.User;

namespace AppBackend.Services.Services.ClassEnrollment;

public class ClassEnrollmentService : IClassEnrollmentService
{
    private readonly IClassRepository _classRepo;
    private readonly IUserRepository _userRepo;
    private readonly IStudentCourseHistoryRepository _studentCourseHistoryRepo;
    private readonly IotShowroomContext _context;

    public ClassEnrollmentService(
        IClassRepository classRepo,
        IUserRepository userRepo,
        IStudentCourseHistoryRepository studentCourseHistoryRepo,
        IotShowroomContext context)
    {
        _classRepo = classRepo;
        _userRepo = userRepo;
        _studentCourseHistoryRepo = studentCourseHistoryRepo;
        _context = context;
        
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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

            // 5. T?m students available (role_id = 3, ch?a c? trong class, AND ch?a hoàn thành môn)
            var availableStudents = await _context.Users
                .Where(u => u.RoleId == 3 && !existingStudentIds.Contains(u.UserId))
                .Take(studentsToAdd * 2) // L?y nhi?u h?n ?? filter
                .ToListAsync();

            // Filter out students who have passed the course
            var eligibleStudents = new List<UserModel>();
            foreach (var student in availableStudents)
            {
                var isEligible = await _studentCourseHistoryRepo.IsEligibleForEnrollmentAsync(student.UserId);
                if (isEligible)
                {
                    eligibleStudents.Add(student);
                    if (eligibleStudents.Count >= studentsToAdd)
                        break;
                }
            }

            if (!eligibleStudents.Any())
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
                        Message = $"No eligible students found to add to class",
                        Warnings = new List<string> { "All available students have either already enrolled, already passed the course, or no students exist" }
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }

            // 6. T?o ClassEnrollment records
            var enrollments = new List<ClassEnrollmentModel>();
            var addedStudentDtos = new List<StudentEnrollmentDto>();
            var enrolledAt = DateTime.UtcNow;

            foreach (var student in eligibleStudents)
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

            // 7. Bulk insert v?o database
            await _context.ClassEnrollments.AddRangeAsync(enrollments);
            await _context.SaveChangesAsync();

            // 8. T?o response
            var newTotalCount = currentStudentCount + eligibleStudents.Count;
            var warnings = new List<string>();

            if (eligibleStudents.Count < studentsToAdd)
            {
                warnings.Add($"Only {eligibleStudents.Count} eligible students were available to add (requested {studentsToAdd})");
            }

            return new ResultModel<BulkAddStudentsResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Successfully added {eligibleStudents.Count} students to class",
                Data = new BulkAddStudentsResponseDto
                {
                    ClassId = request.ClassId,
                    ClassName = classEntity.ClassName,
                    PreviousStudentCount = currentStudentCount,
                    NewStudentsAdded = eligibleStudents.Count,
                    TotalStudentsNow = newTotalCount,
                    StudentsNotAdded = studentsToAdd - eligibleStudents.Count,
                    Message = $"Added {eligibleStudents.Count} out of {studentsToAdd} requested students",
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

            // 3. Ki?m tra student ?? c? trong class ch?a
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

            // 3.5. Ki?m tra student ?ã hoàn thành môn h?c ch?a
            var isEligible = await _studentCourseHistoryRepo.IsEligibleForEnrollmentAsync(studentId);
            if (!isEligible)
            {
                return new ResultModel<AddStudentToClassResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "ALREADY_PASSED_COURSE",
                    Message = "Student has already completed the IoT course and cannot be enrolled again",
                    Data = null,
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            // 4. Th?m student v?o class
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

    public async Task<ResultModel<ClassStudentsWithGroupResponseDto>> GetClassStudentsWithGroupAsync(int classId)
    {
        try
        {
            // 1. Ki?m tra class có t?n t?i
            var classEntity = await _classRepo.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ClassStudentsWithGroupResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "CLASS_NOT_FOUND",
                    Message = "Class not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. L?y danh sách students trong class
            var enrollments = await _context.ClassEnrollments
                .Where(ce => ce.ClassId == classId)
                .Include(ce => ce.Student)
                .ToListAsync();

            if (!enrollments.Any())
            {
                return new ResultModel<ClassStudentsWithGroupResponseDto>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = "No students found in this class",
                    Data = new ClassStudentsWithGroupResponseDto
                    {
                        ClassId = classId,
                        ClassName = classEntity.ClassName,
                        TotalStudents = 0,
                        StudentsWithGroup = 0,
                        StudentsWithoutGroup = 0,
                        Students = new List<StudentWithGroupDto>()
                    },
                    StatusCode = StatusCodes.Status200OK
                };
            }

            var studentIds = enrollments
                .Where(ce => ce.StudentId.HasValue)
                .Select(ce => ce.StudentId!.Value)
                .ToList();

            // 3. L?y thông tin group membership c?a students trong class này
            var groupMemberships = await _context.GroupMembers
                .Where(gm => studentIds.Contains(gm.UserId))
                .Include(gm => gm.Group)
                .Where(gm => gm.Group.ClassId == classId)
                .ToListAsync();

            // 4. T?o dictionary ?? lookup nhanh
            var groupMembershipDict = groupMemberships
                .GroupBy(gm => gm.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.First() // M?i student ch? có th? ? 1 group trong 1 class
                );

            // 5. Map sang DTO
            var studentDtos = enrollments
                .Where(ce => ce.Student != null)
                .Select(ce =>
                {
                    var studentId = ce.Student!.UserId;
                    var hasGroupMembership = groupMembershipDict.ContainsKey(studentId);

                    return new StudentWithGroupDto
                    {
                        UserId = studentId,
                        FullName = ce.Student.FullName,
                        Email = ce.Student.Email,
                        EnrolledAt = ce.EnrolledAt,
                        HasGroup = hasGroupMembership,
                        GroupId = hasGroupMembership ? groupMembershipDict[studentId].GroupId : null,
                        GroupName = hasGroupMembership ? groupMembershipDict[studentId].Group?.GroupName : null,
                        RoleInGroup = hasGroupMembership ? groupMembershipDict[studentId].RoleInGroup : null,
                        JoinedGroupAt = hasGroupMembership ? groupMembershipDict[studentId].JoinedAt : null
                    };
                })
                .OrderBy(s => s.FullName)
                .ToList();

            var studentsWithGroup = studentDtos.Count(s => s.HasGroup);
            var studentsWithoutGroup = studentDtos.Count(s => !s.HasGroup);

            return new ResultModel<ClassStudentsWithGroupResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Retrieved {studentDtos.Count} students ({studentsWithGroup} with groups, {studentsWithoutGroup} without groups)",
                Data = new ClassStudentsWithGroupResponseDto
                {
                    ClassId = classId,
                    ClassName = classEntity.ClassName,
                    TotalStudents = studentDtos.Count,
                    StudentsWithGroup = studentsWithGroup,
                    StudentsWithoutGroup = studentsWithoutGroup,
                    Students = studentDtos
                },
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ClassStudentsWithGroupResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "INTERNAL_ERROR",
                Message = $"Error retrieving class students with group status: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<UnassignedStudentsResponseDto>> GetUnassignedStudentsAsync(int classId, string? searchQuery = null)
    {
        try
        {
            // 1. Ki?m tra class có t?n t?i
            var classEntity = await _classRepo.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<UnassignedStudentsResponseDto>
                {
                    IsSuccess = false,
                    ResponseCode = "CLASS_NOT_FOUND",
                    Message = "Class not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. L?y danh sách student IDs trong class
            var enrolledStudentIds = await _context.ClassEnrollments
                .Where(ce => ce.ClassId == classId)
                .Select(ce => ce.StudentId)
                .ToListAsync();

            // 3. L?y danh sách student IDs ?ã có nhóm trong class này
            var assignedStudentIds = await _context.GroupMembers
                .Where(gm => gm.Group!.ClassId == classId)
                .Select(gm => gm.UserId)
                .Distinct()
                .ToListAsync();

            // 4. Tìm students enrolled nh?ng ch?a có group
            var unassignedStudentIds = enrolledStudentIds
                .Where(id => id.HasValue && !assignedStudentIds.Contains(id.Value))
                .Select(id => id!.Value)
                .ToList();

            // 5. Query students v?i search filter (n?u có)
            var query = _context.Users
                .Where(u => unassignedStudentIds.Contains(u.UserId));

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var search = searchQuery.ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(search)) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)));
            }

            var unassignedStudents = await query
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // 6. L?y enrollment date cho m?i student
            var enrollmentDates = await _context.ClassEnrollments
                .Where(ce => ce.ClassId == classId && unassignedStudentIds.Contains(ce.StudentId!.Value))
                .ToDictionaryAsync(ce => ce.StudentId!.Value, ce => ce.EnrolledAt);

            // 7. Map to DTOs
            var studentDtos = unassignedStudents.Select(s => new UnassignedStudentDto
            {
                UserId = s.UserId,
                FullName = s.FullName,
                Email = s.Email,
                EnrolledAt = enrollmentDates.ContainsKey(s.UserId) ? enrollmentDates[s.UserId] : null
            }).ToList();

            return new ResultModel<UnassignedStudentsResponseDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = $"Found {studentDtos.Count} unassigned students",
                Data = new UnassignedStudentsResponseDto
                {
                    ClassId = classId,
                    ClassName = classEntity.ClassName,
                    TotalUnassignedStudents = studentDtos.Count,
                    Students = studentDtos
                },
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<UnassignedStudentsResponseDto>
            {
                IsSuccess = false,
                ResponseCode = "INTERNAL_ERROR",
                Message = $"Error retrieving unassigned students: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }

    public async Task<ResultModel<ImportStudentsResultDto>> ImportStudentsFromExcelAsync(int classId, IFormFile excelFile)
    {
        try
        {
            // 1. Validate class exists
            var classEntity = await _classRepo.GetByIdAsync(classId);
            if (classEntity == null)
            {
                return new ResultModel<ImportStudentsResultDto>
                {
                    IsSuccess = false,
                    ResponseCode = "CLASS_NOT_FOUND",
                    Message = "Class not found",
                    Data = null,
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            // 2. Validate Excel file
            if (excelFile == null || excelFile.Length == 0)
            {
                return new ResultModel<ImportStudentsResultDto>
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_FILE",
                    Message = "Excel file is required",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            if (!excelFile.FileName.EndsWith(".xlsx") && !excelFile.FileName.EndsWith(".xls"))
            {
                return new ResultModel<ImportStudentsResultDto>
                {
                    IsSuccess = false,
                    ResponseCode = "INVALID_FILE_FORMAT",
                    Message = "Only Excel files (.xlsx, .xls) are allowed",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var successList = new List<ImportStudentSuccessDto>();
            var failedList = new List<ImportStudentFailureDto>();
            var rowsData = new List<StudentImportRowDto>();

            // 3. Read Excel file
            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;

                    if (rowCount < 2)
                    {
                        return new ResultModel<ImportStudentsResultDto>
                        {
                            IsSuccess = false,
                            ResponseCode = "EMPTY_FILE",
                            Message = "Excel file is empty or has no data rows",
                            Data = null,
                            StatusCode = StatusCodes.Status400BadRequest
                        };
                    }

                    // Read data from row 2 onwards (row 1 is header)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var email = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var status = worksheet.Cells[row, 2].Value?.ToString()?.Trim();

                        if (string.IsNullOrWhiteSpace(email))
                        {
                            continue; // Skip empty rows
                        }

                        rowsData.Add(new StudentImportRowDto
                        {
                            RowNumber = row,
                            Email = email,
                            Status = status
                        });
                    }
                }
            }

            if (!rowsData.Any())
            {
                return new ResultModel<ImportStudentsResultDto>
                {
                    IsSuccess = false,
                    ResponseCode = "NO_DATA",
                    Message = "No valid data found in Excel file",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            // 4. Get existing enrollments for this class
            var existingEnrollments = await _context.ClassEnrollments
                .Where(ce => ce.ClassId == classId)
                .Select(ce => ce.StudentId)
                .ToListAsync();

            // 5. Process each row
            var enrolledAt = DateTime.UtcNow;
            var emailsToProcess = rowsData.Select(r => r.Email.ToLower()).ToList();

            // Get all users by email in one query
            var usersDict = await _context.Users
                .Where(u => emailsToProcess.Contains(u.Email.ToLower()))
                .ToDictionaryAsync(u => u.Email.ToLower(), u => u);

            foreach (var row in rowsData)
            {
                var emailLower = row.Email.ToLower();

                // Validation 1: Email exists in system
                if (!usersDict.ContainsKey(emailLower))
                {
                    failedList.Add(new ImportStudentFailureDto
                    {
                        RowNumber = row.RowNumber,
                        Email = row.Email,
                        Status = row.Status,
                        Reason = "Email không t?n t?i trong h? th?ng",
                        ReasonCode = "EMAIL_NOT_FOUND"
                    });
                    continue;
                }

                var user = usersDict[emailLower];

                // Validation 2: Check if user is a student (role_id = 3)
                if (user.RoleId != 3)
                {
                    failedList.Add(new ImportStudentFailureDto
                    {
                        RowNumber = row.RowNumber,
                        Email = row.Email,
                        Status = row.Status,
                        Reason = "Ng??i dùng không ph?i là sinh viên",
                        ReasonCode = "NOT_STUDENT"
                    });
                    continue;
                }

                // Validation 3: Student already in class (check duplicate)
                if (existingEnrollments.Contains(user.UserId))
                {
                    failedList.Add(new ImportStudentFailureDto
                    {
                        RowNumber = row.RowNumber,
                        Email = row.Email,
                        Status = row.Status,
                        Reason = "Sinh vi?n ?? c? trong l?p",
                        ReasonCode = "DUPLICATE"
                    });
                    continue;
                }

                // Validation 3.5: Check if student has already passed the course
                var isEligible = await _studentCourseHistoryRepo.IsEligibleForEnrollmentAsync(user.UserId);
                if (!isEligible)
                {
                    failedList.Add(new ImportStudentFailureDto
                    {
                        RowNumber = row.RowNumber,
                        Email = row.Email,
                        Status = row.Status,
                        Reason = "Sinh viên ?ã hoàn thành môn IoT (không th? thêm vào l?p)",
                        ReasonCode = "ALREADY_PASSED_COURSE"
                    });
                    continue;
                }

                // All validations passed - add to success list
                successList.Add(new ImportStudentSuccessDto
                {
                    RowNumber = row.RowNumber,
                    Email = user.Email,
                    StudentName = user.FullName ?? "Unknown",
                    UserId = user.UserId
                });

                // Add to existing enrollments to prevent duplicates in same import
                existingEnrollments.Add(user.UserId);
            }

            // 6. Bulk insert successful enrollments
            if (successList.Any())
            {
                var enrollments = successList.Select(s => new ClassEnrollmentModel
                {
                    ClassId = classId,
                    StudentId = s.UserId,
                    EnrolledAt = enrolledAt
                }).ToList();

                await _context.ClassEnrollments.AddRangeAsync(enrollments);
                await _context.SaveChangesAsync();
            }

            // 7. Prepare result
            var result = new ImportStudentsResultDto
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                TotalRows = rowsData.Count,
                SuccessCount = successList.Count,
                FailedCount = failedList.Count,
                SuccessfulStudents = successList,
                FailedStudents = failedList,
                Message = $"Imported {successList.Count} students successfully, {failedList.Count} failed"
            };

            return new ResultModel<ImportStudentsResultDto>
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = result.Message,
                Data = result,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex)
        {
            return new ResultModel<ImportStudentsResultDto>
            {
                IsSuccess = false,
                ResponseCode = "INTERNAL_ERROR",
                Message = $"Error importing students: {ex.Message}",
                Data = null,
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
