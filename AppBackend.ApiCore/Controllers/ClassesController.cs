using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AppBackend.Services.Services.Class;
using AppBackend.Services.ApiModels.Commons;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassesController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassesController(IClassService classService)
        {
            _classService = classService;
        }

        /// <summary>
        /// Get all classes
        /// </summary>
        /// <returns>List of all classes</returns>
        [HttpGet]
        [Authorize(Roles = "Admin,Instructor")]
        [ProducesResponseType(typeof(ResultModel<List<ClassResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ResultModel<List<ClassResponseDto>>>> GetAll()
        {
            var result = await _classService.GetAllClassesAsync();
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get class by ID
        /// </summary>
        /// <param name="id">Class ID</param>
        /// <returns>Class information</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Instructor,Student")]
        [ProducesResponseType(typeof(ResultModel<ClassResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ResultModel<ClassResponseDto>>> GetById(int id)
        {
            var result = await _classService.GetClassByIdAsync(id);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get detailed class information including groups and students
        /// </summary>
        /// <param name="id">Class ID</param>
        /// <returns>Detailed class information</returns>
        [HttpGet("{id}/details")]
        [Authorize(Roles = "Admin,Instructor")]
        [ProducesResponseType(typeof(ResultModel<ClassDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ResultModel<ClassDetailDto>>> GetDetails(int id)
        {
            var result = await _classService.GetClassDetailAsync(id);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get classes by semester
        /// </summary>
        /// <param name="semesterId">Semester ID</param>
        /// <returns>List of classes in the semester</returns>
        [HttpGet("semester/{semesterId}")]
        [Authorize(Roles = "Admin,Instructor")]
        [ProducesResponseType(typeof(ResultModel<List<ClassResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ResultModel<List<ClassResponseDto>>>> GetBySemester(int semesterId)
        {
            var result = await _classService.GetClassesBySemesterAsync(semesterId);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Search classes by semester and/or query string
        /// </summary>
        /// <param name="semesterId">Semester ID (optional)</param>
        /// <param name="q">Search query (class name, description, or instructor name)</param>
        /// <returns>List of matching classes</returns>
        [HttpGet("search")]
        [Authorize(Roles = "Admin,Instructor")]
        [ProducesResponseType(typeof(ResultModel<List<ClassResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ResultModel<List<ClassResponseDto>>>> Search([FromQuery] int? semesterId, [FromQuery] string? q)
        {
            var result = await _classService.SearchClassesAsync(semesterId, q);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Create a new class
        /// </summary>
        /// <param name="request">Class creation data</param>
        /// <returns>Created class</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResultModel<ClassResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ResultModel<ClassResponseDto>>> Create([FromBody] CreateClassRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var result = await _classService.CreateClassAsync(request);
            
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetById), new { id = result.Data?.ClassId }, result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update class information
        /// </summary>
        /// <param name="id">Class ID</param>
        /// <param name="request">Class update data</param>
        /// <returns>Updated class</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResultModel<ClassResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ResultModel<ClassResponseDto>>> Update(int id, [FromBody] UpdateClassRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<ClassResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var result = await _classService.UpdateClassAsync(id, request);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Delete a class
        /// </summary>
        /// <param name="id">Class ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ResultModel<bool>>> Delete(int id)
        {
            var result = await _classService.DeleteClassAsync(id);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Assign or change instructor for a class
        /// </summary>
        /// <param name="classId">Class ID</param>
        /// <param name="request">Instructor assignment data</param>
        /// <returns>Assignment result</returns>
        [HttpPut("{classId}/instructor")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ResultModel<bool>>> AssignInstructor(int classId, [FromBody] AssignInstructorRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<bool>
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    Data = false,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var result = await _classService.AssignInstructorAsync(classId, request.InstructorId);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Change class status (Admin only)
        /// </summary>
        /// <param name="classId">Class ID</param>
        /// <param name="request">Status change request with new status</param>
        /// <returns>Status change result with validation details</returns>
        /// <remarks>
        /// Allows admin to change class status between: Not Started, In Progress, Completed
        /// 
        /// **Validation Rules:**
        /// 
        /// When changing to "In Progress":
        /// - All enrolled students MUST be in a group
        /// - If any student doesn't have a group, the request will be rejected
        /// - Returns list of students without groups if validation fails
        /// 
        /// **Status Flow:**
        /// - Not Started ? In Progress ? Completed
        /// - Can change in any direction (e.g., In Progress ? Not Started is allowed)
        /// 
        /// **Response includes:**
        /// - Old and new status
        /// - Total students count
        /// - Students with group count
        /// - Students without group count
        /// - List of warnings/student names if validation fails
        /// 
        /// **Example Scenarios:**
        /// 
        /// Success (all students have groups):
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "data": {
        ///     "classId": 1,
        ///     "className": "SE1234",
        ///     "oldStatus": "Not Started",
        ///     "newStatus": "In Progress",
        ///     "totalStudents": 30,
        ///     "studentsWithGroup": 30,
        ///     "studentsWithoutGroup": 0
        ///   }
        /// }
        /// ```
        /// 
        /// Failure (students without groups):
        /// ```json
        /// {
        ///   "isSuccess": false,
        ///   "responseCode": "STUDENTS_WITHOUT_GROUP",
        ///   "message": "Cannot change status: 3 students do not have a group yet",
        ///   "data": {
        ///     "studentsWithoutGroup": 3,
        ///     "warnings": ["John Doe", "Jane Smith", "Bob Wilson"]
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpPut("{classId}/change-status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResultModel<ChangeClassStatusResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ResultModel<ChangeClassStatusResponseDto>>> ChangeClassStatus(
            int classId, 
            [FromBody] ChangeClassStatusRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<ChangeClassStatusResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid input data. Status must be 'Not Started', 'In Progress', or 'Completed'",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var result = await _classService.ChangeClassStatusAsync(classId, request);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }
    }
}
