using ClinicAPIBusiness.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.Services
{
    // تعريف الـ Enum لتسهيل التعامل مع درجات الخطورة برمجياً داخل النظام
    public enum enLogSeverity : byte
    {
        Information = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public class clsLoggingService
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext عبر المشيّد
        public clsLoggingService(ClinicManagementSystemContext context)
        {
            _context = context;
        }

        /// <summary>
        /// تسجيل حدث جديد في النظام بشكل غير متزامن
        /// </summary>
        /// <param name="eventName">اسم أو وصف الحدث الذي تم</param>
        /// <param name="severity">درجة خطورة الحدث (استخدم الـ Enum)</param>
        /// <param name="userId">معرف المستخدم الفاعل (يمكن تمريره null للعمليات الخارجية)</param>
        public async Task<bool> LogAsync(string eventName, enLogSeverity severityLevel, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            try
            {
                var log = new SystemLog
                {
                    EventName = eventName,
                    SeverityLevel = (byte)severityLevel,
                    LogTimestamp = DateTime.Now,
                    CreatedByUserID = userId
                };

                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                // يمكنك كتابة الكود المناسب للـ Fallback هنا في حال فشل الاتصال بقاعدة البيانات
                return false;
            }
        }

        /// <summary>
        /// جلب كافة السجلات المسجلة في النظام مرتبة من الأحدث إلى الأقدم
        /// </summary>
        public async Task<List<SystemLog>> GetAllLogsAsync()
        {
            return await _context.SystemLogs
                .Include(l => l.CreatorUser) // جلب بيانات المستخدم الذي قام بالفعل لطباعة اسمه
                .OrderByDescending(l => l.LogTimestamp)
                .ToListAsync();
        }

        /// <summary>
        /// جلب السجلات بناءً على درجة خطورة محددة
        /// </summary>
        public async Task<List<SystemLog>> GetLogsBySeverityAsync(enLogSeverity severity)
        {
            return await _context.SystemLogs
                .Include(l => l.CreatorUser)
                .Where(l => l.SeverityLevel == (byte)severity)
                .OrderByDescending(l => l.LogTimestamp)
                .ToListAsync();
        }

        /// <summary>
        /// حذف السجلات القديمة جداً لتخفيف حجم قاعدة البيانات (مثال: السجلات الأقدم من 3 أشهر)
        /// </summary>
        public async Task<int> ClearOldLogsAsync(int keepDays = 90)
        {
            var cutoffDate = DateTime.Now.AddDays(-keepDays);
            var oldLogs = _context.SystemLogs.Where(l => l.LogTimestamp < cutoffDate);

            _context.SystemLogs.RemoveRange(oldLogs);
            return await _context.SaveChangesAsync();
        }
    }
}