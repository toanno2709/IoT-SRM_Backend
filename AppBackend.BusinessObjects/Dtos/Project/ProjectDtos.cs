using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBackend.BusinessObjects.Dtos.Project
{
    public class ProjectCreateDto
    {
        public int GroupId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Component { get; set; }
    }

    public class ProjectCreateResultDto
    {
        public int ProjectId { get; set; }
        public string Title { get; set; } = null!;
        public string? Status { get; set; }

        public ProjectCreateResultDto(int id, string title, string? status)
        {
            ProjectId = id;
            Title = title;
            Status = status;
        }
    }

    public class ProjectUpdateDto
    {
        public int ProjectId { get; set; }
        public int RequesterUserId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Component { get; set; }
    }

    public class ProjectStatusDto
    {
        public int ProjectId { get; set; }
        public int InstructorId { get; set; }
        public string Status { get; set; } = null!;
        public string? Comment { get; set; }
    }

    public class ProjectListItemDto
    {
        public int ProjectId { get; set; }
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
    }

    public class ProjectDetailDto
    {
        public int ProjectId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Component { get; set; }
        public string? Status { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> MemberNames { get; set; } = new();
    }
}
