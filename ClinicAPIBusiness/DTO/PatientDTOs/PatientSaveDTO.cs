using ClinicAPIBusiness.DTO.PeopleDTOs;
using System;

namespace ClinicAPIBusiness.DTO.PatientsDTOs
{
    public class PatientSaveDTO
    {
        // المعرف الرقمي للمريض: يكون 0 في حالة الإضافة (Insert)، ويحمل القيمة الحقيقية في حالة التعديل (Update)

        public PersonSaveDTO Person { get; set; } = new PersonSaveDTO();
        public PatientViewDTO PatientDetails { get; set; }

       
    }
}