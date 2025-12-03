using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// Request DTO for importing users from Excel file
/// </summary>
public class ImportUsersFromExcelRequestDto
{
    /// <summary>
    /// Excel file to upload (.xlsx or .xls)
    /// </summary>
    [Required(ErrorMessage = "Excel file is required")]
    public IFormFile ExcelFile { get; set; } = null!;

    /// <summary>
    /// Default role ID for imported users (3 = Student by default)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Role ID must be between 1 and 10")]
    public int DefaultRoleId { get; set; } = 3;
}

/// <summary>
/// Response DTO after importing users from Excel
/// </summary>
public class ImportUsersResponseDto
{
    public int TotalRowsInFile { get; set; }
    public int UsersCreatedSuccessfully { get; set; }
    public int UsersSkipped { get; set; }
    public int UsersFailed { get; set; }
    public string? Message { get; set; }
    public List<ImportedUserDto> SuccessfulUsers { get; set; } = new();
    public List<ImportErrorDto> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// DTO for successfully imported user
/// </summary>
public class ImportedUserDto
{
    public int RowNumber { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// DTO for import error
/// </summary>
public class ImportErrorDto
{
    public int RowNumber { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public string? ErrorReason { get; set; }
    public string? ErrorType { get; set; } // "DuplicateEmail", "DuplicatePhone", "ValidationError", "InvalidFormat"
}

/// <summary>
/// DTO for user data from Excel row
/// </summary>
public class ExcelUserRowDto
{
    public int RowNumber { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
}
