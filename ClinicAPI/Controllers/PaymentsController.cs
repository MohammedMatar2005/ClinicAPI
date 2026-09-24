using ClinicAPIBusiness.DTO.PaymentsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/Payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly clsPayment _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(clsPayment paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب قائمة جميع عمليات الدفع المسجلة في النظام
        /// </summary>
        [HttpGet("GetAll", Name = "GetAllPayments")]
        [ProducesResponseType(typeof(List<PaymentViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentViewDTO>>> GetAllPayments()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Audit: Admin action executed. AdminId={AdminId}, Action={Action}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "GetAllPayments", "PaymentList", ipAddress);

            var payments = await _paymentService.GetAllPaymentsAsync();
            return Ok(payments);
        }

        /// <summary>
        /// جلب بيانات عملية دفع بناءً على معرفها
        /// </summary>
        [Authorize(Roles = "Admin, Manager, Doctor")] // السماح للأدوار المخولة بالدخول مبدئياً
        [HttpGet("GetById/{paymentId:int}", Name = "GetPaymentById")]
        [ProducesResponseType(typeof(PaymentViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PaymentViewDTO>> GetPaymentById(
            int paymentId,
            [FromServices] IAuthorizationService authorizationService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (paymentId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Get payment failed (Invalid ID). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, paymentId, "GetPaymentById", ipAddress);
                return BadRequest("Invalid payment ID.");
            }

            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning(
                    "Audit: Get payment failed (Not Found). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, paymentId, "GetPaymentById", ipAddress);
                return NotFound();
            }

            // فحص الصلاحية والملكية عبر الـ Policy التي قمنا بإنشائها
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                payment,
                "CanAccessPayment");

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "Audit: Get payment failed (Forbidden/Unauthorized access). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, paymentId, "GetPaymentById", ipAddress);
                return Forbid(); // ترجع 403 Forbidden في حال لم تحقّق الشرط
            }

            return Ok(payment);
        }

        /// <summary>
        /// تسجيل عملية دفع جديدة في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreatePayment")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<int>> CreatePayment([FromBody] PaymentSaveDTO paymentSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (paymentSaveDto == null)
            {
                _logger.LogWarning(
                    "Audit: Create payment failed (Data is null). AdminId={AdminId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, "CreatePayment", ipAddress);
                return BadRequest("Payment data is required.");
            }

            try
            {
                int newPaymentId = await _paymentService.AddNewPaymentAsync(paymentSaveDto);

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "CreatePayment", newPaymentId, "Payment", ipAddress);

                return CreatedAtRoute("GetPaymentById", new { paymentId = newPaymentId }, newPaymentId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Create payment failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, "CreatePayment", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات عملية دفع في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{paymentId:int}", Name = "UpdatePayment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePayment(int paymentId, [FromBody] PaymentSaveDTO paymentSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (paymentSaveDto == null)
            {
                return BadRequest("Payment data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (paymentId != paymentSaveDto.PaymentId)
            {
                _logger.LogWarning(
                    "Audit: Update payment failed (ID Mismatch). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, paymentId, "UpdatePayment", ipAddress);
                return BadRequest("Mismatched payment id between route and body.");
            }

            try
            {
                bool isUpdated = await _paymentService.UpdatePaymentAsync(paymentSaveDto);

                if (!isUpdated)
                {
                    _logger.LogWarning(
                        "Audit: Update payment failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, paymentId, "UpdatePayment", ipAddress);
                    return NotFound($"No payment found with id {paymentId}.");
                }

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "UpdatePayment", paymentId, "Payment", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Update payment failed (Validation Error). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, paymentId, "UpdatePayment", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف عملية دفع من النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{paymentId:int}", Name = "DeletePayment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeletePayment(int paymentId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (paymentId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Delete payment failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, paymentId, "DeletePayment", ipAddress);
                return BadRequest("Invalid payment ID.");
            }

            bool isDeleted = await _paymentService.DeletePaymentAsync(paymentId);

            if (!isDeleted)
            {
                _logger.LogWarning(
                    "Audit: Delete payment failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, paymentId, "DeletePayment", ipAddress);
                return NotFound($"Payment with ID {paymentId} not found.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "DeletePayment", paymentId, "Payment", ipAddress);

            return NoContent();
        }
    }
}