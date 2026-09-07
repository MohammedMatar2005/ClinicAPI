using System;

namespace ClinicAPIBusiness.DTO.PatientVisitsDTOs
{
    public class PatientVisitDetailsDTO
    {
        // ==========================================
        // 1. البيانات الطبية والسريرية الكاملة (جدول PatientVisits)
        // ==========================================
        public int VisitId { get; set; }
        public int AppointmentId { get; set; }
        public DateTime VisitDate { get; set; }

        // النصوص الكاملة والعملاقة (nvarchar(max))
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }

        // المؤشرات الحيوية بالكامل (Vitals) بدقتها الرقمية القادمة من قاعدة البيانات
        public string? BloodPressure { get; set; }
        public decimal? Temperature { get; set; }
        public int? HeartRate { get; set; }
        public int? RespiratoryRate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }

        // ==========================================
        // 2. البيانات المسطحة الإضافية للشاشات التفصيلية
        // ==========================================
        public string PatientFullName { get; set; } = string.Empty;
        public string DoctorFullName { get; set; } = string.Empty;
        public string AppointmentReason { get; set; } = string.Empty;

        // يمكنك هنا إضافة حقول مخصصة لشاشة التفاصيل فقط إذا لزم الأمر، مثل رقم الهاتف أو التخصص
        public string? DoctorSpecialty { get; set; }
    }
}