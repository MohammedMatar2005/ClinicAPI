using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicAPIBusiness.DTO.InvoicesDTOs
{
    public class InvoiceSaveDTO
    {
        // 0 في الإضافة، ويحمل الرقم الحقيقي في التعديل
        [Range(0, int.MaxValue, ErrorMessage = "معرف الفاتورة غير صحيح")]
        public int InvoiceId { get; set; }

        [Required(ErrorMessage = "معرف زيارة المريض (VisitId) مطلوب إجباري لربط الفاتورة")]
        [Range(1, int.MaxValue, ErrorMessage = "معرف الزيارة يجب أن يكون رقماً صحيحاً أكبر من 0")]
        public int VisitId { get; set; }

        [Required(ErrorMessage = "رقم الفاتورة مطلوب")]
        [StringLength(50, ErrorMessage = "رقم الفاتورة يجب أن لا يتجاوز 50 حرف")]
        public string InvoiceNumber { get; set; } = null!;

        [Required(ErrorMessage = "رسوم الكشفية مطلوبة")]
        [Range(0.0, 100000.0, ErrorMessage = "رسوم الكشفية لا يمكن أن تكون قيمة سالبة")]
        public decimal ConsultationFee { get; set; }

        [Range(0.0, 100000.0, ErrorMessage = "رسوم المختبر لا يمكن أن تكون قيمة سالبة")]
        public decimal LabTestFee { get; set; } = 0;

        [Range(0.0, 100000.0, ErrorMessage = "رسوم الإجراءات الطبية لا يمكن أن تكون قيمة سالبة")]
        public decimal ProcedureFee { get; set; } = 0;

        [Range(0.0, 100000.0, ErrorMessage = "الرسوم الأخرى لا يمكن أن تكون قيمة سالبة")]
        public decimal OtherCharges { get; set; } = 0;

        // مطابقة لـ CK_Invoices_Tax_Discount (النسبة بين 0 و 100)
        [Range(0.0, 100.0, ErrorMessage = "نسبة الضريبة يجب أن تكون بين 0% و 100%")]
        public decimal? TaxPercentage { get; set; } = 0;

        [Range(0.0, 100000.0, ErrorMessage = "قيمة الضريبة المضافة لا يمكن أن تكون سالبة")]
        public decimal? TaxAmount { get; set; } = 0;

        [Range(0.0, 100.0, ErrorMessage = "نسبة الخصم يجب أن تكون بين 0% و 100%")]
        public decimal? DiscountPercentage { get; set; } = 0;

        [Range(0.0, 100000.0, ErrorMessage = "قيمة الخصم لا يمكن أن تكون سالبة")]
        public decimal? DiscountAmount { get; set; } = 0;

        [Required(ErrorMessage = "حالة الفاتورة مطلوبة")]
        [Range(1, 255, ErrorMessage = "حالة الفاتورة غير صحيحة")]
        public byte StatusId { get; set; } = 1;

        [DataType(DataType.Date, ErrorMessage = "صيغة تاريخ الاستحقاق غير صالحة")]
        public DateOnly DueDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}