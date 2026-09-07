namespace ClinicAPIBusiness.DTO.PatientVisitsDTOs
{
    public class PatientVisitViewDTO
    {
        public int VisitId { get; set; }
        public int AppointmentId { get; set; }
        public DateTime VisitDate { get; set; }
        public string PatientFullName { get; set; } = string.Empty;
        public string DoctorFullName { get; set; } = string.Empty;
        public string AppointmentReason { get; set; } = string.Empty;
        public string? Diagnosis { get; set; }
        public string VisitStatusTitle { get; set; } = string.Empty;
    }
}