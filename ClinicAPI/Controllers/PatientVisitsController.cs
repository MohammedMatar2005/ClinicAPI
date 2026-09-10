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

        public PatientVisitsController(clsPatientVisit patientVisitService)
        {
            _patientVisitService = patientVisitService ?? throw new ArgumentNullException(nameof(patientVisitService));
        }

        /// <summary>
        /// جلب قائمة جميع الزيارات المسجلة في النظام
        /// </summary>
        /// <returns>قائمة بجميع زيارات المرضى</returns>
        [HttpGet("GetAll", Name = "GetAllPatientVisits")]
        [ProducesResponseType(typeof(List<PatientVisitViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientVisitViewDTO>>> GetAllPatientVisits()
        {
            var visits = await _patientVisitService.GetAllPatientVisitsAsync();
            return Ok(visits);
        }

        /// <summary>
        /// جلب بيانات زيارة بناءً على معرفها
        /// </summary>
        /// <param name="visitId">معرف الزيارة</param>
        /// <returns>بيانات الزيارة (تشمل التشخيص والملاحظات)</returns>
        [Authorize(Roles = "Admin, Receptionist, Doctor")]
        [HttpGet("GetById/{visitId:int}", Name = "GetPatientVisitById")]
        [ProducesResponseType(typeof(PatientVisitViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PatientVisitViewDTO>> GetPatientVisitById(int visitId)
        {
            if (visitId <= 0)
            {
                return BadRequest("Invalid visit ID.");
            }

            var visit = await _patientVisitService.GetPatientVisitByIdAsync(visitId);
            if (visit == null)
            {
                return NotFound($"Visit with ID {visitId} not found.");
            }

            // 1. استخراج الـ UserId من الـ Claim بأمان
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            // 2. الآدمن والاستقبال يمكنهم مشاهدة جميع الزيارات
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");

            // 3. الطبيب يصل للزيارات التي أريت تحت إشرافه فقط (مقارنة مباشرة في الذاكرة عبر DoctorUserId في الـ DTO)
            if (!isStaff && visit.DoctorUserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(visit);
        }

        /// <summary>
        /// تسجيل زيارة مريض جديدة في النظام
        /// </summary>
        /// <param name="visitSaveDto">بيانات الزيارة الجديدة</param>
        /// <returns>معرف الزيارة الجديدة</returns>
        [HttpPost("Create", Name = "CreatePatientVisit")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreatePatientVisit([FromBody] PatientVisitSaveDTO visitSaveDto)
        {
            if (visitSaveDto == null)
            {
                return BadRequest("Visit data is required.");
            }

            try
            {
                int newVisitId = await _patientVisitService.AddNewPatientVisitAsync(visitSaveDto);

                return CreatedAtRoute("GetPatientVisitById", new { visitId = newVisitId }, newVisitId);
            }
            catch (ArgumentException ex)
            {
                // بزنس فاليديشن فشل (مثل: الموعد المرتبط غير موجود، أو المريض غير صالح)
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات زيارة مريض (مثل إضافة تشخيص أو تحديث الملاحظات الطبية)
        /// </summary>
        /// <param name="visitId">معرف الزيارة المراد تحديثها (من الـ Route)</param>
        /// <param name="visitSaveDto">بيانات الزيارة المحدثة</param>
        [HttpPut("{visitId:int}", Name = "UpdatePatientVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePatientVisit(int visitId, [FromBody] PatientVisitSaveDTO visitSaveDto)
        {
            if (visitSaveDto == null)
            {
                return BadRequest("Visit data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (visitId != visitSaveDto.VisitId)
            {
                return BadRequest("Mismatched visit id between route and body.");
            }

            try
            {
                bool isUpdated = await _patientVisitService.UpdatePatientVisitAsync(visitSaveDto);

                if (!isUpdated)
                {
                    return NotFound($"No visit found with id {visitId}.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف زيارة من النظام
        /// </summary>
        /// <param name="visitId">معرف الزيارة المراد حذفها</param>
        [HttpDelete("{visitId:int}", Name = "DeletePatientVisit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePatientVisit(int visitId)
        {
            if (visitId <= 0)
            {
                return BadRequest("Invalid visit ID.");
            }

            bool isDeleted = await _patientVisitService.DeletePatientVisitAsync(visitId);

            if (!isDeleted)
            {
                return NotFound($"Visit with ID {visitId} not found.");
            }

            return NoContent();
        }
    }
}