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


        public AppointmentsController(clsAppointment appointmentService, clsDoctor doctorService)
        {
            _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
        }

        /// <summary>
        /// جلب قائمة جميع المواعيد المسجلة في النظام
        /// </summary>
        /// <returns>قائمة بجميع المواعيد</returns>
         [Authorize(Roles = "Admin")]
        [HttpGet("GetAll", Name = "GetAllAppointments")]
        [ProducesResponseType(typeof(List<AppointmentViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<AppointmentViewDTO>>> GetAllAppointments()
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return Ok(appointments);
        }


        [Authorize(Roles = "Admin, Receptionist, Doctor")]
        [HttpGet("GetById/{appointmentId:int}", Name = "GetAppointmentById")]
        [ProducesResponseType(typeof(AppointmentViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AppointmentViewDTO>> GetAppointmentById(int appointmentId)
        {
            if (appointmentId <= 0)
            {
                return BadRequest("Invalid appointment ID.");
            }

            var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                return NotFound();
            }

            // 1. استخراج آمن للـ UserId مع حماية من الـ Null
            var userIdClaim = User.FindFirst("UserId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            // 2. السماح للموظفين والـ Admin بالوصول المباشر
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");

            // 3. فحص الملكية فقط إذا كان المستدعي طبيباً
            if (!isStaff)
            {
                var doctor = await _doctorService.GetDoctorByIdAsync(appointment.DoctorId);

                if (doctor == null || doctor.User == null || currentUserId != doctor.User.UserId)
                {
                    return Forbid();
                }
            }

            return Ok(appointment);
        }

        /// <summary>
        /// حجز موعد جديد في النظام
        /// </summary>
        /// <param name="appointmentSaveDto">بيانات الموعد الجديد</param>
        /// <returns>معرف الموعد الجديد</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreateAppointment")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreateAppointment([FromBody] AppointmentSaveDto appointmentSaveDto)
        {
            if (appointmentSaveDto == null)
            {
                return BadRequest("Appointment data is required.");
            }

            try
            {
                int newAppointmentId = await _appointmentService.AddNewAppointmentAsync(appointmentSaveDto);

                return CreatedAtRoute("GetAppointmentById", new { appointmentId = newAppointmentId }, newAppointmentId);
            }
            catch (ArgumentException ex)
            {
                // بزنس فاليديشن فشل (مثل تعارض في المواعيد أو عدم وجود الطبيب/المريض)
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات موعد في النظام (تغيير وقت الموعد، الحالة، أو الطبيب)
        /// </summary>
        /// <param name="appointmentId">معرف الموعد المراد تحديثه (من الـ Route)</param>
        /// <param name="appointmentSaveDto">بيانات الموعد المحدثة</param>
        [Authorize(Roles = "Admin")]
        [HttpPut("{appointmentId:int}", Name = "UpdateAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAppointment(int appointmentId, [FromBody] AppointmentSaveDto appointmentSaveDto)
        {
            if (appointmentSaveDto == null)
            {
                return BadRequest("Appointment data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (appointmentId != appointmentSaveDto.AppointmentId)
            {
                return BadRequest("Mismatched appointment id between route and body.");
            }

            try
            {
                bool isUpdated = await _appointmentService.UpdateAppointmentAsync(appointmentSaveDto);

                if (!isUpdated)
                {
                    return NotFound($"No appointment found with id {appointmentId}.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// إلغاء أو حذف موعد من النظام
        /// </summary>
        /// <param name="appointmentId">معرف الموعد المراد حذفه</param>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{appointmentId:int}", Name = "DeleteAppointment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAppointment(int appointmentId)
        {
            if (appointmentId <= 0)
            {
                return BadRequest("Invalid appointment ID.");
            }

            bool isDeleted = await _appointmentService.DeleteAppointmentAsync(appointmentId);

            if (!isDeleted)
            {
                return NotFound($"Appointment with ID {appointmentId} not found.");
            }

            return NoContent();
        }
    }
}