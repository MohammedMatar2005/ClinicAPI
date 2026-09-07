using System;

namespace ClinicAPIBusiness.DTO.PatientsDTOs
{
    public class PatientViewDTO
    {
        // ==========================================
        // 1. بيانات المريض الأساسية (من جدول Patients)
        // ==========================================
        public int PatientId { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        // ==========================================
        // 2. بيانات الشخص المسطحة (من جدول People عبر الـ Join)
        // ==========================================
        public int PersonId { get; set; }
        public string PatientFullName { get; set; } = string.Empty; // الاسم مدمج (الأول + الثاني + العائلة)
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; } = string.Empty;

        public string NationalNumber { get; set; } // رقم الهوية / الرقم الوطني
         

        // حقل ذكي ومحسوب للـ UI (يتم حسابه في البزنس من تاريخ الميلاد)
        public int Age { get; set; }
    }
}