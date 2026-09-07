using System;

namespace ClinicAPIBusiness.DTO.PaymentsDTOs
{
    public class PaymentViewDTO
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public int PaymentStatusId { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsActive { get; set; }

        // التسطيح الذكي (Flattening) للعرض الفوري في الجدول بدون رحلات إضافية للداتا
        public string PatientFullName { get; set; } = string.Empty;
        public string DoctorFullName { get; set; } = string.Empty;
        public string PaymentStatusName { get; set; } = string.Empty; // القيمة النصية للحالة من جدول الحالات
    }
}