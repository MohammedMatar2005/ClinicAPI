using ClinicAPIBusiness.DTO.PatientVisitsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/PatientVisits")]
    [ApiController]
    public class PatientVisitsController : ControllerBase
    {
        private readonly clsPatientVisit _patientVisitService;
        private readonly ILogger<PatientVisitsController> _logger;

        public PatientVisitsController(clsPatientVisit patientVisitService, ILogger<PatientVisitsController> logger)
        {
            _patientVisitService = patientVisitService ?? throw new ArgumentNullException(nameof(patientVisitService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب قائمة جميع الزيارات المسجلة في النظام
        /// </summary>
        [HttpGet("GetAll", Name = "GetAllPatientVisits")]
        [ProducesResponseType(typeof(List<PatientVisitViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientVisitViewDTO>>> GetAllPatientVisits()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Audit: Admin action executed. AdminId={AdminId}, Action={Action}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "GetAllPatientVisits", "PatientVisitList", ipAddress);

            var visits = await _patientVisitService.GetAllPatientVisitsAsync();
            return Ok(visits);
        }

        /// <summary>
        /// جلب بيانات زيارة بناءً على معرفها
        /// </summary>
        [Authorize(Roles = "Admin, Receptionist, Doctor")]
        [HttpGet("GetById/{visitId:int}", Name = "GetPatientVisitById")]
        [ProducesResponseType(typeof(PatientVisitViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientVisitViewDTO>> GetPatientVisitById(
            int visitId,
            [FromServices] IAuthorizationService authorizationService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (visitId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Get patient visit failed (Invalid ID). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, visitId, "GetPatientVisitById", ipAddress);
                return BadRequest("Invalid visit ID.");
            }

            var visit = await _patientVisitService.GetPatientVisitByIdAsync(visitId);
            if (visit == null)
            {
                _logger.LogWarning(
                    "Audit: Get patient visit failed (Not Found). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, visitId, "GetPatientVisitById", ipAddress);
                return NotFound($"Visit with ID {visitId} not found.");
            }

            // تفويض مهمة فحص الصلاحية والملكية بالكامل لـ Policy
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                visit,
                "CanAccessPatientVisit");

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "Audit: Get patient visit failed (Forbidden/Unauthorized access). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, visitId, "GetPatientVisitById", ipAddress);
                return Forbid(); // 403 Forbidden
            }

            return Ok(visit);
        }

        /// <summary>
        /// تسجيل زيارة مريض جديدة في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreatePatientVisit")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<int>> CreatePatientVisit([FromBody] PatientVisitSaveDTO visitSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (visitSaveDto == null)
            {
                _logger.LogWarning(
                    "Audit: Create patient visit failed (Data is null). AdminId={AdminId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, "CreatePatientVisit", ipAddress);
                return BadRequest("Visit data is required.");
            }

            try
            {
                int newVisitId = await _patientVisitService.AddNewPatientVisitAsync(visitSaveDto);

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "CreatePatientVisit", newVisitId, "PatientVisit", ipAddress);

                return CreatedAtRoute("GetPatientVisitById", new { visitId = newVisitId }, newVisitId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Create patient visit failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, "CreatePatientVisit", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات زيارة مريض
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{visitId:int}", Name = "UpdatePatientVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePatientVisit(int visitId, [FromBody] PatientVisitSaveDTO visitSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (visitSaveDto == null)
            {
                return BadRequest("Visit data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (visitId != visitSaveDto.VisitId)
            {
                _logger.LogWarning(
                    "Audit: Update patient visit failed (ID Mismatch). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, visitId, "UpdatePatientVisit", ipAddress);
                return BadRequest("Mismatched visit id between route and body.");
            }

            try
            {
                bool isUpdated = await _patientVisitService.UpdatePatientVisitAsync(visitSaveDto);

                if (!isUpdated)
                {
                    _logger.LogWarning(
                        "Audit: Update patient visit failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, visitId, "UpdatePatientVisit", ipAddress);
                    return NotFound($"No visit found with id {visitId}.");
                }

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "UpdatePatientVisit", visitId, "PatientVisit", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Update patient visit failed (Validation Error). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, visitId, "UpdatePatientVisit", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف زيارة من النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{visitId:int}", Name = "DeletePatientVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeletePatientVisit(int visitId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (visitId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Delete patient visit failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, visitId, "DeletePatientVisit", ipAddress);
                return BadRequest("Invalid visit ID.");
            }

            bool isDeleted = await _patientVisitService.DeletePatientVisitAsync(visitId);

            if (!isDeleted)
            {
                _logger.LogWarning(
                    "Audit: Delete patient visit failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, visitId, "DeletePatientVisit", ipAddress);
                return NotFound($"Visit with ID {visitId} not found.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "DeletePatientVisit", visitId, "PatientVisit", ipAddress);

            return NoContent();
        }
    }
}