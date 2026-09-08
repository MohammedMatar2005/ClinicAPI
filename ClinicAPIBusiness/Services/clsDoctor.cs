using ClinicAPIBusiness.DTO.DoctorsDTOs;
using ClinicAPIBusiness.DTO.PeopleDTOs;
using ClinicAPIBusiness.DTO.UsersDTOs;
using ClinicAPIBusiness.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.Services
{
    public class clsDoctor
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsDoctor(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projection)
        // =========================================================================

        /// <summary>
        /// جلب قائمة جميع الأطباء مسطحة على مقاس الـ DTO لسرعة نقل البيانات
        /// </summary>
        public async Task<List<DoctorViewDTO>> GetAllDoctorsAsync()
        {
            return await _context.Doctors
                .AsNoTracking()
                .Select(d => new DoctorViewDTO
                {
                    DoctorId = d.DoctorId,
                    Specialization = d.Specialization,
                    LicenseNumber = d.LicenseNumber,
                    OfficeLocation = d.OfficeLocation,
                    ExperienceYears = d.ExperienceYears,
                    IsActive = d.IsActive,
                    UserId = d.UserId,
                    PersonId = d.User.PersonId,
                    NationalNumber = d.User.Person.NationalNumber,
                    DoctorFullName = $"{d.User.Person.FirstName} {d.User.Person.SecondName} {d.User.Person.LastName}",
                    Email = d.User.Person.Email ?? string.Empty,
                    PhoneNumber = d.User.Person.Phone
                }).ToListAsync();
        }

        /// <summary>
        /// جلب طبيب محدد بواسطة الـ ID بالتتبع لغايات البزنس أو التعديل
        /// </summary>
        // ── الدالة المعدلة في السيرفس ──
        public async Task<DoctorSaveDTO?> GetDoctorByIdAsync(int id)
        {
            if (id <= 0) return null;

            return await _context.Doctors
                .Where(d => d.DoctorId == id)
                .Select(d => new DoctorSaveDTO
                {
                    // 1. بيانات الشخص (Person)
                    Person = new PersonSaveDTO
                    {
                        PersonId = d.User.Person.PersonId,
                        FirstName = d.User.Person.FirstName,
                        SecondName = d.User.Person.SecondName ?? string.Empty,
                        ThirdName = d.User.Person.ThirdName ?? string.Empty,
                        LastName = d.User.Person.LastName,
                        DateOfBirth = d.User.Person.DateOfBirth,
                        Gender = d.User.Person.Gender,
                        Phone = d.User.Person.Phone,
                        Email = d.User.Person.Email,
                        NationalNumber = d.User.Person.NationalNumber,
                        Address = d.User.Person.Address
                    },

                    // 2. بيانات حساب المستخدم (User)
                    User = new UserSaveDTO
                    {
                        UserId = d.User.UserId,
                        Username = d.User.Username,
                        IsActive = d.User.IsActive, // تأكد من مطابقة اسم الخاصية في UserSaveDTO (سواء IsActive أو UserIsActive)
                        RoleId = Convert.ToByte(d.User.RoleId)
                        // ملاحظة: كلمة المرور (Password) تترك فارغة هنا لدواعي أمنية ولأنها عملية جلب بيانات وليست حفظ
                    },

                    // 3. البيانات المهنية للطبيب (Doctor Details)
                    DoctorDetails = new DoctorDetailsDTO
                    {
                        DoctorId = d.DoctorId,
                        Specialization = d.Specialization,
                        LicenseNumber = d.LicenseNumber,
                        OfficeLocation = d.OfficeLocation,
                        Salary = d.Salary,
                        ExperienceYears = d.ExperienceYears,
                        IsActive = d.IsActive // تأكد من مطابقة اسم الخاصية في DoctorDetailsDTO
                    }
                })
                .FirstOrDefaultAsync();
        }

        // =========================================================================
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة طبيب جديد في النظام مع الكيانات المرتبطة به (Transaction)
        /// </summary>
        public async Task<int> AddNewDoctorAsync(DoctorSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            // 1. التحقق من عدم تكرار البيانات الفريدة
            bool isUsernameExist = await _context.Users
                .AnyAsync(u => u.Username == saveDto.User.Username);
            if (isUsernameExist)
                throw new ArgumentException("اسم المستخدم هذا محجوز بالفعل، يرجى اختيار اسم آخر.");

            bool isNationalNumberExist = await _context.People
                .AnyAsync(p => p.NationalNumber == saveDto.Person.NationalNumber);
            if (isNationalNumberExist)
                throw new ArgumentException("الرقم الوطني هذا مسجل لطبيب أو مستخدم آخر في النظام!");

            // استخدام Transaction لضمان حفظ البيانات كاملة معاً أو التراجع عند حدوث خطأ
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 2. إنشاء كائن الشخص (Person)
                    var personEntity = new Person
                    {
                        FirstName = saveDto.Person.FirstName,
                        SecondName = saveDto.Person.SecondName,
                        ThirdName = saveDto.Person.ThirdName,
                        LastName = saveDto.Person.LastName,
                        DateOfBirth = saveDto.Person.DateOfBirth,
                        Gender = saveDto.Person.Gender,
                        Phone = saveDto.Person.Phone,
                        Email = saveDto.Person.Email,
                        NationalNumber = saveDto.Person.NationalNumber,
                        Address = saveDto.Person.Address
                    };
                    _context.People.Add(personEntity);
                    await _context.SaveChangesAsync();

                    // 3. إنشاء كائن المستخدم (User) ربطاً بالشخص المضاف
                    var userEntity = new User
                    {
                        PersonId = personEntity.PersonId,
                        Username = saveDto.User.Username,
                        PasswordHash = saveDto.User.PasswordHash,
                        RoleId = saveDto.User.RoleId,
                        IsActive = saveDto.User.IsActive
                    };
                    _context.Users.Add(userEntity);
                    await _context.SaveChangesAsync();

                    // 4. إنشاء كائن الطبيب (Doctor) ربطاً بالمستخدم المضاف
                    var doctorEntity = new Doctor
                    {
                        UserId = userEntity.UserId,
                        Specialization = saveDto.DoctorDetails.Specialization,
                        LicenseNumber = saveDto.DoctorDetails.LicenseNumber,
                        Salary = saveDto.DoctorDetails.Salary,
                        OfficeLocation = saveDto.DoctorDetails.OfficeLocation,
                        ExperienceYears = saveDto.DoctorDetails.ExperienceYears,
                        IsActive = saveDto.DoctorDetails.IsActive
                    };
                    _context.Doctors.Add(doctorEntity);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    return doctorEntity.DoctorId;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        /// <summary>
        /// تحديث بيانات طبيب قائم بالتتبع الذكي للحقول المعدلة فقط
        /// </summary>
        public async Task<bool> UpdateDoctorAsync(DoctorSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var existingDoctor = await _context.Doctors
                .Include(d => d.User)
                .ThenInclude(u => u.Person)
                .FirstOrDefaultAsync(d => d.DoctorId == saveDto.DoctorDetails.DoctorId);

            if (existingDoctor == null) return false;

            // ── 1. تحديث بيانات الشخص ──
            existingDoctor.User.Person.FirstName = saveDto.Person.FirstName;
            existingDoctor.User.Person.SecondName = saveDto.Person.SecondName;
            existingDoctor.User.Person.ThirdName = saveDto.Person.ThirdName;
            existingDoctor.User.Person.LastName = saveDto.Person.LastName;
            existingDoctor.User.Person.DateOfBirth = saveDto.Person.DateOfBirth;
            existingDoctor.User.Person.Gender = saveDto.Person.Gender;
            existingDoctor.User.Person.Phone = saveDto.Person.Phone;
            existingDoctor.User.Person.Email = saveDto.Person.Email;
            existingDoctor.User.Person.NationalNumber = saveDto.Person.NationalNumber;
            existingDoctor.User.Person.Address = saveDto.Person.Address;

            // ── 2. تحديث بيانات المستخدم ──
            existingDoctor.User.Username = saveDto.User.Username;

            // 🔑 الشاهد هنا: نحدث كلمة المرور فقط إذا تم إرسال كلمة جديدة في الـ DTO
            if (!string.IsNullOrWhiteSpace(saveDto.User.PasswordHash))
            {
                existingDoctor.User.PasswordHash = saveDto.User.PasswordHash;
            }

            existingDoctor.User.RoleId = saveDto.User.RoleId;
            existingDoctor.User.IsActive = saveDto.User.IsActive;

            // ── 3. تحديث بيانات الطبيب ──
            existingDoctor.Specialization = saveDto.DoctorDetails.Specialization;
            existingDoctor.LicenseNumber = saveDto.DoctorDetails.LicenseNumber;
            existingDoctor.Salary = saveDto.DoctorDetails.Salary;
            existingDoctor.OfficeLocation = saveDto.DoctorDetails.OfficeLocation;
            existingDoctor.ExperienceYears = saveDto.DoctorDetails.ExperienceYears;
            existingDoctor.IsActive = saveDto.DoctorDetails.IsActive;

            return await _context.SaveChangesAsync() > 0;
        }
        /// <summary>
        /// حذف طبيب من النظام بواسطة المعرف الرقمي الخاص به
        /// </summary>
        public async Task<bool> DeleteDoctorAsync(int id)
        {
            if (id <= 0) return false;

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == id);
            if (doctor == null) return false;

            _context.Doctors.Remove(doctor);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsDoctorExistByIdAsync(int selectedDoctorId)
        {
            if (selectedDoctorId <= 0) return false;

            return await _context.Doctors.AnyAsync(d => d.DoctorId == selectedDoctorId);
        }

        public async Task<DoctorViewDTO?> GetDoctorByUserIdAsync(int userId)
        {
            if (userId <= 0) return null;

            return await _context.Doctors
                .AsNoTracking() // لتحسين الأداء طالما العملية قراءة وعرض فقط
                .Where(d => d.UserId == userId)
                .Select(d => new DoctorViewDTO
                {
                    DoctorId = d.DoctorId,
                    PersonId = d.User.PersonId, // أو d.PersonId حسب العلاقات في قاعدة بياناتك
                    Specialization = d.Specialization,
                    LicenseNumber = d.LicenseNumber,
                    OfficeLocation = d.OfficeLocation,
                    ExperienceYears = d.ExperienceYears,
                    IsActive = d.IsActive,

                    // بيانات المستخدم والاسم المسطحة عبر الـ Navigation Properties
                    UserId = d.UserId,

                    // دمج الاسم الثلاثي أو الكامل مع حماية الـ Nullability
                    DoctorFullName = d.User.Person != null
                        ? $"{d.User.Person.FirstName} {d.User.Person.SecondName} {d.User.Person.LastName}".Trim()
                        : string.Empty,

                    Email = d.User.Person != null ? d.User.Person.Email : string.Empty,
                    PhoneNumber = d.User.Person != null ? d.User.Person.Phone: string.Empty,
                    NationalNumber = d.User.Person != null ? d.User.Person.NationalNumber : string.Empty
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// تعديل جزئي على بيانات طبيب - يحدث فقط الحقول التي لها قيمة غير null في الـ DTO
        /// أي حقل يُترك null يبقى بقيمته الحالية بدون تغيير
        /// </summary>
        /// <param name="doctorId">معرف الطبيب</param>
        /// <param name="patchDto">الحقول المراد تعديلها فقط</param>
        /// <returns>true لو تم التحديث بنجاح، false لو الطبيب غير موجود</returns>
        public async Task<bool> PatchDoctorAsync(int doctorId, DoctorPatchDTO patchDto)
        {
            if (doctorId <= 0) return false;

            var existingDoctor = await _context.Doctors
                .Include(d => d.User)
                    .ThenInclude(u => u.Person)
                .FirstOrDefaultAsync(d => d.DoctorId == doctorId);

            if (existingDoctor == null) return false;

            // ── 1. تحديث بيانات الشخص - فقط الحقول غير الـ null ──
            if (patchDto.FirstName != null) existingDoctor.User.Person.FirstName = patchDto.FirstName;
            if (patchDto.SecondName != null) existingDoctor.User.Person.SecondName = patchDto.SecondName;
            if (patchDto.ThirdName != null) existingDoctor.User.Person.ThirdName = patchDto.ThirdName;
            if (patchDto.LastName != null) existingDoctor.User.Person.LastName = patchDto.LastName;
            if (patchDto.DateOfBirth.HasValue) existingDoctor.User.Person.DateOfBirth = patchDto.DateOfBirth.Value;
            if (patchDto.Gender.HasValue) existingDoctor.User.Person.Gender = patchDto.Gender.Value;
            if (patchDto.Phone != null) existingDoctor.User.Person.Phone = patchDto.Phone;
            if (patchDto.Email != null) existingDoctor.User.Person.Email = patchDto.Email;
            if (patchDto.Address != null) existingDoctor.User.Person.Address = patchDto.Address;
            if (patchDto.NationalNumber != null) existingDoctor.User.Person.NationalNumber = patchDto.NationalNumber;

            // ── 2. تحديث بيانات المستخدم ──
            if (patchDto.Username != null) existingDoctor.User.Username = patchDto.Username;

            if (!string.IsNullOrWhiteSpace(patchDto.NewPassword))
                existingDoctor.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(patchDto.NewPassword);

            if (patchDto.RoleId.HasValue) existingDoctor.User.RoleId = patchDto.RoleId.Value;
            if (patchDto.IsUserActive.HasValue) existingDoctor.User.IsActive = patchDto.IsUserActive.Value;

            // ── 3. تحديث بيانات الطبيب ──
            if (patchDto.Specialization != null) existingDoctor.Specialization = patchDto.Specialization;
            if (patchDto.LicenseNumber != null) existingDoctor.LicenseNumber = patchDto.LicenseNumber;
            if (patchDto.Salary.HasValue) existingDoctor.Salary = patchDto.Salary.Value;
            if (patchDto.OfficeLocation != null) existingDoctor.OfficeLocation = patchDto.OfficeLocation;
            if (patchDto.ExperienceYears.HasValue) existingDoctor.ExperienceYears = patchDto.ExperienceYears.Value;
            if (patchDto.IsDoctorActive.HasValue) existingDoctor.IsActive = patchDto.IsDoctorActive.Value;

            return await _context.SaveChangesAsync() > 0;
        }
    }
}