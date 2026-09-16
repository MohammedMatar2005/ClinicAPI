using ClinicAPIBusiness.DTO.InvoicesDTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class InvoiceAccessRequirement : IAuthorizationRequirement { }

public class InvoiceAccessHandler : AuthorizationHandler<InvoiceAccessRequirement, InvoiceViewDTO>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InvoiceAccessRequirement requirement,
        InvoiceViewDTO invoice)
    {
        // 1. الأدوار الإدارية والاستقبال لهم صلاحية كاملة لرؤية أي فاتورة
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Receptionist"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. استخراج الـ User ID الخاص بالمستخدم الحالي
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? context.User.FindFirstValue("UserId");

        if (int.TryParse(userIdClaim, out int currentUserId))
        {
            // 3. إذا كان الطبيب هو المرتبط بالفاتورة المحددة
            if (invoice.DoctorUserId == currentUserId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}