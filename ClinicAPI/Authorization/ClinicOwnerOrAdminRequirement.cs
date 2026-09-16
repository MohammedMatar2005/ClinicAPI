using Microsoft.AspNetCore.Authorization;

// This class represents the authorization rule itself.
// It does NOT contain logic.
// It simply defines the requirement:
// "Owner OR Admin can access the clinic resource."
public class ClinicOwnerOrAdminRequirement : IAuthorizationRequirement
{
}
