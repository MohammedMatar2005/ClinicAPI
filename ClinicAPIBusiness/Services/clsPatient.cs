using ClinicAPIBusiness.DTO.PatientsDTOs;
using ClinicAPIBusiness.DTO.DoctorsDTOs;
using ClinicAPIBusiness.DTO.PeopleDTOs;
using ClinicAPIBusiness.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.Services
{
    public class clsPatient
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsPatient(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projection)
        // =========================================================================

        /// <summary>
        /// جلب قائمة جميع المرضى مسطحة على مقاس الـ ViewDTO لسرعة نقل البيانات
        /// </summary>
        public async Task<List<PatientViewDTO>> GetAllPatientsAsync()
        {
            return await _context.Patients.Where(p => p.IsActive == true)
                .AsNoTracking()
                .Select(p => new PatientViewDTO
                {
                    PatientId = p.PatientId,
                    PersonId = p.PersonId,
                    EmergencyContact = p.EmergencyContact,
                    EmergencyPhone = p.EmergencyPhone,
                    BloodType = p.BloodType,
                    Allergies = p.Allergies,
                    MedicalHistory = p.MedicalHistory,
                    IsActive = p.IsActive,
                    CreatedDate = p.CreatedDate,

                    PatientFullName = p.Person != null
                        ? $"{p.Person.FirstName} {p.Person.SecondName} {p.Person.LastName}"
                        : string.Empty,

                    PhoneNumber = p.Person != null ? p.Person.Phone : null,
                    Email = p.Person != null ? p.Person.Email : null,
                    NationalNumber = p.Person != null ? p.Person.NationalNumber : null,

                    Age = p.Person != null ? DateTime.Now.Year - p.Person.DateOfBirth.Year : 0
                }).ToListAsync();
        }

        /// <summary>
        /// جلب بيانات مريض محدد بواسطة الـ ID وتحويل كائن الـ Entity لـ ViewDTO محمي
        /// </summary>
        public async Task<PatientSaveDTO?> GetPatientByIdAsync(int id)
        {
            if (id <= 0) return null;

            return await _context.Patients
                .AsNoTracking()
                .Where(p => p.PatientId == id)
                .Select(p => new PatientSaveDTO
                {
                    // 1. ملء تفاصيل المريض الطبية (PatientViewDTO)
                    PatientDetails = new PatientViewDTO
                    {
                        PatientId = p.PatientId,
                        PersonId = p.PersonId,
                        EmergencyContact = p.EmergencyContact,
                        EmergencyPhone = p.EmergencyPhone,
                        BloodType = p.BloodType,
                        Allergies = p.Allergies,
                        MedicalHistory = p.MedicalHistory,
                        IsActive = p.IsActive,
                        CreatedDate = p.CreatedDate,

                        PatientFullName = p.Person != null
                            ? $"{p.Person.FirstName} {p.Person.SecondName} {p.Person.LastName}"
                            : string.Empty,

                        PhoneNumber = p.Person != null ? p.Person.Phone : null,
                        Email = p.Person != null ? p.Person.Email : null,
                        NationalNumber = p.Person != null ? p.Person.NationalNumber : null,

                        Age = p.Person != null ? DateTime.Now.Year - p.Person.DateOfBirth.Year : 0
                    },

                    // 2. ملء تفاصيل الشخص الأساسية (PersonSaveDTO) المطلوبة للـ Save/Update
                    Person = p.Person != null ? new PersonSaveDTO
                    {
                        PersonId = p.Person.PersonId,
                        FirstName = p.Person.FirstName,
                        SecondName = p.Person.SecondName,
                        ThirdName = p.Person.ThirdName, // أضفته لو كان موجوداً بنظام الاسم الرباعي لديك
                        LastName = p.Person.LastName,
                        NationalNumber = p.Person.NationalNumber,
                        DateOfBirth = p.Person.DateOfBirth,
                        Gender = p.Person.Gender,
                        Phone = p.Person.Phone,
                        Email = p.Person.Email, 
                        Address = p.Person.Address
                    } : new PersonSaveDTO()
                })
                .FirstOrDefaultAsync();
        }

        // =========================================================================
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة مريض جديد في النظام بعد التحقق من عدم تكرار ارتباط الشخص (Unique Constraint)
        /// </summary>
        public async Task<int> AddNewPatientAsync(PatientSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));
            if (saveDto.Person == null || saveDto.PatientDetails == null)
                throw new ArgumentException("بيانات الشخص أو التفاصيل الطبية للمريض ناقصة!");

            // استخدام Transaction لضمان تراجع التغييرات بالكامل (Rollback) في حال فشل أي خطوة
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int personId = saveDto.Person.PersonId;

                // الحالة الأولى: الشخص جديد تماماً في النظام، نحفظ بياناته أولاً
                if (personId == 0)
                {
                    var personEntity = new Person
                    {
                        FirstName = saveDto.Person.FirstName,
                        SecondName = saveDto.Person.SecondName,
                        ThirdName = saveDto.Person.ThirdName,
                        LastName = saveDto.Person.LastName,
                        NationalNumber = saveDto.Person.NationalNumber,
                        DateOfBirth = saveDto.Person.DateOfBirth,
                        Gender = saveDto.Person.Gender,
                        Phone = saveDto.Person.Phone,
                        Email = saveDto.Person.Email,
                        Address = saveDto.Person.Address
                    };

                    _context.People.Add(personEntity);
                    await _context.SaveChangesAsync(); // توليد الـ PersonId من قاعدة البيانات

                    personId = personEntity.PersonId;
                }
                else
                {
                    // الحالة الثانية: الشخص موجود مسبقاً، نتحقق أنه لم يسجل كمريض من قبل
                    bool isPersonAlreadyPatient = await _context.Patients
                        .AnyAsync(p => p.PersonId == personId);

                    if (isPersonAlreadyPatient)
                        throw new ArgumentException("هذا الشخص مسجل بالفعل كمريض في النظام!");
                }

                // الآن نقوم بإنشاء سجل المريض وربطه بـ personId المحسوم
                var patientEntity = new Patient
                {
                    PersonId = personId,
                    EmergencyContact = saveDto.PatientDetails.EmergencyContact,
                    EmergencyPhone = saveDto.PatientDetails.EmergencyPhone,
                    BloodType = saveDto.PatientDetails.BloodType,
                    Allergies = saveDto.PatientDetails.Allergies,
                    MedicalHistory = saveDto.PatientDetails.MedicalHistory,
                    IsActive = saveDto.PatientDetails.IsActive,
                    CreatedDate = DateTime.Now // إذا لم تكن قاعدة البيانات تولده تلقائياً
                };

                _context.Patients.Add(patientEntity);
                await _context.SaveChangesAsync();

                // تأكيد نجاح العملية بالكامل وحفظها نهائياً
                await transaction.CommitAsync();

                return patientEntity.PatientId;
            }
            catch (Exception)
            {
                // في حال حدوث أي خطأ، يتم التراجع عن كل ما حدث تلقائياً لحماية سلامة البيانات
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// تحديث بيانات مريض قائم بالتتبع الذكي للتعديلات
        /// </summary>
        public async Task<bool> UpdatePatientAsync(PatientSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));
            if (saveDto.Person == null || saveDto.PatientDetails == null)
                throw new ArgumentException("بيانات الشخص أو التفاصيل الطبية للمريض ناقصة!");

            // نتحقق أولاً من وجود المريض في النظام باستخدام المعرف من الـ PatientDetails
            var existingPatient = await _context.Patients
                .Include(p => p.Person) // جلب بيانات الشخص المرتبطة به لتعديلها معاً
                .FirstOrDefaultAsync(p => p.PatientId == saveDto.PatientDetails.PatientId);

            if (existingPatient == null) return false;

            // استخدام Transaction لضمان حفظ تعديل الجدولين معاً أو التراجع في حال حدوث خطأ
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. تحديث البيانات الطبية في جدول الـ Patients
                existingPatient.EmergencyContact = saveDto.PatientDetails.EmergencyContact;
                existingPatient.EmergencyPhone = saveDto.PatientDetails.EmergencyPhone;
                existingPatient.BloodType = saveDto.PatientDetails.BloodType;
                existingPatient.Allergies = saveDto.PatientDetails.Allergies;
                existingPatient.MedicalHistory = saveDto.PatientDetails.MedicalHistory;
                existingPatient.IsActive = saveDto.PatientDetails.IsActive;

                // 2. تحديث البيانات الشخصية في جدول الـ People (إذا كان السجل مرتبطاً بنجاح)
                if (existingPatient.Person != null)
                {
                    existingPatient.Person.FirstName = saveDto.Person.FirstName;
                    existingPatient.Person.SecondName = saveDto.Person.SecondName;
                    existingPatient.Person.ThirdName = saveDto.Person.ThirdName;
                    existingPatient.Person.LastName = saveDto.Person.LastName;
                    existingPatient.Person.NationalNumber = saveDto.Person.NationalNumber;
                    existingPatient.Person.DateOfBirth = saveDto.Person.DateOfBirth;
                    existingPatient.Person.Gender = saveDto.Person.Gender;
                    existingPatient.Person.Phone = saveDto.Person.Phone;
                    existingPatient.Person.Email = saveDto.Person.Email;
                    existingPatient.Person.Address = saveDto.Person.Address;
                }

                // حفظ التغييرات لكلا الجدولين في قاعدة البيانات
                await _context.SaveChangesAsync();

                // تأكيد نجاح التعديلات نهائياً
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                // التراجع عن التعديلات في حال حدوث أي مشكلة طارئة
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// حذف مريض من النظام بواسطة معرفه الرقمي
        /// </summary>
        public async Task<bool> DeletePatientAsync(int id)
        {
            if (id <= 0) return false;

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == id);
            if (patient == null) return false;

            _context.Patients.Remove(patient);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> GetTodaysPatientsCountAsync()
        {
            return await _context.PatientVisits
                .CountAsync(v => v.VisitDate.Date == DateTime.Today);
        }

        /// <summary>
        /// تعديل جزئي على بيانات مريض - يحدث فقط الحقول التي لها قيمة غير null في الـ DTO
        /// أي حقل يُترك null يبقى بقيمته الحالية بدون تغيير
        /// </summary>
        /// <param name="patientId">معرف المريض</param>
        /// <param name="patchDto">الحقول المراد تعديلها فقط</param>
        /// <returns>true لو تم التحديث بنجاح، false لو المريض غير موجود</returns>
        public async Task<bool> PatchPatientAsync(int patientId, PatientPatchDTO patchDto)
        {
            if (patientId <= 0) return false;

            var existingPatient = await _context.Patients
                .Include(p => p.Person)
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (existingPatient == null) return false;

            // ── 1. تحديث بيانات الشخص - فقط الحقول غير الـ null ──
            if (patchDto.FirstName != null) existingPatient.Person.FirstName = patchDto.FirstName;
            if (patchDto.SecondName != null) existingPatient.Person.SecondName = patchDto.SecondName;
            if (patchDto.ThirdName != null) existingPatient.Person.ThirdName = patchDto.ThirdName;
            if (patchDto.LastName != null) existingPatient.Person.LastName = patchDto.LastName;
            if (patchDto.DateOfBirth.HasValue) existingPatient.Person.DateOfBirth = patchDto.DateOfBirth.Value;
            if (patchDto.Gender.HasValue) existingPatient.Person.Gender = patchDto.Gender.Value;
            if (patchDto.Phone != null) existingPatient.Person.Phone = patchDto.Phone;
            if (patchDto.Email != null) existingPatient.Person.Email = patchDto.Email;
            if (patchDto.Address != null) existingPatient.Person.Address = patchDto.Address;
            if (patchDto.NationalNumber != null) existingPatient.Person.NationalNumber = patchDto.NationalNumber;

            // ── 2. تحديث بيانات المريض ──
            if (patchDto.EmergencyContact != null) existingPatient.EmergencyContact = patchDto.EmergencyContact;
            if (patchDto.EmergencyPhone != null) existingPatient.EmergencyPhone = patchDto.EmergencyPhone;
            if (patchDto.BloodType != null) existingPatient.BloodType = patchDto.BloodType;
            if (patchDto.Allergies != null) existingPatient.Allergies = patchDto.Allergies;
            if (patchDto.MedicalHistory != null) existingPatient.MedicalHistory = patchDto.MedicalHistory;
            if (patchDto.IsActive.HasValue) existingPatient.IsActive = patchDto.IsActive.Value;

            return await _context.SaveChangesAsync() > 0;
        }
    }
}