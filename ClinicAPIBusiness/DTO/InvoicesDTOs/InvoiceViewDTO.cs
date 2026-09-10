using System;

namespace ClinicAPIBusiness.DTO.InvoicesDTOs
{
    public class InvoiceViewDTO
    {
        public int InvoiceId { get; set; }
        public int VisitId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }

        // تفاصيل المبالغ والرسوم
        public decimal ConsultationFee { get; set; }
        public decimal LabTestFee { get; set; }
        public decimal ProcedureFee { get; set; }
        public decimal OtherCharges { get; set; }

        // الضرائب والخصومات
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }

        // الحالة والتواريخ
        public int InvoiceStatusId { get; set; }
        public string StatusName { get; set; } = string.Empty; // هنجيب اسمها من جدول الحالات
        public DateOnly DueDate { get; set; }
        public bool IsActive { get; set; }

        // الحقول الحسابية القادمة جاهزة ومحسوبة من الـ SQL Server
        public decimal SubTotal { get; set; }
        public decimal FinalAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public byte PaymentStatusId { get; set; }

        // بيانات إضافية مسطحة لعرض أسرع في الـ UI
        public string PatientFullName { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public string? PaymentStatusName { get; set; } = string.Empty;

        public int DoctorId { get; set; }
        public int DoctorUserId { get; set; }
    }
}