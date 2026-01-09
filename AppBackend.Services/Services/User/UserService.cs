using AppBackend.BusinessObjects.Constants;
using AppBackend.BusinessObjects.Exceptions;
using AppBackend.BusinessObjects.Models;
using AppBackend.Repositories.Repositories.UserRepo;
using AppBackend.Services.ApiModels;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ServicesHelpers;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using AppBackend.BusinessObjects.Data;

namespace AppBackend.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly UserHelper _userHelper;
        private readonly IotShowroomContext _context;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            UserHelper userHelper,
            IotShowroomContext context)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _userHelper = userHelper;
            _context = context;
            
            // Set EPPlus License Context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Helper method to create StudentCourseHistory record for a new student
        /// </summary>
        private async Task CreateStudentCourseHistoryAsync(int studentId)
        {
            try
            {
                // Create new StudentCourseHistory record with nullable semester and final submission
                var history = new StudentCourseHistory
                {
                    StudentId = studentId,
                    SemesterId = null, // Will be updated when student enrolls in a class
                    FinalSubmissionId = null, // Will be updated when student submits final project
                    Status = "Not Started",
                    IsCurrent = true,
                    IsRetake = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.StudentCourseHistories.AddAsync(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Log error but don't fail the user creation process
                // This is a non-critical operation
            }
        }

        public async Task<ResultModel> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = userDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            var dto = _mapper.Map<UserDto>(user);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = CommonMessageConstants.GET_SUCCESS,
                Data = dto,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetCurrentUserInfoAsync(int userId)
        {
            var user = await _userRepository.GetByIdWithRoleAsync(userId);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            var userResponse = _mapper.Map<UserResponseDto>(user);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Current user information retrieved successfully",
                Data = userResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> CreateUserAsync(CreateUserRequest request)
        {
            // Check email duplication
            var existing = await _userRepository.GetByEmailAsync(request.Email);
            if (existing != null)
                throw new AppException(
                    CommonMessageConstants.EXISTED,
                    string.Format(CommonMessageConstants.VALUE_DUPLICATED, "Email"),
                    StatusCodes.Status409Conflict
                );

            // Verify role exists (you may need to add a RoleRepository check here)
            // For now, we'll assume the role exists

            // Map & hash password
            var newUser = _mapper.Map<User>(request);
            newUser.PasswordHash = _userHelper.HashPassword(request.Password);
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            // Auto-create StudentCourseHistory if user is a Student (role_id = 3)
            if (newUser.RoleId == 3)
            {
                await CreateStudentCourseHistoryAsync(newUser.UserId);
            }

            // Get user with role information
            var createdUser = await _userRepository.GetByIdWithRoleAsync(newUser.UserId);
            var userResponse = _mapper.Map<UserResponseDto>(createdUser);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User created successfully",
                Data = userResponse,
                StatusCode = StatusCodes.Status201Created
            };
        }

        public async Task<ResultModel> UpdateUserAsync(int id, UpdateUserRequest request, int requesterId, bool isAdmin)
        {
            // Authorization check: Only admin or the user themselves can update their info
            if (!isAdmin && id != requesterId)
                throw new AppException(
                    CommonMessageConstants.UNAUTHORIZED,
                    "You are not authorized to update this user's information",
                    StatusCodes.Status403Forbidden
                );

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrWhiteSpace(request.Phone))
                user.Phone = request.Phone;

            if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                user.AvatarUrl = request.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
                user.PasswordHash = _userHelper.HashPassword(request.NewPassword);

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Get updated user with role information
            var updatedUser = await _userRepository.GetByIdWithRoleAsync(user.UserId);
            var userResponse = _mapper.Map<UserResponseDto>(updatedUser);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User updated successfully",
                Data = userResponse,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new AppException(
                    CommonMessageConstants.NOT_FOUND,
                    string.Format(CommonMessageConstants.VALUE_NOT_FOUND, "User"),
                    StatusCodes.Status404NotFound
                );

            // Optional: Check if user has related data that prevents deletion
            // For example, check if user is referenced in other tables

            await _userRepository.DeleteAsync(user);
            await _userRepository.SaveChangesAsync();

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "User deleted successfully",
                Data = true,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel> GetUsersByRoleAsync(int? roleId)
        {
            IEnumerable<User> users;

            if (roleId.HasValue)
            {
                // Filter by role
                users = await _userRepository.GetByRoleAsync(roleId.Value);
            }
            else
            {
                // Get all users
                users = await _userRepository.GetAllAsync();
            }

            var userDtos = _mapper.Map<IEnumerable<UserResponseDto>>(users);

            return new ResultModel
            {
                IsSuccess = true,
                ResponseCode = CommonMessageConstants.SUCCESS,
                Message = "Users retrieved successfully",
                Data = userDtos,
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ResultModel<ImportUsersResponseDto>> ImportUsersFromExcelAsync(ImportUsersFromExcelRequestDto request)
        {
            var response = new ImportUsersResponseDto
            {
                SuccessfulUsers = new List<ImportedUserDto>(),
                Errors = new List<ImportErrorDto>(),
                Warnings = new List<string>()
            };

            try
            {
                // Validate file extension
                var fileExtension = Path.GetExtension(request.ExcelFile.FileName).ToLower();
                if (fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    throw new AppException(
                        CommonMessageConstants.INVALID,
                        "Only .xlsx and .xls files are supported",
                        StatusCodes.Status400BadRequest
                    );
                }

                // Validate file size (max 10MB)
                if (request.ExcelFile.Length > 10 * 1024 * 1024)
                {
                    throw new AppException(
                        CommonMessageConstants.INVALID,
                        "File size must not exceed 10MB",
                        StatusCodes.Status400BadRequest
                    );
                }

                using var stream = new MemoryStream();
                await request.ExcelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0]; // First sheet

                if (worksheet == null || worksheet.Dimension == null)
                {
                    throw new AppException(
                        CommonMessageConstants.INVALID,
                        "Excel file is empty or invalid",
                        StatusCodes.Status400BadRequest
                    );
                }

                var rowCount = worksheet.Dimension.Rows;
                response.TotalRowsInFile = rowCount - 1; // Exclude header row

                // Get all existing emails and phones from database
                var allUsers = await _userRepository.GetAllAsync();
                var existingEmails = new HashSet<string>(
                    allUsers.Where(u => !string.IsNullOrEmpty(u.Email))
                           .Select(u => u.Email.ToLower()),
                    StringComparer.OrdinalIgnoreCase
                );
                var existingPhones = new HashSet<string>(
                    allUsers.Where(u => !string.IsNullOrEmpty(u.Phone))
                           .Select(u => u.Phone.Trim()),
                    StringComparer.OrdinalIgnoreCase
                );

                // Track emails and phones in current import batch
                var batchEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var batchPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Process each row (skip header row 1)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Read data from Excel (columns: A=No, B=Fullname, C=Email, D=Phone, E=Role, F=Password)
                        var fullName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var email = worksheet.Cells[row, 3].Value?.ToString()?.Trim()?.ToLower();
                        var phone = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                        var role = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        var password = worksheet.Cells[row, 6].Value?.ToString()?.Trim();

                        // Validate required fields
                        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
                        {
                            response.Errors.Add(new ImportErrorDto
                            {
                                RowNumber = row,
                                FullName = fullName,
                                Email = email,
                                PhoneNumber = phone,
                                ErrorReason = "Full name and email are required",
                                ErrorType = "ValidationError"
                            });
                            response.UsersFailed++;
                            continue;
                        }

                        // Validate email format
                        if (!IsValidEmail(email))
                        {
                            response.Errors.Add(new ImportErrorDto
                            {
                                RowNumber = row,
                                FullName = fullName,
                                Email = email,
                                PhoneNumber = phone,
                                ErrorReason = "Invalid email format",
                                ErrorType = "InvalidFormat"
                            });
                            response.UsersFailed++;
                            continue;
                        }

                        // Check duplicate email in database
                        if (existingEmails.Contains(email))
                        {
                            response.Errors.Add(new ImportErrorDto
                            {
                                RowNumber = row,
                                FullName = fullName,
                                Email = email,
                                PhoneNumber = phone,
                                ErrorReason = "Email already exists in database",
                                ErrorType = "DuplicateEmail"
                            });
                            response.UsersSkipped++;
                            continue;
                        }

                        // Check duplicate email in current batch
                        if (batchEmails.Contains(email))
                        {
                            response.Errors.Add(new ImportErrorDto
                            {
                                RowNumber = row,
                                FullName = fullName,
                                Email = email,
                                PhoneNumber = phone,
                                ErrorReason = "Duplicate email in Excel file",
                                ErrorType = "DuplicateEmail"
                            });
                            response.UsersSkipped++;
                            continue;
                        }

                        // Check duplicate phone in database (if phone is provided)
                        if (!string.IsNullOrEmpty(phone))
                        {
                            if (existingPhones.Contains(phone))
                            {
                                response.Errors.Add(new ImportErrorDto
                                {
                                    RowNumber = row,
                                    FullName = fullName,
                                    Email = email,
                                    PhoneNumber = phone,
                                    ErrorReason = "Phone number already exists in database",
                                    ErrorType = "DuplicatePhone"
                                });
                                response.UsersSkipped++;
                                continue;
                            }

                            // Check duplicate phone in current batch
                            if (batchPhones.Contains(phone))
                            {
                                response.Errors.Add(new ImportErrorDto
                                {
                                    RowNumber = row,
                                    FullName = fullName,
                                    Email = email,
                                    PhoneNumber = phone,
                                    ErrorReason = "Duplicate phone number in Excel file",
                                    ErrorType = "DuplicatePhone"
                                });
                                response.UsersSkipped++;
                                continue;
                            }
                        }

                        // Determine RoleId - Accept Student, Instructor, and Admin roles
                        int roleId = 3; // Default to Student
                        if (!string.IsNullOrEmpty(role))
                        {
                            if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
                            {
                                roleId = 3; // Student
                            }
                            else if (role.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                            {
                                roleId = 2; // Instructor
                            }
                            else if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                            {
                                roleId = 1; // Admin
                            }
                            else
                            {
                                // Unknown role - treat as warning but import as Student
                                response.Warnings.Add($"Row {row}: Unknown role '{role}', imported as Student");
                                roleId = 3; // Default to Student
                            }
                        }
                        else
                        {
                            // No role specified - use default Student
                            roleId = 3;
                        }

                        // Use provided password or default password
                        var userPassword = !string.IsNullOrEmpty(password) ? password : "12345678";

                        // Create new user
                        var newUser = new User
                        {
                            FullName = fullName,
                            Email = email,
                            Phone = phone,
                            RoleId = roleId,
                            PasswordHash = _userHelper.HashPassword(userPassword),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await _userRepository.AddAsync(newUser);
                        await _userRepository.SaveChangesAsync();

                        // Auto-create StudentCourseHistory if user is a Student (role_id = 3)
                        if (newUser.RoleId == 3)
                        {
                            await CreateStudentCourseHistoryAsync(newUser.UserId);
                        }

                        // Add to batch tracking
                        batchEmails.Add(email);
                        if (!string.IsNullOrEmpty(phone))
                            batchPhones.Add(phone);

                        // Add to existing tracking (for next iterations)
                        existingEmails.Add(email);
                        if (!string.IsNullOrEmpty(phone))
                            existingPhones.Add(phone);

                        // Add to successful list
                        response.SuccessfulUsers.Add(new ImportedUserDto
                        {
                            RowNumber = row,
                            UserId = newUser.UserId,
                            FullName = newUser.FullName,
                            Email = newUser.Email,
                            PhoneNumber = newUser.Phone
                        });
                        response.UsersCreatedSuccessfully++;
                    }
                    catch (Exception ex)
                    {
                        response.Errors.Add(new ImportErrorDto
                        {
                            RowNumber = row,
                            ErrorReason = $"Error processing row: {ex.Message}",
                            ErrorType = "ProcessingError"
                        });
                        response.UsersFailed++;
                    }
                }

                // Build response message
                response.Message = $"Import completed: {response.UsersCreatedSuccessfully} users created, " +
                                 $"{response.UsersSkipped} skipped (duplicates), {response.UsersFailed} failed";

                return new ResultModel<ImportUsersResponseDto>
                {
                    IsSuccess = true,
                    ResponseCode = CommonMessageConstants.SUCCESS,
                    Message = response.Message,
                    Data = response,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                throw new AppException(
                    CommonMessageConstants.ERROR,
                    $"Error importing users: {ex.Message}",
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
