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

        public DoctorsController(clsDoctor doctorService)
        {
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
        }

        /// <summary>
        /// جلب قائمة جميع الأطباء المسجلين في النظام
        /// </summary>
        /// <returns>قائمة بجميع الأطباء</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAll", Name = "GetAllDoctors")]
        [ProducesResponseType(typeof(List<DoctorViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DoctorViewDTO>>> GetAllDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        /// <summary>
        /// جلب بيانات طبيب بناءً على معرفه
        /// </summary>
        /// <param name="doctorId">معرف الطبيب</param>
        /// <returns>بيانات الطبيب</returns>
        [Authorize(Roles = "Admin, Doctor, Receptionist, Nurse, Manager")]
        [HttpGet("GetById/{doctorId:int}", Name = "GetDoctorById")]
        [ProducesResponseType(typeof(DoctorViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DoctorViewDTO>> GetDoctorById(int doctorId)
        {
            // 1. التحقق من صحة المعرّف الممرر في المسار
            if (doctorId <= 0)
            {
                return BadRequest("Invalid doctor ID.");
            }

            // 2. استخراج ID المستخدم الحالي من الـ JWT Claims بأمان
            var userIdClaim = User.FindFirst("UserId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            // 3. تحديد الصلاحية التجاوزية للطاقم الإداري والمساند (Management & Support Staff Bypass):
            // الاداريين، الاستقبال والممرضين يحق لهم الاطلاع على بيانات كافة الأطباء لتنسيق العمل
            bool isAdministrativeOrSupportStaff = User.IsInRole("Admin")
                                                  || User.IsInRole("Manager")
                                                  || User.IsInRole("Receptionist")
                                                  || User.IsInRole("Nurse");

            // 4. جلب بيانات الطبيب من قاعدة البيانات
            var doctor = await _doctorService.GetDoctorByIdAsync(doctorId);
            if (doctor == null)
            {
                return NotFound($"Doctor with ID {doctorId} not found.");
            }

            // 5. تطبيق فحص الملكية (Ownership Check):
            // إذا كان المستدعي طبيباً (وليس ضمن الطاقم الإداري)، يُسمح له فقط بقراءة ملفه الشخصي
            if (!isAdministrativeOrSupportStaff)
            {
                // حماية من الـ Null في حال عدم ربط الطبيب بـ User في قاعدة البيانات
                if (doctor.User == null || doctor.User.UserId != currentUserId)
                {
                    return Forbid(); // HTTP 403 Forbidden
                }
            }

            return Ok(doctor);
        }
        /// <summary>
        /// إضافة طبيب جديد في النظام
        /// </summary>
        /// <param name="doctorSaveDto">بيانات الطبيب والتخصص</param>
        /// <returns>معرف الطبيب الجديد</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreateDoctor")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreateDoctor([FromBody] DoctorSaveDTO doctorSaveDto)
        {
            if (doctorSaveDto == null)
            {
                return BadRequest("Doctor data is required.");
            }

            try
            {
                int newDoctorId = await _doctorService.AddNewDoctorAsync(doctorSaveDto);

                return CreatedAtRoute("GetDoctorById", new { doctorId = newDoctorId }, newDoctorId);
            }
            catch (ArgumentException ex)
            {
                // بزنس فاليديشن فشل (مثل وجود الطبيب مسبقاً أو تخصص غير صالح)
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات طبيب في النظام
        /// </summary>
        /// <param name="doctorId">معرف الطبيب المراد تحديثه (من الـ Route)</param>
        [Authorize(Roles = "Admin")]
        /// <param name="doctorSaveDto">بيانات الطبيب المحدثة</param>
        [HttpPut("{doctorId:int}", Name = "UpdateDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDoctor(int doctorId, [FromBody] DoctorSaveDTO doctorSaveDto)
        {
            if (doctorSaveDto == null)
            {
                return BadRequest("Doctor data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (doctorId != doctorSaveDto.DoctorDetails.DoctorId)
            {
                return BadRequest("Mismatched doctor id between route and body.");
            }

            try
            {
                bool isUpdated = await _doctorService.UpdateDoctorAsync(doctorSaveDto);

                if (!isUpdated)
                {
                    return NotFound($"No doctor found with id {doctorId}.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف طبيب من النظام
        /// </summary>
        /// <param name="doctorId">معرف الطبيب المراد حذفه</param>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{doctorId:int}", Name = "DeleteDoctor")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDoctor(int doctorId)
        {
            if (doctorId <= 0)
            {
                return BadRequest("Invalid doctor ID.");
            }

            bool isDeleted = await _doctorService.DeleteDoctorAsync(doctorId);

            if (!isDeleted)
            {
                return NotFound($"Doctor with ID {doctorId} not found.");
            }

            return NoContent();
        }


        /// <summary>
        /// تعديل جزئي على بيانات طبيب - أرسل فقط الحقول التي تريد تغييرها،
        /// واترك الباقي null (أو احذفها من الـ JSON) لتبقى كما هي
        /// </summary>
        /// <param name="doctorId">معرف الطبيب</param>
        /// <param name="patchDto">الحقول المراد تعديلها فقط</param>
        [HttpPatch("{doctorId:int}", Name = "PatchDoctor")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchDoctor(int doctorId, [FromBody] DoctorPatchDTO patchDto)
        {
            if (doctorId <= 0)
            {
                return BadRequest("Invalid doctor ID.");
            }

            if (patchDto == null)
            {
                return BadRequest("Patch data is required.");
            }

            bool isUpdated = await _doctorService.PatchDoctorAsync(doctorId, patchDto);

            if (!isUpdated)
            {
                return NotFound($"No doctor found with id {doctorId}.");
            }

            return NoContent();
        }
    }
}