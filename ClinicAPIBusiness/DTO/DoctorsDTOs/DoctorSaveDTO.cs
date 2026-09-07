using ClinicAPIBusiness.DTO.PeopleDTOs;
using ClinicAPIBusiness.DTO.UsersDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.DTO.DoctorsDTOs
{
    // 1. تعريف الـ DTO المجمع بطريقة احترافية
    public class DoctorSaveDTO
    {
        public PersonSaveDTO Person { get; set; } = new PersonSaveDTO();

        // 2. بيانات حساب النظام (اسم المستخدم، كلمة المرور، صلاحية الدور)
        public UserSaveDTO User { get; set; } = new UserSaveDTO();

        // 3. البيانات المهنية للطبيب (الكلاس المذكور بالأعلى)
        public DoctorDetailsDTO DoctorDetails { get; set; } = new DoctorDetailsDTO();
    }
}
