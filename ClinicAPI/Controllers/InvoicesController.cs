using ClinicAPIBusiness.DTO.InvoicesDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAPI.Controllers
{
    [Route("api/Invoices")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly clsInvoice _invoiceService;
        private readonly clsDoctor _doctorService;
        private readonly clsPatientVisit _patientVisitService;
        private readonly clsAppointment _appointmentService;

        public InvoicesController(clsInvoice invoiceService, clsDoctor doctorService
            , clsPatientVisit patientVisitService, clsAppointment appointmentService)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _patientVisitService = patientVisitService ?? throw new ArgumentNullException(nameof(patientVisitService));
            _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
        }

        /// <summary>
        /// جلب قائمة جميع الفواتير المسجلة في النظام
        /// </summary>
        /// <returns>قائمة بجميع الفواتير</returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAll", Name = "GetAllInvoices")]
        [ProducesResponseType(typeof(List<InvoiceViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<InvoiceViewDTO>>> GetAllInvoices()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        /// <summary>
        /// جلب بيانات فاتورة بناءً على معرفها
        /// </summary>
        /// <param name="invoiceId">معرف الفاتورة</param>
        /// <returns>بيانات الفاتورة</returns>
        [Authorize(Roles = "Admin, Receptionist, Doctor")]
        [HttpGet("GetById/{invoiceId:int}", Name = "GetInvoiceById")]
        [ProducesResponseType(typeof(InvoiceViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<InvoiceViewDTO>> GetInvoiceById(int invoiceId)
        {
            if (invoiceId <= 0)
            {
                return BadRequest("Invalid invoice ID.");
            }

            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);

            if (invoice == null)
            {
                return NotFound();
            }

            // 1. استخراج الـ UserId من الـ Claim بأمان
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            // 2. الآدمن والاستقبال يمكنهم مشاهدة أي فاتورة
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");

            // 3. الطبيب يصل للفواتير المرتبطة به فقط (مقارنة مباشرة بالذاكرة بدون DB call)
            if (!isStaff && invoice.DoctorUserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(invoice);
        }
        /// <summary>
        /// إنشاء فاتورة جديدة في النظام
        /// </summary>
        /// <param name="invoiceSaveDto">بيانات الفاتورة الجديدة</param>
        /// <returns>معرف الفاتورة الجديدة</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreateInvoice")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreateInvoice([FromBody] InvoiceSaveDTO invoiceSaveDto)
        {
            if (invoiceSaveDto == null)
            {
                return BadRequest("Invoice data is required.");
            }

            try
            {
                int newInvoiceId = await _invoiceService.AddNewInvoiceAsync(invoiceSaveDto);

                return CreatedAtRoute("GetInvoiceById", new { invoiceId = newInvoiceId }, newInvoiceId);
            }
            catch (ArgumentException ex)
            {
                // بزنس فاليديشن فشل (مثل الموعد غير موجود، أو قيمة المبلغ غير صحيحة)
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات فاتورة في النظام (حالة الدفع، المبلغ المدفوع، طريقة الدفع)
        /// </summary>
        /// <param name="invoiceId">معرف الفاتورة المراد تحديثها (من الـ Route)</param>
        /// <param name="invoiceSaveDto">بيانات الفاتورة المحدثة</param>
        [Authorize(Roles = "Admin")]
        [HttpPut("{invoiceId:int}", Name = "UpdateInvoice")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateInvoice(int invoiceId, [FromBody] InvoiceSaveDTO invoiceSaveDto)
        {
            if (invoiceSaveDto == null)
            {
                return BadRequest("Invoice data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (invoiceId != invoiceSaveDto.InvoiceId)
            {
                return BadRequest("Mismatched invoice id between route and body.");
            }

            try
            {
                bool isUpdated = await _invoiceService.UpdateInvoiceAsync(invoiceSaveDto);

                if (!isUpdated)
                {
                    return NotFound($"No invoice found with id {invoiceId}.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف فاتورة من النظام
        /// </summary>
        /// <param name="invoiceId">معرف الفاتورة المراد حذفها</param>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{invoiceId:int}", Name = "DeleteInvoice")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteInvoice(int invoiceId)
        {
            if (invoiceId <= 0)
            {
                return BadRequest("Invalid invoice ID.");
            }

            bool isDeleted = await _invoiceService.DeleteInvoiceAsync(invoiceId);

            if (!isDeleted)
            {
                return NotFound($"Invoice with ID {invoiceId} not found.");
            }

            return NoContent();
        }
    }
}