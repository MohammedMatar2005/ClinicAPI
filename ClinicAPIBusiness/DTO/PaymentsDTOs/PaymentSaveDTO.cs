using System;

namespace ClinicAPIBusiness.DTO.PaymentsDTOs
{
    public class PaymentSaveDTO
    {
        // يحمل 0 عند إنشاء دفعة جديدة (Insert)، ويحمل المعرف الفعلي عند التعديل (Update)
        public int PaymentId { get; set; }

        // [InvoiceId] [int] NOT NULL -> مربوط بجدول الفواتير
        public int InvoiceId { get; set; }

        // [PaymentAmount] [decimal](18, 2) NOT NULL -> يجب أن يكون أكبر من 0 بناءً على الـ CHECK Constraint
        public decimal PaymentAmount { get; set; }

        // [PaymentMethod] [nvarchar](50) NULL -> يقبل Null في السكربت
        public string? PaymentMethod { get; set; }

        // [PaymentStatusId] [int] NOT NULL -> لديه DEFAULT ((2)) في السكربت (مثلاً: جزئي أو قيد الانتظار)
        // نضعه nullable هنا لكي نتيح للسيرفر تطبيق القيمة الافتراضية إذا لم نرسلها صراحة من الشاشة
        public int PaymentStatusId { get; set; }

        // [TransactionReference] [nvarchar](100) NULL -> يقبل Null ولديه Unique Constraint
        public string? TransactionReference { get; set; }

        // [Notes] [nvarchar](max) NULL -> يقبل Null
        public string? Notes { get; set; }

        // [IsActive] [bit] NOT NULL -> لديه DEFAULT ((1))
        public bool IsActive { get; set; }

        // ملاحظة هامة جداً بخصوص التواريخ:
        // طالما أنك وضعت Default Constraints في الـ SQL للسداد والإنشاء، يفضل الاحتفاظ بها في الـ DTO 
        // كـ Nullable DateTime (DateTime?)؛ وذلك لكي تمنح الـ Repository أو الـ المطور خيار تمرير تاريخ مخصص 
        // من الشاشة (مثل سداد بأثر رجعي) أو تركها Null لتقوم قاعدة البيانات بإسناد التاريخ الحالي تلقائياً.

        public DateTime? PaymentDate { get; set; }
    }
}