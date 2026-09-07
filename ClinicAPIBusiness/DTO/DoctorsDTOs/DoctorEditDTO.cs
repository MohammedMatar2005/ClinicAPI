// DoctorEditDTO.cs — DTO مخصص لتحميل بيانات التعديل
namespace ClinicAPIBusiness.DTO.DoctorsDTOs
{
    public class DoctorEditDTO
    {
        // ── معرفات ضرورية للـ Update لاحقاً ──
        public int DoctorId { get; set; }
        public int PersonId { get; set; }
        public int UserId { get; set; }

        // ── البيانات الشخصية ──
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string ThirdName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public bool Gender { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string NationalNumber { get; set; } = string.Empty;
        public string? Address { get; set; }

        // ── بيانات الحساب ──
        public string Username { get; set; } = string.Empty;
        public bool UserIsActive { get; set; }
        public int RoleId { get; set; }

        // ── البيانات المهنية ──
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? OfficeLocation { get; set; }
        public decimal? Salary { get; set; }
        public int ExperienceYears { get; set; }
        public bool DoctorIsActive { get; set; }
    }
}