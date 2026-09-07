using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models; // تم الاعتماد على الـ namespace الموحد الخاص بك

namespace ClinicAPIBusiness.Services
{
    public class AppointmentStatusService
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public AppointmentStatusService(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================
        // 1. جلب كل الحالات كـ List للاستخدام مع الـ BindingSource
        // =========================================
        public async Task<List<AppointmentStatus>> GetAllStatusesAsync()
        {
            return await _context.AppointmentStatuses
                .AsNoTracking()
                .ToListAsync();
        }

        // =========================================
        // 2. البحث عن حالة بواسطة الـ ID
        // =========================================
        public async Task<AppointmentStatus?> GetStatusByIdAsync(int statusId)
        {
            if (statusId <= 0) return null;

            return await _context.AppointmentStatuses
                .FirstOrDefaultAsync(s => s.AppointmentStatusId == statusId);
        }

        // =========================================
        // 3. إضافة حالة جديدة
        // =========================================
        public async Task<int> CreateStatusAsync(AppointmentStatus status)
        {
            if (status == null)
                throw new ArgumentNullException(nameof(status));

            if (string.IsNullOrWhiteSpace(status.StatusName))
                throw new ArgumentException("اسم الحالة لا يمكن أن يكون فارغاً!");

            _context.AppointmentStatuses.Add(status);
            await _context.SaveChangesAsync();

            return status.AppointmentStatusId;
        }

        // =========================================
        // 4. تحديث بيانات الحالة
        // =========================================
        public async Task<bool> UpdateStatusAsync(AppointmentStatus status)
        {
            if (status == null)
                throw new ArgumentNullException(nameof(status));

            if (status.AppointmentStatusId <= 0)
                return false;

            if (string.IsNullOrWhiteSpace(status.StatusName))
                throw new ArgumentException("اسم الحالة لا يمكن أن يكون فارغاً عند التعديل!");

            var existingStatus = await _context.AppointmentStatuses
                .FirstOrDefaultAsync(s => s.AppointmentStatusId == status.AppointmentStatusId);

            if (existingStatus == null) return false;

            existingStatus.StatusName = status.StatusName;

            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================
        // 5. حذف حالة مواعيد
        // =========================================
        public async Task<bool> DeleteStatusAsync(int statusId)
        {
            if (statusId <= 0) return false;

            var status = await _context.AppointmentStatuses
                .FirstOrDefaultAsync(s => s.AppointmentStatusId == statusId);

            if (status == null) return false;

            _context.AppointmentStatuses.Remove(status);
            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================
        // 6. فحص وجود الحالة برمجياً
        // =========================================
        public async Task<bool> IsStatusExistAsync(int statusId)
        {
            if (statusId <= 0) return false;

            return await _context.AppointmentStatuses
                .AnyAsync(s => s.AppointmentStatusId == statusId);
        }
    }
}