using System.ComponentModel.DataAnnotations;

namespace ClinicAPIBusiness.DTO.UsersDTOs
{
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
        public string OldPassword { get; set; } = null!;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [MinLength(6, ErrorMessage = "كلمة المرور الجديدة يجب أن تكون 6 أحرف على الأقل")]
        public string NewPassword { get; set; } = null!;
    }
}