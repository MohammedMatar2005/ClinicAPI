
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.Services;

namespace ClinicAPIBusiness.DTO.UsersDTOs
{
    public class UserSaveDTO
    {
        public int UserId { get; set; } // 0 في الإضافة
        public int PersonId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // مجهز لاستقبال التشفير (بزنس)
        public int RoleId { get; set; }
        public bool IsActive { get; set; }



    }
}