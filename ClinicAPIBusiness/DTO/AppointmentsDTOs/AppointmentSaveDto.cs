using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicAPIBusiness.DTO.AppointmentsDTOs
{
    public class AppointmentSaveDto
    {
        // 0 في الإضافة، ويحمل الرقم الفعلي في التعديل (يجب أن لا يكون سالباً)
       [Range(0, int.MaxValue, ErrorMessage = "معرف الموعد غير صحيح")]
       public int AppointmentId { get; set; }

        [Required(ErrorMessage = "معرف المريض مطلوب إجباري")]
        [Range(1, int.MaxValue, ErrorMessage = "معرف المريض يجب أن يكون رقماً صحيحاً أكبر من 0")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "معرف الطبيب مطلوب إجباري")]
        [Range(1, int.MaxValue, ErrorMessage = "معرف الطبيب يجب أن يكون رقماً صحيحاً أكبر من 0")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "حالة الموعد مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "معرف حالة الموعد يجب أن يكون رقماً صحيحاً أكبر من 0")]
        public int AppointmentStatusId { get; set; }

        [Required(ErrorMessage = "تاريخ ووقت الموعد مطلوب")]
        [DataType(DataType.DateTime, ErrorMessage = "صيغة التاريخ والوقت غير صالحة")]
        public DateTime AppointmentDate { get; set; }

        [StringLength(500, ErrorMessage = "الملاحظات يجب أن لا تتجاوز 500 حرف")]
        public string? Notes { get; set; }

        public string? ReasonForVisit { get; set; }
        public bool IsActive { get; set; } = true;
    }
}