using ClinicAPIBusiness.DTO.AppointmentsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAPI.Controllers
{
    [Route("api/Appointments")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly clsAppointment _appointmentService;
        private readonly clsDoctor _doctorService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(clsAppointment appointmentService, clsDoctor doctorService, ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب قائمة جميع المواعيد المسجلة في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAll", Name = "GetAllAppointments")]
        [ProducesResponseType(typeof(List<AppointmentViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AppointmentViewDTO>>> GetAllAppointments()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Audit: Admin action executed. AdminId={AdminId}, Action={Action}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "GetAllAppointments", "AppointmentList", ipAddress);

            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return Ok(appointments);
        }

        [Authorize(Roles = "Admin, Doctor, Receptionist, Nurse, Manager")]
        [HttpGet("GetById/{appointmentId:int}", Name = "GetAppointmentById")]
        [ProducesResponseType(typeof(AppointmentViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentViewDTO>> GetAppointmentById(
           int appointmentId,
           [FromServices] IAuthorizationService authorizationService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // 1. التحقق من صحة المعرّف الممرر في المسار
            if (appointmentId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Get appointment failed (Invalid ID). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, appointmentId, "GetAppointmentById", ipAddress);
                return BadRequest("Invalid appointment ID.");
            }

            // 2. البحث عن الموعد المطلوب في قاعدة البيانات
            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                _logger.LogWarning(
                    "Audit: Get appointment failed (Not Found). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, appointmentId, "GetAppointmentById", ipAddress);
                return NotFound("Appointment not found.");
            }

            // 3. التحقق من الصلاحيات والملكية عبر الـ Policy
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                appointment,
                "CanAccessAppointment");

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "Audit: Get appointment failed (Forbidden/Unauthorized access). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, appointmentId, "GetAppointmentById", ipAddress);
                return Forbid(); // 403 Forbidden
            }

            // 4. عند اجتياز كافة الفحوصات يتم إرجاع بيانات الموعد
            return Ok(appointment);
        }

        /// <summary>
        /// حجز موعد جديد في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreateAppointment")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<int>> CreateAppointment([FromBody] AppointmentSaveDto appointmentSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (appointmentSaveDto == null)
            {
                _logger.LogWarning(
                    "Audit: Create appointment failed (Data is null). AdminId={AdminId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, "CreateAppointment", ipAddress);
                return BadRequest("Appointment data is required.");
            }

            try
            {
                int newAppointmentId = await _appointmentService.AddNewAppointmentAsync(appointmentSaveDto);

                // Audit Log احترافي لعملية الإضافة الناجحة
                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "CreateAppointment", newAppointmentId, "Appointment", ipAddress);

                return CreatedAtRoute("GetAppointmentById", new { appointmentId = newAppointmentId }, newAppointmentId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Create appointment failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, "CreateAppointment", ex.Message, ipAddress);

                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات موعد في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{appointmentId:int}", Name = "UpdateAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAppointment(int appointmentId, [FromBody] AppointmentSaveDto appointmentSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (appointmentSaveDto == null)
            {
                return BadRequest("Appointment data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (appointmentId != appointmentSaveDto.AppointmentId)
            {
                _logger.LogWarning(
                    "Audit: Update appointment failed (ID Mismatch). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, appointmentId, "UpdateAppointment", ipAddress);
                return BadRequest("Mismatched appointment id between route and body.");
            }

            try
            {
                bool isUpdated = await _appointmentService.UpdateAppointmentAsync(appointmentSaveDto);

                if (!isUpdated)
                {
                    _logger.LogWarning(
                        "Audit: Update appointment failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, appointmentId, "UpdateAppointment", ipAddress);
                    return NotFound($"No appointment found with id {appointmentId}.");
                }

                // Audit Log للنجاح
                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "UpdateAppointment", appointmentId, "Appointment", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Update appointment failed (Validation Error). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, appointmentId, "UpdateAppointment", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// إلغاء أو حذف موعد من النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{appointmentId:int}", Name = "DeleteAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAppointment(int appointmentId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (appointmentId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Delete appointment failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, appointmentId, "DeleteAppointment", ipAddress);
                return BadRequest("Invalid appointment ID.");
            }

            bool isDeleted = await _appointmentService.DeleteAppointmentAsync(appointmentId);

            if (!isDeleted)
            {
                _logger.LogWarning(
                    "Audit: Delete appointment failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, appointmentId, "DeleteAppointment", ipAddress);
                return NotFound($"Appointment with ID {appointmentId} not found.");
            }

            // Audit Log لعملية الحذف الناجحة
            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "DeleteAppointment", appointmentId, "Appointment", ipAddress);

            return NoContent();
        }
    }
}