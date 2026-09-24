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
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(clsInvoice invoiceService, clsDoctor doctorService
            , clsPatientVisit patientVisitService, clsAppointment appointmentService, ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _patientVisitService = patientVisitService ?? throw new ArgumentNullException(nameof(patientVisitService));
            _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب قائمة جميع الفواتير المسجلة في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAll", Name = "GetAllInvoices")]
        [ProducesResponseType(typeof(List<InvoiceViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<InvoiceViewDTO>>> GetAllInvoices()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Audit: Admin action executed. AdminId={AdminId}, Action={Action}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "GetAllInvoices", "InvoiceList", ipAddress);

            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        [Authorize(Roles = "Admin, Receptionist, Doctor")]
        [HttpGet("GetById/{invoiceId:int}", Name = "GetInvoiceById")]
        [ProducesResponseType(typeof(InvoiceViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<InvoiceViewDTO>> GetInvoiceById(
          int invoiceId,
          [FromServices] IAuthorizationService authorizationService)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (invoiceId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Get invoice failed (Invalid ID). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, invoiceId, "GetInvoiceById", ipAddress);
                return BadRequest("Invalid invoice ID.");
            }

            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);

            if (invoice == null)
            {
                _logger.LogWarning(
                    "Audit: Get invoice failed (Not Found). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, invoiceId, "GetInvoiceById", ipAddress);
                return NotFound();
            }

            // تمرير الفاتورة (Resource) للـ Policy للتأكد من الصلاحية
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                invoice,
                "CanAccessInvoice");

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "Audit: Get invoice failed (Forbidden/Unauthorized access). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    userId, invoiceId, "GetInvoiceById", ipAddress);
                return Forbid(); // 403 Forbidden
            }

            return Ok(invoice);
        }

        /// <summary>
        /// إنشاء فاتورة جديدة في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreateInvoice")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<int>> CreateInvoice([FromBody] InvoiceSaveDTO invoiceSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (invoiceSaveDto == null)
            {
                _logger.LogWarning(
                    "Audit: Create invoice failed (Data is null). AdminId={AdminId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, "CreateInvoice", ipAddress);
                return BadRequest("Invoice data is required.");
            }

            try
            {
                int newInvoiceId = await _invoiceService.AddNewInvoiceAsync(invoiceSaveDto);

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "CreateInvoice", newInvoiceId, "Invoice", ipAddress);

                return CreatedAtRoute("GetInvoiceById", new { invoiceId = newInvoiceId }, newInvoiceId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Create invoice failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, "CreateInvoice", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات فاتورة في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{invoiceId:int}", Name = "UpdateInvoice")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateInvoice(int invoiceId, [FromBody] InvoiceSaveDTO invoiceSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (invoiceSaveDto == null)
            {
                return BadRequest("Invoice data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (invoiceId != invoiceSaveDto.InvoiceId)
            {
                _logger.LogWarning(
                    "Audit: Update invoice failed (ID Mismatch). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, invoiceId, "UpdateInvoice", ipAddress);
                return BadRequest("Mismatched invoice id between route and body.");
            }

            try
            {
                bool isUpdated = await _invoiceService.UpdateInvoiceAsync(invoiceSaveDto);

                if (!isUpdated)
                {
                    _logger.LogWarning(
                        "Audit: Update invoice failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, invoiceId, "UpdateInvoice", ipAddress);
                    return NotFound($"No invoice found with id {invoiceId}.");
                }

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "UpdateInvoice", invoiceId, "Invoice", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Update invoice failed (Validation Error). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, invoiceId, "UpdateInvoice", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف فاتورة من النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{invoiceId:int}", Name = "DeleteInvoice")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteInvoice(int invoiceId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (invoiceId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Delete invoice failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, invoiceId, "DeleteInvoice", ipAddress);
                return BadRequest("Invalid invoice ID.");
            }

            bool isDeleted = await _invoiceService.DeleteInvoiceAsync(invoiceId);

            if (!isDeleted)
            {
                _logger.LogWarning(
                    "Audit: Delete invoice failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, invoiceId, "DeleteInvoice", ipAddress);
                return NotFound($"Invoice with ID {invoiceId} not found.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "DeleteInvoice", invoiceId, "Invoice", ipAddress);

            return NoContent();
        }
    }
}