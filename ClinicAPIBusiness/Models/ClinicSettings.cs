using System;

namespace ClinicAPIBusiness.Models
{
    public class ClinicSettings
    {
        // 💡 المفتاح الأساسي الإلزامي للموديل (سيكون دائماً = 1 بقوة الـ Constraint)
        public int ClinicId { get; set; }

        // ---- [البيانات العامة] ----
        public string ClinicName { get; set; } = string.Empty;
        public string? ClinicNameEn { get; set; }
        public byte[]? Logo { get; set; } // تم تعديله لـ Logo ليطابق اسم الـ Column
        public string? TaxNumber { get; set; }
        public string Address { get; set; } = string.Empty;

        // ---- [بيانات الاتصال] ----
        public string PhoneNumber1 { get; set; } = string.Empty;
        public string? PhoneNumber2 { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }

        // ---- [جدولة المواعيد] ----
        public int AppointmentDurationMinutes { get; set; } = 20;
        public TimeSpan ClinicStartTime { get; set; } // TimeSpan متوافق مع time(7)
        public TimeSpan ClinicEndTime { get; set; }   // TimeSpan متوافق مع time(7)

        // 💡 تم تحويلها إلى string لأن الـ DB تخزنها كنص مفصول بفاصلة (مثل: "Friday,Saturday")
        public string? WeekendDays { get; set; }

        // ---- [الإعدادات المالية] ----
        public string DefaultCurrency { get; set; } = "$";
        public decimal TaxRate { get; set; } // مطابقة لـ TaxRate decimal(5,2)
        public string? DefaultInvoiceNotes { get; set; }

        // ---- [بيانات المتابعة والنظام] ----
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}