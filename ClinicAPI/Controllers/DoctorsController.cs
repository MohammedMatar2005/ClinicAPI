using ClinicAPIBusiness.DTO.DoctorsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAPI.Controllers
{
    [Route("api/Doctors")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly clsDoctor _doctorService;
        private readonly ILogger<DoctorsController> _logger;

        public DoctorsController(clsDoctor doctorService, ILogger<DoctorsController> logger)
        {
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب قائمة جميع الأطباء المسجلين في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAll", Name = "GetAllDoctors")]
        [ProducesResponseType(typeof(List<DoctorViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DoctorViewDTO>>> GetAllDoctors()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Audit: Admin action executed. AdminId={AdminId}, Action={Action}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "GetAllDoctors", "DoctorList", ipAddress);

            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        /// <summary>
        /// جلب بيانات طبيب بناءً على معرفه
        /// </summary>
        [Authorize(Roles = "Admin, Doctor, Receptionist, Nurse, Manager")]
        [HttpGet("GetById/{doctorId:int}", Name = "GetDoctorById")]
        [ProducesResponseType(typeof(DoctorViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DoctorViewDTO>> GetDoctorById(
            int doctorId,
            [FromServices] IAuthorizationService authorizationService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // 1. التحقق من صحة المعرّف الممرر في المسار
            if (doctorId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Get doctor failed (Invalid ID). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, doctorId, "GetDoctorById", ipAddress);
                return BadRequest("Invalid doctor ID.");
            }

            // 2. جلب بيانات الطبيب من قاعدة البيانات أولاً
            var doctor = await _doctorService.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                _logger.LogWarning(
                    "Audit: Get doctor failed (Not Found). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, doctorId, "GetDoctorById", ipAddress);
                return NotFound($"Doctor with ID {doctorId} not found.");
            }

            // 3. التحقق من الصلاحيات والملكية عبر الـ Policy
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                doctor,
                "CanAccessDoctor");

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "Audit: Get doctor failed (Forbidden/Unauthorized access). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, doctorId, "GetDoctorById", ipAddress);
                return Forbid(); // HTTP 403 Forbidden
            }

            // 4. إرجاع بيانات الطبيب عند اجتياز الفحوصات
            return Ok(doctor);
        }

        /// <summary>
        /// إضافة طبيب جديد في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreateDoctor")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<int>> CreateDoctor([FromBody] DoctorSaveDTO doctorSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (doctorSaveDto == null)
            {
                _logger.LogWarning(
                    "Audit: Create doctor failed (Data is null). AdminId={AdminId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, "CreateDoctor", ipAddress);
                return BadRequest("Doctor data is required.");
            }

            try
            {
                int newDoctorId = await _doctorService.AddNewDoctorAsync(doctorSaveDto);

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "CreateDoctor", newDoctorId, "Doctor", ipAddress);

                return CreatedAtRoute("GetDoctorById", new { doctorId = newDoctorId }, newDoctorId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Create doctor failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, "CreateDoctor", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات طبيب في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{doctorId:int}", Name = "UpdateDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateDoctor(int doctorId, [FromBody] DoctorSaveDTO doctorSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (doctorSaveDto == null)
            {
                return BadRequest("Doctor data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (doctorId != doctorSaveDto.DoctorDetails.DoctorId)
            {
                _logger.LogWarning(
                    "Audit: Update doctor failed (ID Mismatch). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, doctorId, "UpdateDoctor", ipAddress);
                return BadRequest("Mismatched doctor id between route and body.");
            }

            try
            {
                bool isUpdated = await _doctorService.UpdateDoctorAsync(doctorSaveDto);

                if (!isUpdated)
                {
                    _logger.LogWarning(
                        "Audit: Update doctor failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, doctorId, "UpdateDoctor", ipAddress);
                    return NotFound($"No doctor found with id {doctorId}.");
                }

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "UpdateDoctor", doctorId, "Doctor", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Update doctor failed (Validation Error). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, doctorId, "UpdateDoctor", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف طبيب من النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{doctorId:int}", Name = "DeleteDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteDoctor(int doctorId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (doctorId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Delete doctor failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, doctorId, "DeleteDoctor", ipAddress);
                return BadRequest("Invalid doctor ID.");
            }

            bool isDeleted = await _doctorService.DeleteDoctorAsync(doctorId);

            if (!isDeleted)
            {
                _logger.LogWarning(
                    "Audit: Delete doctor failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, doctorId, "DeleteDoctor", ipAddress);
                return NotFound($"Doctor with ID {doctorId} not found.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "DeleteDoctor", doctorId, "Doctor", ipAddress);

            return NoContent();
        }

        /// <summary>
        /// تعديل جزئي على بيانات طبيب
        /// </summary>
        [HttpPatch("{doctorId:int}", Name = "PatchDoctor")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> PatchDoctor(int doctorId, [FromBody] DoctorPatchDTO patchDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (doctorId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Patch doctor failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, doctorId, "PatchDoctor", ipAddress);
                return BadRequest("Invalid doctor ID.");
            }

            if (patchDto == null)
            {
                _logger.LogWarning(
                    "Audit: Patch doctor failed (Data is null). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, doctorId, "PatchDoctor", ipAddress);
                return BadRequest("Patch data is required.");
            }

            bool isUpdated = await _doctorService.PatchDoctorAsync(doctorId, patchDto);

            if (!isUpdated)
            {
                _logger.LogWarning(
                    "Audit: Patch doctor failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, doctorId, "PatchDoctor", ipAddress);
                return NotFound($"No doctor found with id {doctorId}.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "PatchDoctor", doctorId, "Doctor", ipAddress);

            return NoContent();
        }
    }
}