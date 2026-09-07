using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.DTO.UsersDTOs;
using BCrypt.Net;

namespace ClinicAPIBusiness.Services
{
    public class clsUser
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsUser(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض والـ Projection
        // =========================================================================

        public async Task<List<UserViewDTO>> GetAllUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .Select(u => new UserViewDTO
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.Person != null
                        ? $"{u.Person.FirstName} {u.Person.SecondName ?? string.Empty} {u.Person.ThirdName ?? string.Empty} {u.Person.LastName}".Replace("   ", " ").Replace("  ", " ").Trim()
                        : string.Empty,
                    RoleName = u.Role != null ? u.Role.RoleName : "No Role",
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate
                }).ToListAsync();
        }

        public async Task<UserViewDTO?> GetUserByIdAsync(int userId)
        {
            if (userId <= 0) return null;

            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new UserViewDTO
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.Person.FullName, // أو u.Person.FirstName + " " + u.Person.LastName حسب خصائص الكائن لديك
                    RoleName = u.Role.RoleName,   // اسحب اسم الدور صراحة من جدول Roles
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate
                })
                .FirstOrDefaultAsync();
        }

        // =========================================================================
        // 2. عمليات الحفظ والتحقق (CUD Operations)
        // =========================================================================

        public async Task<int> AddNewUserAsync(UserSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            bool isUsernameExist = await _context.Users.AnyAsync(u => u.Username == saveDto.Username);
            if (isUsernameExist)
                throw new ArgumentException("اسم المستخدم هذا محجوز مسبقاً، اختر اسماً آخر!");

            bool isPersonExist = await _context.Users.AnyAsync(u => u.PersonId == saveDto.PersonId);
            if (isPersonExist)
                throw new ArgumentException("هذا الشخص لديه حساب مستخدم بالفعل في النظام!");

            var userEntity = new User
            {
                PersonId = saveDto.PersonId,
                Username = saveDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(saveDto.PasswordHash),
                RoleId = saveDto.RoleId,
                IsActive = saveDto.IsActive,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync();

            return userEntity.UserId;
        }

        public async Task<bool> UpdateUserAsync(UserSaveDTO saveDto)
        {
            if (saveDto == null) throw new ArgumentNullException(nameof(saveDto));

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == saveDto.UserId);

            if (existingUser == null) return false;

            if (existingUser.Username != saveDto.Username)
            {
                bool isUsernameExist = await _context.Users.AnyAsync(u => u.Username == saveDto.Username);
                if (isUsernameExist)
                    throw new ArgumentException("اسم المستخدم الجديد مستخدم بالفعل من قبل شخص آخر!");
            }

            existingUser.Username = saveDto.Username;
            existingUser.RoleId = saveDto.RoleId;
            existingUser.IsActive = saveDto.IsActive;

            if (!string.IsNullOrWhiteSpace(saveDto.PasswordHash))
            {
                existingUser.PasswordHash = saveDto.PasswordHash;
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateUserAsync(UserDetailsDTO userDto)
        {
            if (userDto == null) throw new ArgumentNullException(nameof(userDto));

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userDto.UserId);

            if (existingUser == null) return false;

            existingUser.LastLoginDate = userDto.LastLoginDate;
            existingUser.IsActive = userDto.IsActive;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            if (userId <= 0) return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            // لو المستخدم أصلاً معطّل، لا داعي لعملية Save إضافية
            if (!user.IsActive) return true;

            user.IsActive = false;

            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================================================
        // 3. تسجيل الدخول والتحقق والتجديد (Authentication Logic)
        // =========================================================================

        public async Task<UserViewDTO?> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var u = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Person)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (u == null || !u.IsActive)
                return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, u.PasswordHash);
            if (!isPasswordValid)
                return null;

            // تحديث تاريخ آخر تسجيل دخول
            u.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // 🛠️ التعديل هنا: بناء وإرجاع الـ UserViewDTO
            return new UserViewDTO
            {
                UserId = u.UserId,
                Username = u.Username,
                // جلب الاسم الكامل من جدول الأشخاص المرتبط (تأكد من تسمية الحقل لديك بـ Name أو FullName في كائن Person)
                FullName = u.Person?.FullName ?? string.Empty,
                // تسطيح الكائن وجلب اسم الصلاحية صراحةً
                RoleName = u.Role?.RoleName ?? string.Empty,
                IsActive = u.IsActive,
                CreatedDate = u.CreatedDate,
                LastLoginDate = u.LastLoginDate
            };
        }

        public async Task<bool> IsUserExistByPersonIdAsync(int personId)
        {
            if (personId <= 0) return false;

            return await _context.Users.AnyAsync(u => u.PersonId == personId);
        }

        public async Task<UserDetailsDTO?> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            return await _context.Users
                .AsNoTracking()
                .Select(u => new UserDetailsDTO
                {
                    UserId = u.UserId,
                    PersonId = u.PersonId,
                    Username = u.Username,
                    PasswordHash = u.PasswordHash,
                    RoleId = u.RoleId,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate,
                    FullName = u.Person != null
                        ? $"{u.Person.FirstName} {u.Person.SecondName ?? string.Empty} {u.Person.ThirdName ?? string.Empty} {u.Person.LastName}".Replace("   ", " ").Replace("  ", " ").Trim()
                        : string.Empty
                }).FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            // 1. جلب المستخدم من قاعدة البيانات
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            // 2. التحقق من صحة كلمة المرور القديمة
            bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);
            if (!isOldPasswordValid) return false;

            // 3. تشفير كلمة المرور الجديدة وحفظها
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
           

            await _context.SaveChangesAsync();
            return true;
        }
    }
}