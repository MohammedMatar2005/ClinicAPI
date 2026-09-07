using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.DTO.DoctorsDTOs
{

    public class DoctorViewDTO
    {
        public int DoctorId { get; set; }
        public int PersonId { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? OfficeLocation { get; set; }
        public int ExperienceYears { get; set; }
        public bool IsActive { get; set; }

        // بيانات المستخدم والاسم المسطحة القادمة من جداول العلاقات
        public int UserId { get; set; }
        public string DoctorFullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NationalNumber { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
    }

}
