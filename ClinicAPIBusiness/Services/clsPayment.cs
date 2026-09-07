using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.DTO.PaymentsDTOs;

namespace ClinicAPIBusiness.Services
{
    public class clsPayment
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsPayment(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projection)
        // =========================================================================

        /// <summary>
        /// جلب جميع عمليات الدفع مسطحة بالكامل للأداء العالي
        /// </summary>
        public async Task<List<PaymentViewDTO>> GetAllPaymentsAsync()
        {
            return await _context.Payments
                .AsNoTracking()
                .Select(p => new PaymentViewDTO
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    PaymentAmount = p.PaymentAmount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatusId = p.PaymentStatusId,
                    TransactionReference = p.TransactionReference,
                    PaymentDate = p.PaymentDate,
                    IsActive = p.IsActive,

                    PatientFullName = p.Invoice.Visit.Appointment.Patient.Person != null
                        ? $"{p.Invoice.Visit.Appointment.Patient.Person.FirstName} {p.Invoice.Visit.Appointment.Patient.Person.SecondName} {p.Invoice.Visit.Appointment.Patient.Person.LastName}"
                        : string.Empty,

                    DoctorFullName = p.Invoice.Visit.Appointment.Doctor.User.Person != null
                        ? $"{p.Invoice.Visit.Appointment.Doctor.User.Person.FirstName} {p.Invoice.Visit.Appointment.Doctor.User.Person.SecondName} {p.Invoice.Visit.Appointment.Doctor.User.Person.LastName}"
                        : string.Empty,
                    PaymentStatusName = p.PaymentStatus != null ? p.PaymentStatus.StatusName : string.Empty
                }).ToListAsync();
        }

        /// <summary>
        /// جلب بيانات عملية دفع محددة بواسطة المعرف مع حماية الـ Nullability
        /// </summary>
        public async Task<PaymentDetailsDTO?> GetPaymentByIdAsync(int id)
        {
            if (id <= 0) return null;

            return await _context.Payments
                .AsNoTracking()
                .Select(p => new PaymentDetailsDTO
                {
                    PaymentId = p.PaymentId,
                    InvoiceId = p.InvoiceId,
                    PaymentAmount = p.PaymentAmount,
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatusId = p.PaymentStatusId,
                    TransactionReference = p.TransactionReference,
                    PaymentDate = p.PaymentDate,
                    IsActive = p.IsActive,
                    Notes = p.Notes,

                    PatientFullName = p.Invoice.Visit.Appointment.Patient.Person != null
                        ? $"{p.Invoice.Visit.Appointment.Patient.Person.FirstName} {p.Invoice.Visit.Appointment.Patient.Person.SecondName} {p.Invoice.Visit.Appointment.Patient.Person.LastName}"
                        : string.Empty,

                    DoctorFullName = p.Invoice.Visit.Appointment.Doctor.User.Person != null
                        ? $"{p.Invoice.Visit.Appointment.Doctor.User.Person.FirstName} {p.Invoice.Visit.Appointment.Doctor.User.Person.SecondName} {p.Invoice.Visit.Appointment.Doctor.User.Person.LastName}"
                        : string.Empty,

                    PaymentStatusName = p.PaymentStatus != null ? p.PaymentStatus.StatusName : string.Empty,
                    InvoiceTotalAmount = p.Invoice != null ? (p.Invoice.FinalAmount ?? 0m) : 0m
                }).FirstOrDefaultAsync(p => p.PaymentId == id);
        }

        /// <summary>
        /// جلب مجموع مبالغ الدفع لليوم الحالي
        /// </summary>
        public async Task<decimal> GetPaymentAmountsTodayAsync()
        {
            var today = DateTime.Today;

            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.PaymentDate.Date == today && p.IsActive == true)
                .SumAsync(p => p.PaymentAmount);
        }

        // =========================================================================
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// تسجيل عملية دفع جديدة في النظام
        /// </summary>
        public async Task<int> AddNewPaymentAsync(PaymentSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var paymentEntity = new Payment
            {
                InvoiceId = saveDto.InvoiceId,
                PaymentAmount = saveDto.PaymentAmount,
                PaymentMethod = saveDto.PaymentMethod,
                PaymentStatusId = saveDto.PaymentStatusId,
                TransactionReference = saveDto.TransactionReference,
                Notes = saveDto.Notes,
                IsActive = saveDto.IsActive,
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(paymentEntity);
            await _context.SaveChangesAsync();

            return paymentEntity.PaymentId;
        }

        /// <summary>
        /// تحديث بيانات حركة دفع قائمة.
        /// ملاحظة: TransactionReference لا يُمسح أبداً هنا — إذا وصلت قيمة فارغة/null من الواجهة
        /// نحتفظ بالقيمة القديمة المخزنة أصلاً بدل الكتابة فوقها بـ null.
        /// </summary>
        public async Task<bool> UpdatePaymentAsync(PaymentSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == saveDto.PaymentId);

            if (existingPayment == null) return false;

            existingPayment.PaymentAmount = saveDto.PaymentAmount;
            existingPayment.PaymentMethod = saveDto.PaymentMethod;
            existingPayment.PaymentStatusId = saveDto.PaymentStatusId;

            // لا نسمح بمسح رقم العملية: إذا لم تُرسل قيمة جديدة، نحافظ على القديمة
            if (!string.IsNullOrWhiteSpace(saveDto.TransactionReference))
            {
                existingPayment.TransactionReference = saveDto.TransactionReference;
            }

            existingPayment.Notes = saveDto.Notes;
            existingPayment.IsActive = saveDto.IsActive;

            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// حذف عملية دفع من النظام
        /// </summary>
        public async Task<bool> DeletePaymentAsync(int id)
        {
            if (id <= 0) return false;

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == id);
            if (payment == null) return false;

            _context.Payments.Remove(payment);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<decimal> GetTotalPaymentsForInvoiceAsync(int invoiceId)
        {
            if (invoiceId <= 0) return 0m;
            return await _context.Payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == invoiceId && p.IsActive)
                .SumAsync(p => p.PaymentAmount);
        }
    }
}