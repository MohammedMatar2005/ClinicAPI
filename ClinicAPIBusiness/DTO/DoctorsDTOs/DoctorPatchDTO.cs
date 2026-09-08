using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.DTO.DoctorsDTOs
{
    /// <summary>
    /// DTO مخصص للتعديل الجزئي (PATCH) - كل الخصائص Nullable
    /// أي خاصية تُترك null يعني "لا تعدلها"، وأي خاصية لها قيمة يعني "حدّثها بهذه القيمة"
    /// </summary>
    public class DoctorPatchDTO
    {
        // ── بيانات الشخص ──
        public string? FirstName { get; set; }
        public string? SecondName { get; set; }
        public string? ThirdName { get; set; }
        public string? LastName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public bool? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? NationalNumber { get; set; }

        // ── بيانات المستخدم ──
        public string? Username { get; set; }
        public string? NewPassword { get; set; } // null = لا تغيّر كلمة المرور
        public int? RoleId { get; set; }
        public bool? IsUserActive { get; set; }

        // ── بيانات الطبيب ──
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public decimal? Salary { get; set; }
        public string? OfficeLocation { get; set; }
        public int? ExperienceYears { get; set; }
        public bool? IsDoctorActive { get; set; }
    }
}

