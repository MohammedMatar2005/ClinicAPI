using System;

namespace ClinicAPIBusiness.DTO.PatientVisitsDTOs
{
    public class PatientVisitSaveDTO
    {
        // يحمل القيمة 0 عند إضافة زيارة جديدة (Insert) ويحمل المعرف الحقيقي عند التعديل (Update)
        public int VisitId { get; set; }

        // الموعد المرتبط بالزيارة (حقل إجباري NOT NULL)
        public int AppointmentId { get; set; }

        // النصوص التشخيصية والسريرية (Nullable)
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public string? TreatmentPlan { get; set; }

        // حقول المؤشرات الحيوية متطابقة مع دقة أنواع الـ SQL (مثل decimal للوزن والطول والحرارة)
        public string? BloodPressure { get; set; }
        public decimal? Temperature { get; set; }
        public int? HeartRate { get; set; }
        public int? RespiratoryRate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }

        public string? Notes { get; set; }

       
    }
}