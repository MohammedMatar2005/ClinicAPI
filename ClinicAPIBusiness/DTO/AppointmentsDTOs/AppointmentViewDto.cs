using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.DTO.AppointmentsDTOs
{

    public class AppointmentViewDTO
    {
        private string _statusTitle;

        public int AppointmentId { get; set; }
        public DateTime AppointmentDate { get; set; }

        
        public int PatientId { get; set; }
        public string PatientFullName { get; set; } = string.Empty;

        public string PatientNationalNumber { get; set; } = string.Empty;


        public int DoctorId { get; set; }
        public string DoctorFullName { get; set; } = string.Empty;

       
        public int AppointmentStatusId { get; set; }
        public string StatusTitle
        {
            get => _statusTitle switch
            {
                "Pending" => "معلق",
                "Confirmed" => "مؤكد",
                "Completed" => "مكتمل",
                "Cancelled" => "ملغي",
                _ => _statusTitle // إذا كانت القيمة فارغة أو أي نص آخر، ترجعه كما هو بدون تغيير
            };
            set => _statusTitle = value;
        }

        public string? Notes { get; set; }
    }

}
