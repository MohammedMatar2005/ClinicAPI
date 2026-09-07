using System;

namespace ClinicAPIBusiness.DTO.AppointmentsDTOs
{
    public class AppointmentDetailsDTO
    {
        public int AppointmentId { get; set; }

        // بيانات المريض
        public int PatientId { get; set; }
        public string PatientFullName { get; set; } = string.Empty; // 👈 اسم المريض الكامل

        // بيانات الطبيب
        public int DoctorId { get; set; }
        public string DoctorFullName { get; set; } = string.Empty;   // 👈 اسم الطبيب الكامل

        public DateTime AppointmentDate { get; set; }
        public string? ReasonForVisit { get; set; }

        // حالة الموعد
        public int AppointmentStatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;       // 👈 اسم الحالة (مؤكد، ملغي، إلخ)

        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}