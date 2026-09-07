using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models; // تأكد من مطابقة مسار الموديلات والـ DbContext لديك
using ClinicAPIBusiness.DTO.PatientsDTOs;
using ClinicAPIBusiness.DTO.PatientVisitsDTOs;
using ClinicAPIBusiness.DTO.PaitentVisitsDTOs;

namespace ClinicAPIBusiness.Services
{
    public class clsPatientVisit
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsPatientVisit(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projection)
        // =========================================================================

        /// <summary>
        /// جلب قائمة جميع الزيارات مسطحة على مقاس الـ ViewDTO
        /// </summary>
        public async Task<List<PatientVisitViewDTO>> GetAllPatientVisitsAsync()
        {
            return await _context.PatientVisits.Where(p => !p.Invoices.Any())
                .AsNoTracking() // تحسين الأداء للقراءة
                .Select(p => new PatientVisitViewDTO
                {
                    VisitId = p.VisitId,
                    AppointmentId = p.AppointmentId,
                    VisitDate = p.VisitDate,
                    Diagnosis = p.Diagnosis,

                    PatientFullName = (p.Appointment != null && p.Appointment.Patient != null && p.Appointment.Patient.Person != null)
                        ? p.Appointment.Patient.Person.FirstName + " " + p.Appointment.Patient.Person.SecondName + " " + p.Appointment.Patient.Person.LastName
                        : string.Empty,

                    DoctorFullName = (p.Appointment != null && p.Appointment.Doctor != null && p.Appointment.Doctor.User != null && p.Appointment.Doctor.User.Person != null)
                        ? p.Appointment.Doctor.User.Person.FirstName + " " + p.Appointment.Doctor.User.Person.SecondName + " " + p.Appointment.Doctor.User.Person.LastName
                        : string.Empty,

                    AppointmentReason = p.Appointment != null ? (p.Appointment.ReasonForVisit ?? string.Empty) : string.Empty,
                    VisitStatusTitle = p.VisitStatus != null ? p.VisitStatus.StatusTitle : string.Empty
                })
                .ToListAsync();
        }

        public async Task<List<PatientVisitViewDTO>> GetUninvoicedPatientVisitsAsync()
        {
            return await _context.PatientVisits
                .AsNoTracking()
                // الشرط: جلب الزيارات التي ليس لها أي سجل في جدول الفواتير (Invoices)
                .Where(p => !_context.Invoices.Any(inv => inv.VisitId == p.VisitId))
                .Select(p => new PatientVisitViewDTO
                {
                    VisitId = p.VisitId,
                    AppointmentId = p.AppointmentId,
                    VisitDate = p.VisitDate,
                    Diagnosis = p.Diagnosis,

                    PatientFullName = (p.Appointment != null && p.Appointment.Patient != null && p.Appointment.Patient.Person != null)
                        ? p.Appointment.Patient.Person.FirstName + " " + p.Appointment.Patient.Person.SecondName + " " + p.Appointment.Patient.Person.LastName
                        : string.Empty,

                    DoctorFullName = (p.Appointment != null && p.Appointment.Doctor != null && p.Appointment.Doctor.User != null && p.Appointment.Doctor.User.Person != null)
                        ? p.Appointment.Doctor.User.Person.FirstName + " " + p.Appointment.Doctor.User.Person.SecondName + " " + p.Appointment.Doctor.User.Person.LastName
                        : string.Empty,

                    AppointmentReason = p.Appointment != null ? (p.Appointment.ReasonForVisit ?? string.Empty) : string.Empty,
                    VisitStatusTitle = p.VisitStatus != null ? p.VisitStatus.StatusTitle : string.Empty
                })
                .ToListAsync();
        }

        /// <summary>
        /// جلب بيانات زيارة محددة بواسطة الـ ID وتحويلها لـ DetailsDTO
        /// </summary>
        public async Task<PatientVisitDetailsDTO?> GetPatientVisitByIdAsync(int id)
        {
            if (id <= 0) return null;

            return await _context.PatientVisits
                .AsNoTracking()
                .Where(v => v.VisitId == id)
                .Select(p => new PatientVisitDetailsDTO
                {
                    VisitId = p.VisitId,
                    AppointmentId = p.AppointmentId,
                    VisitDate = p.VisitDate,
                    Symptoms = p.Symptoms,
                    Diagnosis = p.Diagnosis,
                    TreatmentPlan = p.TreatmentPlan,
                    Notes = p.Notes,
                    CreatedDate = p.CreatedDate,
                    BloodPressure = p.BloodPressure,
                    Temperature = p.Temperature,
                    HeartRate = p.HeartRate,
                    RespiratoryRate = p.RespiratoryRate,
                    Weight = p.Weight,
                    Height = p.Height,

                    PatientFullName = (p.Appointment != null && p.Appointment.Patient != null && p.Appointment.Patient.Person != null)
                        ? p.Appointment.Patient.Person.FirstName + " " + p.Appointment.Patient.Person.SecondName + " " + p.Appointment.Patient.Person.LastName
                        : string.Empty,

                    DoctorFullName = (p.Appointment != null && p.Appointment.Doctor != null && p.Appointment.Doctor.User != null && p.Appointment.Doctor.User.Person != null)
                        ? p.Appointment.Doctor.User.Person.FirstName + " " + p.Appointment.Doctor.User.Person.SecondName + " " + p.Appointment.Doctor.User.Person.LastName
                        : string.Empty,

                    AppointmentReason = p.Appointment != null ? (p.Appointment.ReasonForVisit ?? string.Empty) : string.Empty,
                    DoctorSpecialty = (p.Appointment != null && p.Appointment.Doctor != null) ? (p.Appointment.Doctor.Specialization ?? string.Empty) : string.Empty
                })
                .FirstOrDefaultAsync();
        }        // =========================================================================
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة زيارة مريض جديدة في النظام بعد التحقق من عدم تكرار الزيارة للموعد
        /// </summary>
        public async Task<int> AddNewPatientVisitAsync(PatientVisitSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            // الفحص المباشر عبر الـ Context
            bool isPatientVisitExist = await _context.PatientVisits
                .AnyAsync(v => v.AppointmentId == saveDto.AppointmentId);

            if (isPatientVisitExist)
                throw new ArgumentException("هذا الموعد مسجل بالفعل في النظام!");

            var patientVisitEntity = new PatientVisit
            {
                AppointmentId = saveDto.AppointmentId,
                Symptoms = saveDto.Symptoms,
                Diagnosis = saveDto.Diagnosis,
                TreatmentPlan = saveDto.TreatmentPlan,
                BloodPressure = saveDto.BloodPressure,
                Temperature = saveDto.Temperature,
                HeartRate = saveDto.HeartRate,
                RespiratoryRate = saveDto.RespiratoryRate,
                Weight = saveDto.Weight,
                Height = saveDto.Height,
                Notes = saveDto.Notes,
                VisitDate = DateTime.Now // تسند برمجياً وقت الحفظ الحالي
            };

            _context.PatientVisits.Add(patientVisitEntity);
            await _context.SaveChangesAsync();

            return patientVisitEntity.VisitId;
        }

        /// <summary>
        /// تحديث بيانات زيارة قائمة بالتتبع المباشر لـ EF Core
        /// </summary>
        public async Task<bool> UpdatePatientVisitAsync(PatientVisitSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var existingPatientVisit = await _context.PatientVisits
                .FirstOrDefaultAsync(v => v.VisitId == saveDto.VisitId);

            if (existingPatientVisit == null) return false;

            existingPatientVisit.AppointmentId = saveDto.AppointmentId;
            existingPatientVisit.Symptoms = saveDto.Symptoms;
            existingPatientVisit.Diagnosis = saveDto.Diagnosis;
            existingPatientVisit.TreatmentPlan = saveDto.TreatmentPlan;
            existingPatientVisit.BloodPressure = saveDto.BloodPressure;
            existingPatientVisit.Temperature = saveDto.Temperature;
            existingPatientVisit.HeartRate = saveDto.HeartRate;
            existingPatientVisit.RespiratoryRate = saveDto.RespiratoryRate;
            existingPatientVisit.Weight = saveDto.Weight;
            existingPatientVisit.Height = saveDto.Height;
            existingPatientVisit.Notes = saveDto.Notes;

            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// حذف زيارة من النظام بواسطة معرفها الرقمي
        /// </summary>
        public async Task<bool> DeletePatientVisitAsync(int id)
        {
            if (id <= 0) return false;

            var visit = await _context.PatientVisits.FirstOrDefaultAsync(v => v.VisitId == id);
            if (visit == null) return false;

            _context.PatientVisits.Remove(visit);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// جلب جميع زيارات مريض محدد لغايات شاشات الاختيار
        /// </summary>
        public async Task<List<ChoosePatientVisitDTO>> GetAllVisitsByPatientId(int patientId)
        {
            if (patientId <= 0) return new List<ChoosePatientVisitDTO>();

            return await _context.PatientVisits
                .AsNoTracking()
                .Where(p => p.Appointment.PatientId == patientId)
                .Select(p => new ChoosePatientVisitDTO
                {
                    VisitId = p.VisitId,
                    VisitDate = p.VisitDate,
                    DoctorName = p.Appointment.Doctor.User.Person.FirstName
                        + " " + p.Appointment.Doctor.User.Person.SecondName
                        + " " + p.Appointment.Doctor.User.Person.LastName
                })
                .ToListAsync();
        }

       
    }
}