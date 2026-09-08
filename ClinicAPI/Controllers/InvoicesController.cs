using ClinicAPIBusiness.DTO.InvoicesDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Route("api/Invoices")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly clsInvoice _invoiceService;

        public InvoicesController(clsInvoice invoiceService)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
        }

        /// <summary>
        /// جلب قائمة جميع الفواتير المسجلة في النظام
        /// </summary>
        /// <returns>قائمة بجميع الفواتير</returns>
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
        [HttpGet("GetById/{invoiceId:int}", Name = "GetInvoiceById")]
        [ProducesResponseType(typeof(InvoiceViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            return Ok(invoice);
        }

        /// <summary>
        /// إنشاء فاتورة جديدة في النظام
        /// </summary>
        /// <param name="invoiceSaveDto">بيانات الفاتورة الجديدة</param>
        /// <returns>معرف الفاتورة الجديدة</returns>
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