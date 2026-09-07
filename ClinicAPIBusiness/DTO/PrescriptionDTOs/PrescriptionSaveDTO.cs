using System;

namespace ClinicAPIBusiness.DTO.PrescriptionDTOs
{
    public class PrescriptionSaveDTO
    {
        public int PrescriptionId { get; set; } // 0 للإضافة ويحمل المعرف في التعديل
        public int VisitId { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
    }
}