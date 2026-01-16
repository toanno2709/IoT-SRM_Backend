namespace AppBackend.Services.ApiModels.Commons;

/// <summary>
/// DTO for creating a new project template
/// </summary>
public class CreateProjectTemplateDto
{
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Component { get; set; }
    public int? MaxGroups { get; set; } // null = unlimited
    public List<CreateTemplateMilestoneDto> Milestones { get; set; } = new();
}

/// <summary>
/// DTO for creating a template milestone
/// </summary>
public class CreateTemplateMilestoneDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public decimal? Weight { get; set; }
    public int? DaysDuration { get; set; }
}

/// <summary>
/// DTO for project template response
/// </summary>
public class ProjectTemplateResponseDto
{
    public int TemplateId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Component { get; set; }
    public int? MaxGroups { get; set; }
    public int RegisteredCount { get; set; }
    public int? AvailableSlots { get; set; } // null = unlimited, 0 = full
    public bool IsActive { get; set; }
    public bool CanRegister { get; set; } // Calculated: is_active AND has slots
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<TemplateMilestoneDto> Milestones { get; set; } = new();
}

/// <summary>
/// DTO for template milestone
/// </summary>
public class TemplateMilestoneDto
{
    public int TemplateMilestoneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public decimal? Weight { get; set; }
    public int? DaysDuration { get; set; }
}

/// <summary>
/// DTO for available templates list (student view)
/// </summary>
public class AvailableTemplateDto
{
    public int TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Component { get; set; }
    public int? MaxGroups { get; set; }
    public int RegisteredCount { get; set; }
    public int? AvailableSlots { get; set; } // null = unlimited
    public bool CanRegister { get; set; }
    public bool IsMyGroupRegistered { get; set; } // Check if current user's group already registered
    public int? MyRegistrationId { get; set; } // Registration ID if group is registered (for cancellation)
    public int MilestoneCount { get; set; }
    public List<TemplateMilestoneDto> Milestones { get; set; } = new();
}

/// <summary>
/// DTO for registering group to a template
/// </summary>
public class RegisterTemplateDto
{
    public int TemplateId { get; set; }
    public int GroupId { get; set; }
}

/// <summary>
/// DTO for template registration response
/// </summary>
public class TemplateRegistrationResponseDto
{
    public int RegistrationId { get; set; }
    public int TemplateId { get; set; }
    public string TemplateTitle { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int ProjectId { get; set; } // Project is created immediately
    public string ProjectTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime RegisteredAt { get; set; }
    public int MilestonesCreated { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating project template
/// </summary>
public class UpdateProjectTemplateDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Component { get; set; }
    public int? MaxGroups { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for template registration list (instructor view)
/// </summary>
public class TemplateRegistrationListDto
{
    public int RegistrationId { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string RegisteredByName { get; set; } = string.Empty;
}

/// <summary>
/// DTO for template statistics (instructor view)
/// </summary>
public class TemplateStatisticsDto
{
    public int TemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? MaxGroups { get; set; }
    public int RegisteredCount { get; set; }
    public int? AvailableSlots { get; set; }
    public int ActiveRegistrations { get; set; }
    public int CancelledRegistrations { get; set; }
    public bool IsFull { get; set; }
}

/// <summary>
/// DTO for student's group registrations
/// </summary>
public class MyGroupRegistrationDto
{
    public int RegistrationId { get; set; }
    public int TemplateId { get; set; }
    public string TemplateTitle { get; set; } = string.Empty;
    public string? TemplateDescription { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int? ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public bool CanCancel { get; set; } // Can cancel if no submissions
}
