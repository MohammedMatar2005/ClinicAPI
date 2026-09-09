using ClinicAPIBusiness.DTO.PaymentsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/Payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly clsPayment _paymentService;

        public PaymentsController(clsPayment paymentService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        }

        /// <summary>
        /// جلب قائمة جميع عمليات الدفع المسجلة في النظام
        /// </summary>
        /// <returns>قائمة بجميع المدفوعات</returns>
        [HttpGet("GetAll", Name = "GetAllPayments")]
        [ProducesResponseType(typeof(List<PaymentViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentViewDTO>>> GetAllPayments()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();
            return Ok(payments);
        }

        /// <summary>
        /// جلب بيانات عملية دفع بناءً على معرفها
        /// </summary>
        /// <param name="paymentId">معرف عملية الدفع</param>
        /// <returns>بيانات الدفع</returns>
        [HttpGet("GetById/{paymentId:int}", Name = "GetPaymentById")]
        [ProducesResponseType(typeof(PaymentViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentViewDTO>> GetPaymentById(int paymentId)
        {
            if (paymentId <= 0)
            {
                return BadRequest("Invalid payment ID.");
            }

            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        /// <summary>
        /// تسجيل عملية دفع جديدة في النظام
        /// </summary>
        /// <param name="paymentSaveDto">بيانات عملية الدفع الجديدة</param>
        /// <returns>معرف عملية الدفع الجديدة</returns>
        [HttpPost("Create", Name = "CreatePayment")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreatePayment([FromBody] PaymentSaveDTO paymentSaveDto)
        {
            if (paymentSaveDto == null)
            {
                return BadRequest("Payment data is required.");
            }

            try
            {
                int newPaymentId = await _paymentService.AddNewPaymentAsync(paymentSaveDto);

                return CreatedAtRoute("GetPaymentById", new { paymentId = newPaymentId }, newPaymentId);
            }
            catch (ArgumentException ex)
            {
                // بزنس فاليديشن فشل (مثل الفاتورة غير موجودة أو المبلغ المدفوع يتجاوز المتبقي)
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات عملية دفع في النظام
        /// </summary>
        /// <param name="paymentId">معرف عملية الدفع المراد تحديثها (من الـ Route)</param>
        /// <param name="paymentSaveDto">بيانات عملية الدفع المحدثة</param>
        [HttpPut("{paymentId:int}", Name = "UpdatePayment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePayment(int paymentId, [FromBody] PaymentSaveDTO paymentSaveDto)
        {
            if (paymentSaveDto == null)
            {
                return BadRequest("Payment data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (paymentId != paymentSaveDto.PaymentId)
            {
                return BadRequest("Mismatched payment id between route and body.");
            }

            try
            {
                bool isUpdated = await _paymentService.UpdatePaymentAsync(paymentSaveDto);

                if (!isUpdated)
                {
                    return NotFound($"No payment found with id {paymentId}.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف عملية دفع من النظام
        /// </summary>
        /// <param name="paymentId">معرف عملية الدفع المراد حذفها</param>
        [HttpDelete("{paymentId:int}", Name = "DeletePayment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePayment(int paymentId)
        {
            if (paymentId <= 0)
            {
                return BadRequest("Invalid payment ID.");
            }

            bool isDeleted = await _paymentService.DeletePaymentAsync(paymentId);

            if (!isDeleted)
            {
                return NotFound($"Payment with ID {paymentId} not found.");
            }

            return NoContent();
        }
    }
}