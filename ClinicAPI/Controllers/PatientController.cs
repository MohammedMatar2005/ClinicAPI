using ClinicAPIBusiness.DTO.PatientsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/Patients")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly clsPatient _patientService;
        private readonly ILogger<PatientController> _logger;

        public PatientController(clsPatient patientService, ILogger<PatientController> logger)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب قائمة جميع المرضى المسجلين في النظام
        /// </summary>
        [HttpGet("GetAll", Name = "GetAllPatients")]
        [ProducesResponseType(typeof(List<PatientViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientViewDTO>>> GetAllPatients()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Audit: Admin action executed. AdminId={AdminId}, Action={Action}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "GetAllPatients", "PatientList", ipAddress);

            var patients = await _patientService.GetAllPatientsAsync();
            return Ok(patients);
        }

        /// <summary>
        /// جلب بيانات مريض بناءً على معرفه
        /// </summary>
        [Authorize(Roles = "Admin, Receptionist, Doctor")]
        [HttpGet("GetById/{patientId:int}", Name = "GetPatientById")]
        [ProducesResponseType(typeof(PatientViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PatientViewDTO>> GetPatientById(int patientId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (patientId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Get patient failed (Invalid ID). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, patientId, "GetPatientById", ipAddress);
                return BadRequest("Invalid patient ID.");
            }

            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                _logger.LogWarning(
                    "Audit: Get patient failed (Not Found). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, patientId, "GetPatientById", ipAddress);
                return NotFound($"Patient with ID {patientId} not found.");
            }

            return Ok(patient);
        }

        /// <summary>
        /// إضافة مريض جديد في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreatePatient")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<int>> CreatePatient([FromBody] PatientSaveDTO patientSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (patientSaveDto == null)
            {
                _logger.LogWarning(
                    "Audit: Create patient failed (Data is null). AdminId={AdminId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, "CreatePatient", ipAddress);
                return BadRequest("Patient data is required.");
            }

            try
            {
                int newPatientId = await _patientService.AddNewPatientAsync(patientSaveDto);

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "CreatePatient", newPatientId, "Patient", ipAddress);

                return CreatedAtRoute("GetPatientById", new { patientId = newPatientId }, newPatientId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Create patient failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, "CreatePatient", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات مريض في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{patientId:int}", Name = "UpdatePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePatient(int patientId, [FromBody] PatientSaveDTO patientSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (patientSaveDto == null)
            {
                return BadRequest("Patient data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (patientId != patientSaveDto.PatientDetails.PatientId)
            {
                _logger.LogWarning(
                    "Audit: Update patient failed (ID Mismatch). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, patientId, "UpdatePatient", ipAddress);
                return BadRequest("Mismatched patient id between route and body.");
            }

            try
            {
                bool isUpdated = await _patientService.UpdatePatientAsync(patientSaveDto);

                if (!isUpdated)
                {
                    _logger.LogWarning(
                        "Audit: Update patient failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, patientId, "UpdatePatient", ipAddress);
                    return NotFound($"No patient found with id {patientId}.");
                }

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "UpdatePatient", patientId, "Patient", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Update patient failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, patientId, "UpdatePatient", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف مريض من النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{patientId:int}", Name = "DeletePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeletePatient(int patientId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (patientId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Delete patient failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, patientId, "DeletePatient", ipAddress);
                return BadRequest("Invalid patient ID.");
            }

            bool isDeleted = await _patientService.DeletePatientAsync(patientId);

            if (!isDeleted)
            {
                _logger.LogWarning(
                    "Audit: Delete patient failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, patientId, "DeletePatient", ipAddress);
                return NotFound($"Patient with ID {patientId} not found.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "DeletePatient", patientId, "Patient", ipAddress);

            return NoContent();
        }

        /// <summary>
        /// تعديل جزئي على بيانات مريض
        /// </summary>
        [HttpPatch("{patientId:int}", Name = "PatchPatient")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PatchPatient(int patientId, [FromBody] PatientPatchDTO patchDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (patientId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Patch patient failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, patientId, "PatchPatient", ipAddress);
                return BadRequest("Invalid patient ID.");
            }

            if (patchDto == null)
            {
                _logger.LogWarning(
                    "Audit: Patch patient failed (Data is null). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, patientId, "PatchPatient", ipAddress);
                return BadRequest("Patch data is required.");
            }

            bool isUpdated = await _patientService.PatchPatientAsync(patientId, patchDto);

            if (!isUpdated)
            {
                _logger.LogWarning(
                    "Audit: Patch patient failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, patientId, "PatchPatient", ipAddress);
                return NotFound($"No patient found with id {patientId}.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "PatchPatient", patientId, "Patient", ipAddress);

            return NoContent();
        }
    }
}