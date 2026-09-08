namespace ClinicAPIBusiness.DTO.PatientsDTOs
{
    /// <summary>
    /// DTO مخصص للتعديل الجزئي (PATCH) على بيانات المريض - كل الخصائص Nullable
    /// أي خاصية تُترك null يعني "لا تعدلها"، وأي خاصية لها قيمة يعني "حدّثها بهذه القيمة"
    /// </summary>
    public class PatientPatchDTO
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

        // ── بيانات المريض ──
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? MedicalHistory { get; set; }
        public bool? IsActive { get; set; }
    }
}