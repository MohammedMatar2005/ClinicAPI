using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models;

namespace ClinicAPIBusiness.Services
{
    public class clsInvoiceStatusService
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsInvoiceStatusService(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================
        // 1. جلب كل الحالات كـ List للاستخدام مع الـ BindingSource
        // =========================================
        public async Task<List<InvoiceStatus>> GetAllInvoiceStatusesAsync()
        {
            return await _context.InvoiceStatuses
                .AsNoTracking()
                .ToListAsync();
        }

        // =========================================
        // 2. البحث عن حالة بواسطة الـ ID
        // =========================================
        public async Task<InvoiceStatus?> GetInvoiceStatusByIdAsync(int statusId)
        {
            if (statusId <= 0) return null;

            return await _context.InvoiceStatuses
                .FirstOrDefaultAsync(s => s.StatusId == statusId);
        }

        // =========================================
        // 3. إضافة حالة جديدة
        // =========================================
        public async Task<int> CreateInvoiceStatusAsync(InvoiceStatus status)
        {
            if (status == null)
                throw new ArgumentNullException(nameof(status));

            if (string.IsNullOrWhiteSpace(status.StatusName))
                throw new ArgumentException("اسم حالة الفاتورة لا يمكن أن يكون فارغاً!");

            _context.InvoiceStatuses.Add(status);
            await _context.SaveChangesAsync();

            return status.StatusId;
        }

        // =========================================
        // 4. تحديث بيانات الحالة
        // =========================================
        public async Task<bool> UpdateInvoiceStatusAsync(InvoiceStatus status)
        {
            if (status == null)
                throw new ArgumentNullException(nameof(status));

            if (status.StatusId <= 0)
                return false;

            if (string.IsNullOrWhiteSpace(status.StatusName))
                throw new ArgumentException("اسم الحالة لا يمكن أن يكون فارغاً عند التعديل!");

            var existingStatus = await _context.InvoiceStatuses
                .FirstOrDefaultAsync(s => s.StatusId == status.StatusId);

            if (existingStatus == null) return false;

            existingStatus.StatusName = status.StatusName;

            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================
        // 5. حذف حالة فاتورة
        // =========================================
        public async Task<bool> DeleteInvoiceStatusAsync(int statusId)
        {
            if (statusId <= 0) return false;

            var status = await _context.InvoiceStatuses
                .FirstOrDefaultAsync(s => s.StatusId == statusId);

            if (status == null) return false;

            _context.InvoiceStatuses.Remove(status);
            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================
        // 6. فحص وجود الحالة برمجياً
        // =========================================
        public async Task<bool> IsInvoiceStatusExistAsync(int statusId)
        {
            if (statusId <= 0) return false;

            return await _context.InvoiceStatuses
                .AnyAsync(s => s.StatusId == statusId);
        }
    }
}