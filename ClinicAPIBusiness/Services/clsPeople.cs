using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.DTO.PeopleDTOs;

namespace ClinicAPIBusiness.Services
{
    public class clsPeople
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsPeople(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض (Queries & Projections)
        // =========================================================================

        /// <summary>
        /// جلب جميع الأشخاص مسطحين ومجهزين للعرض السريع في جداول الـ UI
        /// </summary>
        public async Task<List<PersonViewDTO>> GetAllPeopleAsync()
        {
            return await _context.People
                .AsNoTracking()
                .Select(p => new PersonViewDTO
                {
                    PersonId = p.PersonId,
                    NationalNumber = p.NationalNumber,
                    FullName = $"{p.FirstName} {p.SecondName ?? string.Empty} {p.ThirdName ?? string.Empty} {p.LastName}".Replace("  ", " ").Trim(),
                    Gender = p.Gender == true ? "Male" : "Female",
                    DateOfBirth = p.DateOfBirth,
                    Phone = p.Phone ?? string.Empty,
                    Email = p.Email
                }).ToListAsync();
        }

        /// <summary>
        /// جلب التفاصيل الكاملة لشخص محدد بواسطة المعرف الرقمي ID
        /// </summary>
        public async Task<PersonViewDTO?> GetPersonByIdAsync(int personId)
        {
            if (personId <= 0) return null;

            return await _context.People
                .AsNoTracking()
                .Where(p => p.PersonId == personId) // 👈 تصفية السجلات أولاً أفضل للأداء قبل الـ Select
                .Select(p => new PersonViewDTO
                {
                    PersonId = p.PersonId,
                    NationalNumber = p.NationalNumber,

                    // دمج الأسماء الأربعة بطريقة مرنة تتفادى المسافات الزائدة في حال كانت الأسماء الوسطى فارغة
                    FullName = (p.FirstName + " " +
                                (p.SecondName ?? "") + " " +
                                (p.ThirdName ?? "") + " " +
                                p.LastName).Trim().Replace("  ", " "),

                    // تحويل قيمة الجنس البولينية إلى نص يتوافق مع الـ DTO
                    Gender = p.Gender == true ? "ذكر" : "أنثى",

                    DateOfBirth = p.DateOfBirth,
                    Phone = p.Phone ?? string.Empty,
                    Email = p.Email,
                    Address = p.Address ?? string.Empty

                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// جلب تفاصيل شخص بواسطة الرقم القومي
        /// </summary>
        public async Task<PersonSaveDTO?> GetPersonByNationalNumberAsync(string nationalNumber)
        {
            if (string.IsNullOrWhiteSpace(nationalNumber)) return null;

            return await _context.People
                .AsNoTracking()
                .Select(p => new PersonSaveDTO
                {
                    PersonId = p.PersonId,
                    NationalNumber = p.NationalNumber,
                    FirstName = p.FirstName,
                    SecondName = p.SecondName,
                    ThirdName = p.ThirdName,
                    LastName = p.LastName,
                    Gender = p.Gender == true ? true : false,
                    DateOfBirth = p.DateOfBirth,
                    Phone = p.Phone ?? string.Empty,
                    Email = p.Email,
                    Address = p.Address
                }).FirstOrDefaultAsync(p => p.NationalNumber == nationalNumber);
        }

        // =========================================================================
        // 2. العمليات الأساسية (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة شخص جديد بعد التحقق الصارم من عدم تكرار الرقم القومي في النظام
        /// </summary>
        public async Task<int> AddNewPersonAsync(PersonSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            bool isNationalNumberExist = await _context.People
                .AnyAsync(p => p.NationalNumber == saveDto.NationalNumber);

            if (isNationalNumberExist)
                throw new ArgumentException("الرقم القومي هذا مسجل لشخص آخر بالفعل في النظام!");

            var personEntity = new Person
            {
                NationalNumber = saveDto.NationalNumber,
                FirstName = saveDto.FirstName,
                SecondName = saveDto.SecondName,
                ThirdName = saveDto.ThirdName,
                LastName = saveDto.LastName,
                Gender = saveDto.Gender == true,
                DateOfBirth = saveDto.DateOfBirth,
                Phone = saveDto.Phone,
                Email = saveDto.Email,
                Address = saveDto.Address
            };

            _context.People.Add(personEntity);
            await _context.SaveChangesAsync();

            return personEntity.PersonId;
        }

        /// <summary>
        /// تحديث بيانات شخص قائمة مع حماية الرقم القومي من التداخل مع أشخاص آخرين
        /// </summary>
        public async Task<bool> UpdatePersonAsync(PersonSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var existingPerson = await _context.People
                .FirstOrDefaultAsync(p => p.PersonId == saveDto.PersonId);

            if (existingPerson == null) return false;

            if (existingPerson.NationalNumber != saveDto.NationalNumber)
            {
                bool isNationalNumberExist = await _context.People
                    .AnyAsync(p => p.NationalNumber == saveDto.NationalNumber);

                if (isNationalNumberExist)
                    throw new ArgumentException("لا يمكن التحديث، الرقم القومي الجديد مستخدم بالفعل من قبل شخص آخر!");
            }

            existingPerson.NationalNumber = saveDto.NationalNumber;
            existingPerson.FirstName = saveDto.FirstName;
            existingPerson.SecondName = saveDto.SecondName;
            existingPerson.ThirdName = saveDto.ThirdName;
            existingPerson.LastName = saveDto.LastName;
            existingPerson.Gender = saveDto.Gender == true;
            existingPerson.DateOfBirth = saveDto.DateOfBirth;
            existingPerson.Phone = saveDto.Phone;
            existingPerson.Email = saveDto.Email;
            existingPerson.Address = saveDto.Address;

            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// حذف شخص من النظام بالكامل بواسطة الـ ID
        /// </summary>
        public async Task<bool> DeletePersonAsync(int personId)
        {
            if (personId <= 0) return false;

            var person = await _context.People.FirstOrDefaultAsync(p => p.PersonId == personId);
            if (person == null) return false;

            _context.People.Remove(person);
            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================================================
        // 3. دوال التحقق السريع (Existence Checks)
        // =========================================================================

        public async Task<bool> IsPersonExistByIdAsync(int personId)
        {
            if (personId <= 0) return false;

            return await _context.People
                .AnyAsync(p => p.PersonId == personId);
        }

        public async Task<bool> IsPersonExistByNationalNumberAsync(string nationalNumber)
        {
            if (string.IsNullOrWhiteSpace(nationalNumber)) return false;

            return await _context.People
                .AnyAsync(p => p.NationalNumber == nationalNumber);
        }
    }
}