using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using ClinicAPIBusiness.DTO;
using ClinicAPIBusiness.Models;

namespace ClinicAPIBusiness.Services
{
    public class clsClinicSettings
    {
        private readonly ClinicManagementSystemContext _context;
        private const int StaticClinicId = 1; // 💡 لأن الجدول يحتوي على صف واحد فقط برقمه الفريد

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsClinicSettings(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }


        /// <summary>
        /// جلب الإعدادات الوحيدة للعيادة مسطحة ومجهزة بالكامل للـ UI
        /// </summary>
        public async Task<ClinicSettings?> GetClinicSettingsAsync()
        {
            return await _context.ClinicSettings
                .AsNoTracking()
                .Where(c => c.ClinicId == StaticClinicId)
                .Select(c => new ClinicSettings
                {
                    ClinicId = c.ClinicId,
                    ClinicName = c.ClinicName,
                    ClinicNameEn = c.ClinicNameEn,
                    Logo = c.Logo,
                    TaxNumber = c.TaxNumber,
                    PhoneNumber1 = c.PhoneNumber1,
                    PhoneNumber2 = c.PhoneNumber2,
                    Email = c.Email,
                    Address = c.Address,
                    Website = c.Website,
                    AppointmentDurationMinutes = c.AppointmentDurationMinutes,
                    ClinicStartTime = c.ClinicStartTime,
                    ClinicEndTime = c.ClinicEndTime,
                    WeekendDays = c.WeekendDays,
                    DefaultCurrency = c.DefaultCurrency,
                    TaxRate = c.TaxRate,
                    DefaultInvoiceNotes = c.DefaultInvoiceNotes,
                    UpdatedByUserId = c.UpdatedByUserId
                })
                .FirstOrDefaultAsync();
        }


        /// <summary>
        /// فحص ما إذا كانت الإعدادات قد تمت تهيئتها مسبقاً في النظام أم لا
        /// </summary>
        public async Task<bool> IsSettingsInitializedAsync()
        {
            return await _context.ClinicSettings
                .AnyAsync(c => c.ClinicId == StaticClinicId);
        }

        public async Task<ClinicSettings?> GetSettingsAsync()
        {
            return await _context.ClinicSettings.Where(c => c.ClinicId == 1)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateClinicSettingsAsync(ClinicSettings? settings)
        {
            if (settings == null) return false;

            // نبحث عن السجل رقم 1
            var existingSettings = await _context.ClinicSettings
                .FirstOrDefaultAsync(c => c.ClinicId == 1);

            bool isNew = false;

            // 💡 إذا لم يجد السجل (أول مرة فتح للنظام)، نقوم بإنشاء كائن جديد
            if (existingSettings == null)
            {
                existingSettings = new ClinicSettings { ClinicId = 1 };
                isNew = true;
            }

            // تحديث الحقول
            existingSettings.ClinicName = settings.ClinicName;
            existingSettings.ClinicNameEn = settings.ClinicNameEn;

            // 🛠️ إصلاح حماية الشعار: يتم التحديث في كل الحالات (سواء صورة جديدة أو null للحذف)
            existingSettings.Logo = settings.Logo;

            existingSettings.TaxNumber = settings.TaxNumber;
            existingSettings.PhoneNumber1 = settings.PhoneNumber1;
            existingSettings.PhoneNumber2 = settings.PhoneNumber2;
            existingSettings.Email = settings.Email;
            existingSettings.Address = settings.Address;
            existingSettings.Website = settings.Website;
            existingSettings.AppointmentDurationMinutes = settings.AppointmentDurationMinutes;
            existingSettings.ClinicStartTime = settings.ClinicStartTime;
            existingSettings.ClinicEndTime = settings.ClinicEndTime;
            existingSettings.WeekendDays = settings.WeekendDays;
            existingSettings.DefaultCurrency = settings.DefaultCurrency;
            existingSettings.TaxRate = settings.TaxRate;
            existingSettings.DefaultInvoiceNotes = settings.DefaultInvoiceNotes;
            existingSettings.UpdatedAt = DateTime.Now;

            if (isNew)
            {
                _context.ClinicSettings.Add(existingSettings);
            }

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // لطرد الخطأ الحقيقي لمعرفته أثناء الـ Debugging
                throw new Exception($"خطأ في قاعدة البيانات: {ex.Message}", ex);
            }
        }
        public async Task<string> GetClinicName()
        {
            return await _context.ClinicSettings
                .Where(c => c.ClinicId == StaticClinicId)
                .Select(c => c.ClinicName)
                .FirstOrDefaultAsync() ?? string.Empty;
        }
    }
}