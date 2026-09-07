using System;

namespace ClinicAPIBusiness.DTO.PrescriptionDTOs
{
    public class PrescriptionDetailsDTO
    {
        // حقول جدول الروشتات كاملة كما هي في الـ Entity
        public int PrescriptionId { get; set; }
        public int VisitId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string? Instructions { get; set; } // يقبل Null لأنه حقل اختياري للملاحظات
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // التسطيح الاحترافي (Flattening) لجلب الأسماء مباشرة بدون حيازة كائنات كاملة
        public string PatientFullName { get; set; } = string.Empty;
        public string DoctorFullName { get; set; } = string.Empty;
    }
}