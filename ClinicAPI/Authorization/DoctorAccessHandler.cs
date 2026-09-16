using ClinicAPIBusiness.DTO.DoctorsDTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class DoctorAccessRequirement : IAuthorizationRequirement { }

public class DoctorAccessHandler : AuthorizationHandler<DoctorAccessRequirement, DoctorViewDTO>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DoctorAccessRequirement requirement,
        DoctorViewDTO doctor)
    {
        // 1. الطاقم الإداري والمساند يملك صلاحية كاملة للاطلاع على بيانات الأطباء
        if (context.User.IsInRole("Admin") ||
            context.User.IsInRole("Manager") ||
            context.User.IsInRole("Receptionist") ||
            context.User.IsInRole("Nurse"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. استخراج الـ User ID الخاص بالمستخدم الحالي من التوكن
        var userIdClaim = context.User.FindFirstValue("UserId")
                          ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdClaim, out int currentUserId))
        {
            // 3. فحص الملكية: إذا كان الطبيب يقرأ ملفه الشخصي فقط
            if (doctor.UserId != null && doctor.UserId == currentUserId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}