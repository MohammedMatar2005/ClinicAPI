using System;

namespace ClinicAPIBusiness.DTO.PrescriptionDTOs
{
    public class PrescriptionViewDTO
    {
        public int PrescriptionId { get; set; }
        public int VisitId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // التسطيح الذكي القادم من العلاقات
        public string PatientFullName { get; set; } = string.Empty;
        public string DoctorFullName { get; set; } = string.Empty;
    }
}