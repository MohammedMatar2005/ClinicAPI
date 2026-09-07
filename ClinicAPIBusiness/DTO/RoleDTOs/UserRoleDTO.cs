using System;

namespace ClinicAPIBusiness.DTO.UserRoleDTOs
{
    public class UserRoleDTO
    {
        public int RoleId { get; set; } // 0 في الإضافة ويحمل المعرف في التعديل
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}