using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.DTO.DoctorsDTOs
{
    public class DoctorDetailsDTO
    {
        // التخصص الطبي
        public string Specialization { get; set; } = string.Empty;

        // رقم مزاولة المهنة / الترخيص الطبي
        public string LicenseNumber { get; set; } = string.Empty;

        // الراتب (يفضل استخدام decimal للقيم المالية لضمان الدقة العالية)
        public decimal Salary { get; set; }

        // مكان العيادة / المكتب (مثل: الطابق الثاني - عيادة رقم 204)
        public string OfficeLocation { get; set; } = string.Empty;

        // عدد سنوات الخبرة
        public int ExperienceYears { get; set; }

        // حالة الحساب المهني للطبيب (نشط / غير نشط في العيادة)
        public bool IsActive { get; set; } = true;
        public int DoctorId { get;  set; }
    }
}
