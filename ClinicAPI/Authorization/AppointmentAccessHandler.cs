using ClinicAPIBusiness.DTO.AppointmentsDTOs;
using ClinicAPIBusiness.DTO.DoctorsDTOs;
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class AppointmentAccessRequirement : IAuthorizationRequirement { }

public class AppointmentAccessHandler : AuthorizationHandler<AppointmentAccessRequirement, AppointmentViewDTO>
{
    private readonly IServiceProvider _serviceProvider; // أو حقن الـ Service مباشرة إذا كانت مسجلة في الـ DI

    // إذا كانت الـ clsDoctor مسجلة في الـ DI (مثل باقي الخدمات عندك في Program.cs):
    private readonly clsDoctor _doctorService;

    public AppointmentAccessHandler(clsDoctor doctorService)
    {
        _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AppointmentAccessRequirement requirement,
        AppointmentViewDTO appointment)
    {
        // 1. الطاقم الإداري والمساند يمتلك صلاحية كاملة
        if (context.User.IsInRole("Admin") ||
            context.User.IsInRole("Manager") ||
            context.User.IsInRole("Receptionist") ||
            context.User.IsInRole("Nurse"))
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

        // 3. جلب بيانات الطبيب باستخدام الخدمة المحقونة
        var doctor = await _doctorService.GetDoctorByIdAsync(appointment.DoctorId);

        if (doctor != null && doctor.User != null && doctor.User.UserId == currentUserId)
        {
            context.Succeed(requirement);
        }
    }
}