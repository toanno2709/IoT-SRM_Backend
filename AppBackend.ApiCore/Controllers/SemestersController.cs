using Microsoft.AspNetCore.Mvc;
using AppBackend.Services.Services.Semester;
using AppBackend.Services.ApiModels.Commons;
using AppBackend.Services.ApiModels.Semester;

namespace AppBackend.ApiCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemestersController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemestersController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        /// <summary>
        /// Get all semesters
        /// </summary>
        /// <returns>List of semesters</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ResultModel<List<SemesterResponseDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResultModel<List<SemesterResponseDto>>>> GetAll()
        {
            var result = await _semesterService.GetAllSemestersAsync();
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get semester by ID
        /// </summary>
        /// <param name="id">Semester ID</param>
        /// <returns>Semester details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResultModel<SemesterDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultModel<SemesterDetailDto>>> GetById(int id)
        {
            var result = await _semesterService.GetSemesterByIdAsync(id);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get active semester
        /// </summary>
        /// <returns>Active semester</returns>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ResultModel<SemesterResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultModel<SemesterResponseDto>>> GetActive()
        {
            var result = await _semesterService.GetActiveSemesterAsync();
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Get semesters by year
        /// </summary>
        /// <param name="year">Year</param>
        /// <returns>List of semesters for the specified year</returns>
        [HttpGet("year/{year}")]
        [ProducesResponseType(typeof(ResultModel<List<SemesterResponseDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ResultModel<List<SemesterResponseDto>>>> GetByYear(int year)
        {
            var result = await _semesterService.GetSemestersByYearAsync(year);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Create a new semester
        /// </summary>
        /// <param name="request">Semester creation data</param>
        /// <returns>Created semester</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResultModel<SemesterResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultModel<SemesterResponseDto>>> Create([FromBody] CreateSemesterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<SemesterResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var result = await _semesterService.CreateSemesterAsync(request);
            
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetById), new { id = result.Data?.SemesterId }, result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Update semester
        /// </summary>
        /// <param name="id">Semester ID</param>
        /// <param name="request">Semester update data</param>
        /// <returns>Updated semester</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ResultModel<SemesterResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultModel<SemesterResponseDto>>> Update(int id, [FromBody] UpdateSemesterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResultModel<SemesterResponseDto>
                {
                    IsSuccess = false,
                    Message = "Invalid input data",
                    Data = null,
                    StatusCode = StatusCodes.Status400BadRequest
                });
            }

            var result = await _semesterService.UpdateSemesterAsync(id, request);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Delete semester
        /// </summary>
        /// <param name="id">Semester ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ResultModel<bool>>> Delete(int id)
        {
            var result = await _semesterService.DeleteSemesterAsync(id);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Set active semester
        /// </summary>
        /// <param name="id">Semester ID to set as active</param>
        /// <returns>Activation result</returns>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(ResultModel<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ResultModel<bool>>> SetActive(int id)
        {
            var result = await _semesterService.SetActiveSemesterAsync(id);
            
            if (result.IsSuccess)
                return Ok(result);
            
            return StatusCode(result.StatusCode, result);
        }
    }
}
