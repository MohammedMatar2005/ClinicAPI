using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


public class ClinicOwnerOrAdminHandler
    : AuthorizationHandler<ClinicOwnerOrAdminRequirement, int>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClinicOwnerOrAdminRequirement requirement,
        int targetUserId) // تغيير التسمية لتدل على أننا نقارن مع User ID
    {
        // 1. صلاحيات الإدارة (Admin أو Manager)
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Manager"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. التحقق من الهوية واستخراج الـ ID من الـ Claims
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? context.User.FindFirstValue("UserId");

        if (int.TryParse(userIdClaim, out int currentUserId) &&
            currentUserId == targetUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}