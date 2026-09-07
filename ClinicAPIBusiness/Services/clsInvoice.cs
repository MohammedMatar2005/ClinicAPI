using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.DTO.InvoicesDTOs;

namespace ClinicAPIBusiness.Services
{
    public class clsInvoice
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor بناءً على المعمارية الجديدة
        public clsInvoice(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projection)
        // =========================================================================

        /// <summary>
        /// جلب قائمة جميع الفواتير مسطحة على مقاس الـ DTO لسرعة نقل البيانات
        /// </summary>
        public async Task<List<InvoiceViewDTO>> GetAllInvoicesAsync()
        {
            // 1. حساب المبالغ المتبقية في الذاكرة كما فعلت بدقة
            var remainingAmounts = await _context.Invoices
                .Select(i => new
                {
                    i.InvoiceId,
                    RemainingAmount = (i.FinalAmount ?? 0m) - _context.Payments
                        .Where(p => p.InvoiceId == i.InvoiceId && p.IsActive)
                        .Sum(p => (decimal?)p.PaymentAmount) ?? 0m
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.RemainingAmount);

            // 2. جلب الفواتير وبناء الحالات
            var invoices = await _context.Invoices
                .AsNoTracking()
                .Select(i => new
                {
                    // نجلبه كـ Anonymous Object أولاً لتسهيل دمج الحسبة الديناميكية
                    i.InvoiceId,
                    i.VisitId,
                    i.InvoiceNumber,
                    i.InvoiceDate,
                    i.ConsultationFee,
                    i.LabTestFee,
                    i.ProcedureFee,
                    i.OtherCharges,
                    TaxPercentage = i.TaxPercentage ?? 0m,
                    TaxAmount = i.TaxAmount ?? 0m,
                    DiscountPercentage = i.DiscountPercentage ?? 0m,
                    DiscountAmount = i.DiscountAmount ?? 0m,
                    InvoiceStatusId = i.StatusId,

                    // جلب اسم حالة الفاتورة التنظيمية مباشرة من جدول الحالات عبر الـ Navigation Property
                    StatusName = i.Status.StatusName,

                    DueDate = i.DueDate,
                    i.IsActive,
                    VisitDate = i.Visit.VisitDate,
                    PatientFullName = i.Visit.Appointment.Patient.Person.FirstName + " " +
                                      i.Visit.Appointment.Patient.Person.SecondName + " " +
                                      i.Visit.Appointment.Patient.Person.LastName,
                    SubTotal = i.SubTotal ?? 0m,
                    FinalAmount = i.FinalAmount ?? 0m
                })
                .ToListAsync();

            // 3. تحويل البيانات إلى الـ DTO النهائي وحساب حالة الدفع بالاعتماد على الـ Dictionary
            return invoices.Select(i => {
                // جلب المبلغ المتبقي للفاتورة الحالية
                decimal remaining = remainingAmounts.ContainsKey(i.InvoiceId) ? remainingAmounts[i.InvoiceId] : i.FinalAmount;

                // الحسبة الذكية لحالة الدفع:
                string paymentStatusName;
                byte paymentStatusId;

                if (remaining <= 0)
                {
                    paymentStatusName = "مدفوعة";
                    paymentStatusId = 2; // Paid حسب السكربت السابق
                }
                else if (remaining < i.FinalAmount)
                {
                    paymentStatusName = "مدفوعة جزئياً";
                    paymentStatusId = 1; // Partially Paid
                }
                else
                {
                    paymentStatusName = "غير مدفوعة";
                    paymentStatusId = 0; // Unpaid
                }

                return new InvoiceViewDTO
                {
                    InvoiceId = i.InvoiceId,
                    VisitId = i.VisitId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    ConsultationFee = i.ConsultationFee,
                    LabTestFee = i.LabTestFee,
                    ProcedureFee = i.ProcedureFee,
                    OtherCharges = i.OtherCharges,
                    TaxPercentage = i.TaxPercentage,
                    TaxAmount = i.TaxAmount,
                    DiscountPercentage = i.DiscountPercentage,
                    DiscountAmount = i.DiscountAmount,

                    // حالة الفاتورة (نشطة، ملغاة)
                    InvoiceStatusId = i.InvoiceStatusId,
                    StatusName = i.StatusName, // تأتي من الـ DB (مثل: Active, Cancelled)

                    DueDate = i.DueDate,
                    IsActive = i.IsActive,
                    VisitDate = i.VisitDate,
                    PatientFullName = i.PatientFullName,
                    SubTotal = i.SubTotal,
                    FinalAmount = i.FinalAmount,

                    // المتبقي وحالة الدفع المحسوبة ديناميكياً بنجاح
                    RemainingAmount = remaining < 0 ? 0 : remaining,
                    PaymentStatusId = paymentStatusId,
                    PaymentStatusName = paymentStatusName // تحتاج لإضافة هذا الحقل في الـ DTO إذا لم يكن موجوداً لعرضه بالواجهة
                };
            }).ToList();
        }

        /// <summary>
        /// جلب فاتورة محددة بواسطة الـ ID بالتتبع لغايات البزنس أو التعديل
        /// </summary>
        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            if (id <= 0) return null;

            return await _context.Invoices
                // 1. السلسلة الأولى: جلب بيانات المريض كاملة
                .Include(i => i.Visit)
                    .ThenInclude(v => v.Appointment)
                        .ThenInclude(a => a.Patient)
                            .ThenInclude(p => p.Person)

                // 2. السلسلة الثانية: نعود للـ Visit لجلب بيانات الطبيب والمستخدم الخاص به
                .Include(i => i.Visit)
                    .ThenInclude(v => v.Appointment)
                        .ThenInclude(a => a.Doctor)
                            .ThenInclude(d => d.User)
                                .ThenInclude(u => u.Person)

                // 3. السلسلة الثالثة: جلب حالة الفاتورة لتلوينها وعرضها في الشاشة
                .Include(i => i.Status)

                // 4. شرط البحث الجوهري بناءً على المعرّف الممرر
                .FirstOrDefaultAsync(i => i.InvoiceId == id);
        }

        // =========================================================================    
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة فاتورة جديدة في النظام بعد حساب القيم المالية برمجياً وتوليد رقم الفاتورة
        /// </summary>
        public async Task<int> AddNewInvoiceAsync(InvoiceSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            // فحص مباشرة على الـ Context: هل رقم الفاتورة مسجل مسبقاً؟
            bool isInvoiceNumberExist = await _context.Invoices
                .AnyAsync(i => i.InvoiceNumber == saveDto.InvoiceNumber);

            if (isInvoiceNumberExist)
                throw new ArgumentException("رقم الفاتورة هذا مسجل مسبقاً في النظام!");

            // الحسابات المالية
            decimal subTotal = saveDto.ConsultationFee + saveDto.LabTestFee + saveDto.ProcedureFee + saveDto.OtherCharges;
            decimal taxAmount = saveDto.TaxPercentage.HasValue ? (subTotal * (saveDto.TaxPercentage.Value / 100)) : 0;
            decimal discountAmount = saveDto.DiscountPercentage.HasValue ? (subTotal * (saveDto.DiscountPercentage.Value / 100)) : 0;

            // توليد رقم فاتورة تلقائي بسيط في حال لم يتم إرساله من الواجهة لضمان عدم حدوث خطأ
            string finalInvoiceNumber = string.IsNullOrWhiteSpace(saveDto.InvoiceNumber)
                ? $"INV-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
                : saveDto.InvoiceNumber;

            var invoiceEntity = new Invoice
            {
                VisitId = saveDto.VisitId,
                InvoiceNumber = finalInvoiceNumber,
                ConsultationFee = saveDto.ConsultationFee,
                LabTestFee = saveDto.LabTestFee,
                ProcedureFee = saveDto.ProcedureFee,
                OtherCharges = saveDto.OtherCharges,
                TaxPercentage = saveDto.TaxPercentage,
                TaxAmount = taxAmount,
                DiscountPercentage = saveDto.DiscountPercentage,
                DiscountAmount = discountAmount,
                StatusId = saveDto.StatusId,
                DueDate = saveDto.DueDate,
                IsActive = saveDto.IsActive
            };

            _context.Invoices.Add(invoiceEntity);
            await _context.SaveChangesAsync();

            return invoiceEntity.InvoiceId; // إرجاع الـ ID الجديد تلقائياً بعد الحفظ
        }

        /// <summary>
        /// تحديث بيانات فاتورة قائمة (تغيير الرسوم فقط).
        /// ملاحظة هامة: لا يتم تعديل رقم الفاتورة (InvoiceNumber) ولا حالتها (StatusId)
        /// هنا عمداً، لأنهما يجب ألا يتغيرا نتيجة تعديل بسيط في الرسوم.
        /// - رقم الفاتورة: يُولَّد مرة واحدة فقط عند الإنشاء ولا يتغير أبداً بعدها.
        /// - حالة الفاتورة: تُدار من مكان مختص (شاشة/عملية تغيير حالة) وليس من هنا.
        /// </summary>
        public async Task<bool> UpdateInvoiceAsync(InvoiceSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            // جلب السجل تحت تتبع الـ EF Core مباشرة من جدول الفواتير
            var existingInvoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == saveDto.InvoiceId);

            if (existingInvoice == null) return false;

            // إعادة حساب المبالغ
            decimal subTotal = saveDto.ConsultationFee + saveDto.LabTestFee + saveDto.ProcedureFee + saveDto.OtherCharges;
            decimal taxAmount = saveDto.TaxPercentage.HasValue ? (subTotal * (saveDto.TaxPercentage.Value / 100)) : 0;
            decimal discountAmount = saveDto.DiscountPercentage.HasValue ? (subTotal * (saveDto.DiscountPercentage.Value / 100)) : 0;

            // تحديث الحقول المالية فقط
            existingInvoice.VisitId = saveDto.VisitId;
            // لا نلمس InvoiceNumber هنا إطلاقاً — يبقى كما تم توليده عند الإنشاء
            existingInvoice.ConsultationFee = saveDto.ConsultationFee;
            existingInvoice.LabTestFee = saveDto.LabTestFee;
            existingInvoice.ProcedureFee = saveDto.ProcedureFee;
            existingInvoice.OtherCharges = saveDto.OtherCharges;
            existingInvoice.TaxPercentage = saveDto.TaxPercentage;
            existingInvoice.TaxAmount = taxAmount;
            existingInvoice.DiscountPercentage = saveDto.DiscountPercentage;
            existingInvoice.DiscountAmount = discountAmount;
            // لا نلمس StatusId هنا إطلاقاً — تبقى الحالة الحالية للفاتورة كما هي
            existingInvoice.DueDate = saveDto.DueDate;
            existingInvoice.IsActive = saveDto.IsActive;

            // حفظ التغييرات المتتبعة
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// حذف فاتورة من النظام بواسطة المعرف الرقمي الخاص بها
        /// </summary>
        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            if (id <= 0) return false;

            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == id);
            if (invoice == null) return false;

            _context.Invoices.Remove(invoice);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// فحص وجود فاتورة سابقة لزيارة معينة من خلال رقم الزيارة
        /// </summary>
        public async Task<bool> IsInvoiceExistByVisitIdAsync(int visitId)
        {
            if (visitId <= 0) return false;

            return await _context.Invoices.AnyAsync(i => i.VisitId == visitId);
        }

        public async Task<bool> IsInvoiceFullyPaidAsync(int invoiceId)
        {
            if (invoiceId <= 0) return false;

            var finalAmount = await _context.Invoices
                .Where(i => i.InvoiceId == invoiceId)
                .Select(i => (decimal?)i.FinalAmount)
                .FirstOrDefaultAsync();

            // إذا كانت الفاتورة غير موجودة في قاعدة البيانات أصلاً، نخرج بـ false
            if (finalAmount == null) return false;

            // حساب مجموع الدفعات النشطة
            var totalPayments = await _context.Payments
                .Where(p => p.InvoiceId == invoiceId && p.IsActive)
                .SumAsync(p => p.PaymentAmount);

            // مقارنة المجموع بالفاتورة (إذا كانت الفاتورة صفر والمجموع صفر، ستعيد true وهو الصحيح)
            return totalPayments >= finalAmount.Value;
        }

        public async Task<decimal> GetInvoiceFinalAmountAsync(int selectedInvoiceId)
        {
            return await _context.Invoices.Where(i => i.InvoiceId == selectedInvoiceId)
                .Select(i => (decimal?)i.FinalAmount ?? 0m)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// جلب المبلغ المتبقي الفعلي على الفاتورة (الإجمالي - مجموع الدفعات النشطة).
        /// تستخدم لمنع الدفع الزائد (Overpayment).
        /// </summary>
        public async Task<decimal> GetInvoiceRemainingAmountAsync(int invoiceId, int excludePaymentId = -1)
        {
            decimal finalAmount = await GetInvoiceFinalAmountAsync(invoiceId);

            var paymentsQuery = _context.Payments
                .Where(p => p.InvoiceId == invoiceId && p.IsActive);

            // عند التعديل، يجب استبعاد الدفعة الحالية نفسها من مجموع "الدفعات السابقة"
            // حتى لا تُحتسب مرتين عند مقارنة قيمتها الجديدة بالمتبقي.
            if (excludePaymentId != -1)
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentId != excludePaymentId);
            }

            decimal totalPaid = await paymentsQuery.SumAsync(p => (decimal?)p.PaymentAmount) ?? 0m;

            decimal remaining = finalAmount - totalPaid;
            return remaining < 0 ? 0 : remaining;
        }

        public async Task<bool> IsInvoiceIssuedForVisitAsync(int visitId)
        {
            // يترجم إلى: SELECT CASE WHEN EXISTS (SELECT 1 FROM Invoices WHERE VisitId = @visitId) THEN 1 ELSE 0 END
            return await _context.Invoices
                                 .AnyAsync(i => i.VisitId == visitId);
        }

        public async Task<decimal> GetInvoicePaidAmountAsync(int selectedInvoiceId)
        {
            var totalPaid = await _context.Payments
                .Where(p => p.InvoiceId == selectedInvoiceId && p.IsActive)
                .SumAsync(p => (decimal?)p.PaymentAmount) ?? 0m;

            return totalPaid;
        }
    }
}