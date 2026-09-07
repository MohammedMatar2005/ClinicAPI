using ClinicAPIBusiness.DTO.PeopleDTOs;
using ClinicAPIBusiness.Models;
using System;

namespace ClinicAPIBusiness.DTO.PatientsDTOs
{
    public class PatientSaveDTO
    {
        // المعرف الرقمي للمريض: يكون 0 في حالة الإضافة (Insert)، ويحمل القيمة الحقيقية في حالة التعديل (Update)

        public int PatientId { get; set; }

        public int PersonId { get; set; }

        public string? EmergencyContact { get; set; }

        public string? EmergencyPhone { get; set; }

        public string? BloodType { get; set; }

        public string? Allergies { get; set; }

        public string? MedicalHistory { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

    }     

}