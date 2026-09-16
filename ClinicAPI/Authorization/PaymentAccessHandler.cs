using ClinicAPIBusiness.DTO.PaymentsDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class PaymentAccessRequirement : IAuthorizationRequirement { }

public class PaymentAccessHandler : AuthorizationHandler<PaymentAccessRequirement, PaymentViewDTO>
{
    private readonly clsPayment _paymentService;
    private readonly clsInvoice _invoiceService;
    private readonly clsAppointment _appointmentService;
    private readonly clsDoctor _doctorService;

    public PaymentAccessHandler(
        clsPayment payment,
        clsInvoice invoice,
        clsAppointment appointment,
        clsDoctor doctor)
    {
        _paymentService = payment ?? throw new ArgumentNullException(nameof(payment));
        _invoiceService = invoice ?? throw new ArgumentNullException(nameof(invoice));
        _appointmentService = appointment ?? throw new ArgumentNullException(nameof(appointment));
        _doctorService = doctor ?? throw new ArgumentNullException(nameof(doctor));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PaymentAccessRequirement requirement,
        PaymentViewDTO payment)
    {
        // 1. الأدوار الإدارية العليا تمتلك صلاحية كاملة
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Manager"))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. استخراج الـ User ID الخاص بالمستخدم الحالي من التوكن
        var userIdClaim = context.User.FindFirstValue("UserId")
                          ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int currentUserId))
        {
            return;
        }

        // 3. تتبع العلاقات المعقدة للوصول للطبيب المسؤول عن الفاتورة/الدفعة بأمان
        var targetedPayment = await _paymentService.GetPaymentByIdAsync(payment.PaymentId);
        if (targetedPayment == null) return;

        var invoice = await _invoiceService.GetInvoiceByIdAsync(targetedPayment.InvoiceId);
        if (invoice == null) return;

        var appointment = await _appointmentService.GetAppointmentByIdAsync(invoice.VisitId);
        if (appointment == null) return;

        var doctor = await _doctorService.GetDoctorByIdAsync(appointment.DoctorId);
        if (doctor == null || doctor.User == null) return;

        // 4. فحص الملكية: السماح للطبيب فقط إذا كانت الدفعة تخص موعداً تابعاً له (== وليست !=)
        if (doctor.User.UserId == currentUserId)
        {
            context.Succeed(requirement);
        }
    }
}