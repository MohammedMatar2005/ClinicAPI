using ClinicAPIBusiness.DTO.PatientsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/Patients")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly clsPatient _patientService;

        public PatientController(clsPatient patientService)
        {
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        }

        /// <summary>
        /// جلب قائمة جميع المرضى المسجلين في النظام
        /// </summary>
        /// <returns>قائمة بجميع المرضى</returns>
        
        [HttpGet("GetAll", Name = "GetAllPatients")]
        [ProducesResponseType(typeof(List<PatientViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientViewDTO>>> GetAllPatients()
        {
            var patients = await _patientService.GetAllPatientsAsync();
            return Ok(patients);
        }

        /// <summary>
        /// جلب بيانات مريض بناءً على معرفه
        /// </summary>
        /// <param name="patientId">معرف المريض</param>
        /// <returns>بيانات المريض</returns>
        [Authorize(Roles = "Admin, Receptionist, Doctor")]
        [HttpGet("GetById/{patientId:int}", Name = "GetPatientById")]
        [ProducesResponseType(typeof(PatientViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<PatientViewDTO>> GetPatientById(int patientId)
        {
            if (patientId <= 0)
            {
                return BadRequest("Invalid patient ID.");
            }

            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                return NotFound($"Patient with ID {patientId} not found.");
            }

            return Ok(patient);
        }

        /// <summary>
        /// إضافة مريض جديد في النظام
        /// </summary>
        /// <param name="patientSaveDto">بيانات المريض الشخصية والطبية</param>
        /// <returns>معرف المريض الجديد</returns>
        [HttpPost("Create", Name = "CreatePatient")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreatePatient([FromBody] PatientSaveDTO patientSaveDto)
        {
            if (patientSaveDto == null)
            {
                return BadRequest("Patient data is required.");
            }

            try
            {
                int newPatientId = await _patientService.AddNewPatientAsync(patientSaveDto);

                return CreatedAtRoute("GetPatientById", new { patientId = newPatientId }, newPatientId);
            }
            catch (ArgumentException ex)
            {
                // بزنس فاليديشن فشل (مثل رقم هوية مكرر أو تاريخ غير صالح)
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات مريض في النظام
        /// </summary>
        /// <param name="patientId">معرف المريض المراد تحديثه (من الـ Route)</param>
        /// <param name="patientSaveDto">بيانات المريض المحدثة</param>
        [HttpPut("{patientId:int}", Name = "UpdatePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePatient(int patientId, [FromBody] PatientSaveDTO patientSaveDto)
        {
            if (patientSaveDto == null)
            {
                return BadRequest("Patient data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (patientId != patientSaveDto.PatientDetails.PatientId)
            {
                return BadRequest("Mismatched patient id between route and body.");
            }

            try
            {
                bool isUpdated = await _patientService.UpdatePatientAsync(patientSaveDto);

                if (!isUpdated)
                {
                    return NotFound($"No patient found with id {patientId}.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف مريض من النظام
        /// </summary>
        /// <param name="patientId">معرف المريض المراد حذفه</param>
        [HttpDelete("{patientId:int}", Name = "DeletePatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePatient(int patientId)
        {
            if (patientId <= 0)
            {
                return BadRequest("Invalid patient ID.");
            }

            bool isDeleted = await _patientService.DeletePatientAsync(patientId);

            if (!isDeleted)
            {
                return NotFound($"Patient with ID {patientId} not found.");
            }

            return NoContent();
        }

        /// <summary>
        /// تعديل جزئي على بيانات مريض - أرسل فقط الحقول التي تريد تغييرها،
        /// واترك الباقي null (أو احذفها من الـ JSON) لتبقى كما هي
        /// </summary>
        /// <param name="patientId">معرف المريض</param>
        /// <param name="patchDto">الحقول المراد تعديلها فقط</param>
        [HttpPatch("{patientId:int}", Name = "PatchPatient")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PatchPatient(int patientId, [FromBody] PatientPatchDTO patchDto)
        {
            if (patientId <= 0)
            {
                return BadRequest("Invalid patient ID.");
            }

            if (patchDto == null)
            {
                return BadRequest("Patch data is required.");
            }

            bool isUpdated = await _patientService.PatchPatientAsync(patientId, patchDto);

            if (!isUpdated)
            {
                return NotFound($"No patient found with id {patientId}.");
            }

            return NoContent();
        }
    }
}