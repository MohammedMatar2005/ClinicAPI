using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.DTO.PaitentVisitsDTOs
{
    public class ChoosePatientVisitDTO
    {
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public string DoctorName { get; set; } // أو اسم القسم/العيادة

        // 🎯 هذه الخاصية المدمجة التي سنعرضها في الـ ComboBox
        public string VisitDisplayInfo => $"زيارة رقم #{VisitId} | بتاريخ: {VisitDate:yyyy/MM/dd} - {VisitDate:hh:mm tt} (د. {DoctorName})";
    }
}
