using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.DTO.PrescriptionDTOs;

namespace ClinicAPIBusiness.Services
{
    public class clsPrescription
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsPrescription(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projections)
        // =========================================================================

        /// <summary>
        /// جلب جميع الروشتات مسطحة وجاهزة للعرض السريع
        /// </summary>
        public async Task<List<PrescriptionViewDTO>> GetAllPrescriptionsAsync()
        {
            return await _context.Prescriptions
                .AsNoTracking()
                .Select(p => new PrescriptionViewDTO
                {
                    PrescriptionId = p.PrescriptionId,
                    VisitId = p.VisitId,
                    MedicationName = p.MedicineName,
                    Dosage = p.Dosage,
                    Frequency = p.Frequency,
                    Instructions = p.Instructions,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,

                    PatientFullName = p.Visit.Appointment.Patient.Person != null
                        ? $"{p.Visit.Appointment.Patient.Person.FirstName} {p.Visit.Appointment.Patient.Person.SecondName} {p.Visit.Appointment.Patient.Person.LastName}"
                        : string.Empty,

                    DoctorFullName = p.Visit.Appointment.Doctor.User.Person != null
                        ? $"{p.Visit.Appointment.Doctor.User.Person.FirstName} {p.Visit.Appointment.Doctor.User.Person.SecondName} {p.Visit.Appointment.Doctor.User.Person.LastName}"
                        : string.Empty
                }).ToListAsync();
        }

        /// <summary>
        /// جلب تفاصيل روشتة معينة بواسطة المعرف الرقمي ID
        /// </summary>
        public async Task<PrescriptionDetailsDTO?> GetPrescriptionByIdAsync(int prescriptionId)
        {
            if (prescriptionId <= 0) return null;

            return await _context.Prescriptions
                .AsNoTracking()
                .Select(p => new PrescriptionDetailsDTO
                {
                    PrescriptionId = p.PrescriptionId,
                    VisitId = p.VisitId,
                    MedicationName = p.MedicineName,
                    Dosage = p.Dosage,
                    Frequency = p.Frequency,
                    Instructions = p.Instructions,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,

                    PatientFullName = p.Visit.Appointment.Patient.Person != null
                        ? $"{p.Visit.Appointment.Patient.Person.FirstName} {p.Visit.Appointment.Patient.Person.SecondName} {p.Visit.Appointment.Patient.Person.LastName}"
                        : string.Empty,

                    DoctorFullName = p.Visit.Appointment.Doctor.User.Person != null
                        ? $"{p.Visit.Appointment.Doctor.User.Person.FirstName} {p.Visit.Appointment.Doctor.User.Person.SecondName} {p.Visit.Appointment.Doctor.User.Person.LastName}"
                        : string.Empty
                }).FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);
        }

        /// <summary>
        /// جلب جميع الروشتات الخاصة بمريض معين
        /// </summary>
        public async Task<List<PrescriptionViewDTO>> GetPrescriptionsByPatientIdAsync(int patientId)
        {
            if (patientId <= 0) return new List<PrescriptionViewDTO>();

            return await _context.Prescriptions
                .AsNoTracking()
                .Where(p => p.Visit.Appointment.PatientId == patientId)
                .Select(p => new PrescriptionViewDTO
                {
                    PrescriptionId = p.PrescriptionId,
                    VisitId = p.VisitId,
                    MedicationName = p.MedicineName,
                    Dosage = p.Dosage,
                    Frequency = p.Frequency,
                    Instructions = p.Instructions,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,

                    PatientFullName = p.Visit.Appointment.Patient.Person != null
                        ? $"{p.Visit.Appointment.Patient.Person.FirstName} {p.Visit.Appointment.Patient.Person.SecondName} {p.Visit.Appointment.Patient.Person.LastName}"
                        : string.Empty,

                    DoctorFullName = p.Visit.Appointment.Doctor.User.Person != null
                        ? $"{p.Visit.Appointment.Doctor.User.Person.FirstName} {p.Visit.Appointment.Doctor.User.Person.SecondName} {p.Visit.Appointment.Doctor.User.Person.LastName}"
                        : string.Empty
                }).ToListAsync();
        }

        /// <summary>
        /// جلب الروشتات المرتبطة بزيارة عيادية محددة
        /// </summary>
        public async Task<List<PrescriptionViewDTO>> GetPrescriptionsByVisitIdAsync(int visitId)
        {
            if (visitId <= 0) return new List<PrescriptionViewDTO>();

            return await _context.Prescriptions
                .AsNoTracking()
                .Where(p => p.VisitId == visitId)
                .Select(p => new PrescriptionViewDTO
                {
                    PrescriptionId = p.PrescriptionId,
                    VisitId = p.VisitId,
                    MedicationName = p.MedicineName,
                    Dosage = p.Dosage,
                    Frequency = p.Frequency,
                    Instructions = p.Instructions,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,

                    PatientFullName = p.Visit.Appointment.Patient.Person != null
                        ? $"{p.Visit.Appointment.Patient.Person.FirstName} {p.Visit.Appointment.Patient.Person.SecondName} {p.Visit.Appointment.Patient.Person.LastName}"
                        : string.Empty,

                    DoctorFullName = p.Visit.Appointment.Doctor.User.Person != null
                        ? $"{p.Visit.Appointment.Doctor.User.Person.FirstName} {p.Visit.Appointment.Doctor.User.Person.SecondName} {p.Visit.Appointment.Doctor.User.Person.LastName}"
                        : string.Empty
                }).ToListAsync();
        }

        // =========================================================================
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة روشتة علاجية جديدة للزيارة الحالية
        /// </summary>
        public async Task<int> AddNewPrescriptionAsync(PrescriptionSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var prescriptionEntity = new Prescription
            {
                VisitId = saveDto.VisitId,
                MedicineName = saveDto.MedicationName,
                Dosage = saveDto.Dosage,
                Frequency = saveDto.Frequency,
                Instructions = saveDto.Instructions,
                StartDate = saveDto.StartDate,
                EndDate = saveDto.EndDate
            };

            _context.Prescriptions.Add(prescriptionEntity);
            await _context.SaveChangesAsync();

            return prescriptionEntity.PrescriptionId;
        }

        /// <summary>
        /// تحديث بيانات روشتة علاجية قائمة
        /// </summary>
        public async Task<bool> UpdatePrescriptionAsync(PrescriptionSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var existingPrescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.PrescriptionId == saveDto.PrescriptionId);

            if (existingPrescription == null) return false;

            existingPrescription.MedicineName = saveDto.MedicationName;
            existingPrescription.Dosage = saveDto.Dosage;
            existingPrescription.Frequency = saveDto.Frequency;
            existingPrescription.Instructions = saveDto.Instructions;
            existingPrescription.StartDate = saveDto.StartDate;
            existingPrescription.EndDate = saveDto.EndDate;

            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// حذف روشتة بالكامل بواسطة المعرف
        /// </summary>
        public async Task<bool> DeletePrescriptionAsync(int prescriptionId)
        {
            if (prescriptionId <= 0) return false;

            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId);

            if (prescription == null) return false;

            _context.Prescriptions.Remove(prescription);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// الفحص السريع لوجود الروشتة في النظام
        /// </summary>
        public async Task<bool> IsPrescriptionExistAsync(int prescriptionId)
        {
            if (prescriptionId <= 0) return false;

            return await _context.Prescriptions
                .AnyAsync(p => p.PrescriptionId == prescriptionId);
        }
    }
}