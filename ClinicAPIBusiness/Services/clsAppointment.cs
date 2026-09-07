using ClinicAPIBusiness.DTO.AppointmentsDTOs;
using ClinicAPIBusiness.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.Services
{
    public class clsAppointment
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsAppointment(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projection)
        // =========================================================================

        /// <summary>
        /// جلب ملخص المواعيد على مقاس الـ DTO للعرض السريع في واجهات المستخدم
        /// </summary>
        public async Task<List<AppointmentViewDTO>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments.Where(a => a.StatusId != 4)
                .AsNoTracking()
                .Select(a => new AppointmentViewDTO
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    PatientId = a.PatientId,
                    PatientNationalNumber = a.Patient.Person.NationalNumber,
                    PatientFullName = a.Patient.Person.FirstName + " " + a.Patient.Person.SecondName + " " + a.Patient.Person.LastName,
                    DoctorId = a.DoctorId,
                    DoctorFullName = a.Doctor.User.Person.FirstName + " " + a.Doctor.User.Person.SecondName + " " + a.Doctor.User.Person.LastName,
                    AppointmentStatusId = a.Status.AppointmentStatusId,
                    StatusTitle = a.Status.StatusName,
                    Notes = a.Notes
                }).OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }

        /// <summary>
        /// جلب موعد محدد بواسطة الـ ID لغايات العرض أو تفاصيل الشاشة
        /// </summary>
        public async Task<AppointmentDetailsDTO?> GetAppointmentByIdAsync(int id)
        {
            if (id <= 0) return null;

            return await _context.Appointments
                .AsNoTracking()
                .Select(a => new AppointmentDetailsDTO
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDate = a.AppointmentDate,
                    PatientId = a.PatientId,
                    PatientFullName = a.Patient.Person.FirstName + " " + a.Patient.Person.SecondName + " " + a.Patient.Person.LastName,
                    DoctorId = a.DoctorId,
                    DoctorFullName = a.Doctor.User.Person.FirstName + " " + a.Doctor.User.Person.SecondName + " " + a.Doctor.User.Person.LastName,
                    ReasonForVisit = a.ReasonForVisit,
                    AppointmentStatusId = a.Status.AppointmentStatusId,
                    StatusName = a.Status.StatusName,
                    Notes = a.Notes,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    IsActive = a.IsActive
                }).FirstOrDefaultAsync(a => a.AppointmentId == id);
        }

        // =========================================================================
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة موعد جديد
        /// </summary>
        public async Task<int> AddNewAppointmentAsync(AppointmentSaveDto saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            if (saveDto.AppointmentDate < DateTime.Today)
                throw new ArgumentException("لا يمكن حجز موعد في تاريخ قديم!");

            var appointmentEntity = new Appointment
            {
                PatientId = saveDto.PatientId,
                DoctorId = saveDto.DoctorId,
                StatusId = saveDto.AppointmentStatusId,
                AppointmentDate = saveDto.AppointmentDate,
                Notes = saveDto.Notes,
                CreatedDate = DateTime.Now,
                ReasonForVisit = saveDto.ReasonForVisit
            };

            if(!await IsPatientAvailableAsync(saveDto.PatientId, saveDto.AppointmentDate))
                throw new InvalidOperationException("المريض لديه موعد آخر في نفس الوقت.");

            if(!await IsDoctorAvailableAsync(saveDto.DoctorId, saveDto.AppointmentDate))
                throw new InvalidOperationException("الطبيب ليس متاحاً في هذا الوقت.");

            _context.Appointments.Add(appointmentEntity);
            await _context.SaveChangesAsync();

            return appointmentEntity.AppointmentId;
        }

        /// <summary>
        /// تحديث بيانات موعد قائم
        /// </summary>
        public async Task<bool> UpdateAppointmentAsync(AppointmentSaveDto saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var existingAppointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == saveDto.AppointmentId);

            if (existingAppointment == null) return false;

            existingAppointment.PatientId = saveDto.PatientId;
            existingAppointment.DoctorId = saveDto.DoctorId;
            existingAppointment.StatusId = saveDto.AppointmentStatusId;
            existingAppointment.AppointmentDate = saveDto.AppointmentDate;
            existingAppointment.Notes = saveDto.Notes;
            existingAppointment.UpdatedDate = DateTime.Now;
            existingAppointment.ReasonForVisit = saveDto.ReasonForVisit;
            existingAppointment.IsActive = saveDto.IsActive;

            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// حذف موعد بواسطة المعرف الرقمي الخاص به
        /// </summary>
        public async Task<bool> DeleteAppointmentAsync(int id)
        {
            if (id <= 0) return false;

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (appointment == null) return false;

            _context.Appointments.Remove(appointment);
            return await _context.SaveChangesAsync() > 0;
        }

        // فحص توفر المريض في تاريخ معين
        public async Task<bool> IsPatientAvailableAsync(int patientId, DateTime appointmentDate, int? currentAppointmentId = null)
        {
            bool hasConflict = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId &&
                               a.AppointmentDate.Date == appointmentDate.Date &&
                               a.AppointmentDate.Hour == appointmentDate.Hour &&
                               a.StatusId != 4 &&
                               (!currentAppointmentId.HasValue || a.AppointmentId != currentAppointmentId.Value)); // 👈 هنا سحر الاستثناء

            return !hasConflict;
        }

        public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime appointmentDate, int? currentAppointmentId = null)
        {
            bool hasConflict = await _context.Appointments
               .AnyAsync(a => a.DoctorId == doctorId &&
                              a.AppointmentDate.Date == appointmentDate.Date &&
                              a.AppointmentDate.Hour == appointmentDate.Hour &&
                              a.StatusId != 4 &&
                              (!currentAppointmentId.HasValue || a.AppointmentId != currentAppointmentId.Value)); // 👈 هنا سحر الاستثناء

            return !hasConflict;
        }

        public async Task<bool> IsAppointmentCancelled(int appointmentId)
        {
            // فحص إذا كان الموعد المحدد برقم المعرف يحمل حالة ملغي (4)
            return await _context.Appointments
                .AnyAsync(a => a.AppointmentId == appointmentId && a.Status.AppointmentStatusId == 4);
        }

        public async Task<int> GetTodaysAppointmentsCountAsync()
        {
            return await _context.Appointments
                .CountAsync(a => a.AppointmentDate.Date == DateTime.Today);
        }
    }
}