using System;

namespace ClinicAPIBusiness.DTO.PaymentsDTOs
{
    public class PaymentDetailsDTO
    {
        // حقول جدول المدفوعات كاملة
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public decimal PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public int PaymentStatusId { get; set; }
        public string? TransactionReference { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; } // تم تضمينه هنا لأنه nvarchar(max)
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        // تفاصيل البيانات المسطحة الإضافية للتقرير أو السند المالي
        public string PatientFullName { get; set; } = string.Empty;
        public string DoctorFullName { get; set; } = string.Empty;
        public string PaymentStatusName { get; set; } = string.Empty;
        public decimal? InvoiceTotalAmount { get; set; } // إجمالي الفاتورة الأصلية للمقارنة
    }
}