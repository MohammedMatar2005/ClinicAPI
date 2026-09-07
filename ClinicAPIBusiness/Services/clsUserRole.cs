using ClinicAPIBusiness.DTO.UserRoleDTOs;
using ClinicAPIBusiness.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.Services
{
    public class clsUserRole
    {
        private readonly ClinicManagementSystemContext _context;

        // حقن الـ DbContext مباشرة عبر الـ Constructor
        public clsUserRole(ClinicManagementSystemContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // 1. القراءة والعرض والـ Projection (Queries)
        // =========================================================================

        /// <summary>
        /// جلب جميع الأدوار لعرضها في جداول الصلاحيات أو الـ ComboBox
        /// </summary>
        public async Task<List<UserRoleDTO>> GetAllUserRolesAsync()
        {
            return await _context.UserRoles
                .AsNoTracking()
                .Select(r => new UserRoleDTO
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description
                }).ToListAsync();
        }

        /// <summary>
        /// جلب تفاصيل دور معين بواسطة الـ ID
        /// </summary>
        public async Task<UserRoleDTO?> GetRoleByIdAsync(int roleId)
        {
            if (roleId <= 0) return null;

            return await _context.UserRoles
                .AsNoTracking()
                .Select(r => new UserRoleDTO
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description
                }).FirstOrDefaultAsync(r => r.RoleId == roleId);
        }

        // =========================================================================
        // 2. عمليات الحفظ والتحقق البزنسي (CUD Operations)
        // =========================================================================

        /// <summary>
        /// إضافة دور وظيفي جديد مع فحص حماية لمنع تكرار اسم الدور
        /// </summary>
        public async Task<int> AddNewUserRoleAsync(UserRoleDTO roleDto)
        {
            if (roleDto == null) throw new ArgumentNullException(nameof(roleDto));

            bool isRoleExist = await _context.UserRoles
                .AnyAsync(r => r.RoleName == roleDto.RoleName);

            if (isRoleExist)
                throw new ArgumentException("اسم الدور الوظيفي هذا مسجل بالفعل في النظام!");

            var roleEntity = new UserRole
            {
                RoleName = roleDto.RoleName,
                Description = roleDto.Description
            };

            _context.UserRoles.Add(roleEntity);
            await _context.SaveChangesAsync();

            return roleEntity.RoleId;
        }

        /// <summary>
        /// تحديث بيانات صلاحية أو دور قائم
        /// </summary>
        public async Task<bool> UpdateUserRoleAsync(UserRoleDTO roleDto)
        {
            if (roleDto == null) throw new ArgumentNullException(nameof(roleDto));

            var existingRole = await _context.UserRoles
                .FirstOrDefaultAsync(r => r.RoleId == roleDto.RoleId);

            if (existingRole == null) return false;

            if (existingRole.RoleName.ToLower() != roleDto.RoleName.ToLower())
            {
                bool isRoleExist = await _context.UserRoles
                    .AnyAsync(r => r.RoleName == roleDto.RoleName);

                if (isRoleExist)
                    throw new ArgumentException("لا يمكن التحديث، اسم الدور الجديد محجوز لدور آخر!");
            }

            existingRole.RoleName = roleDto.RoleName;
            existingRole.Description = roleDto.Description;

            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// حذف دور وظيفي من النظام
        /// </summary>
        public async Task<bool> DeleteUserRoleAsync(int roleId)
        {
            if (roleId <= 0) return false;

            var role = await _context.UserRoles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null) return false;

            _context.UserRoles.Remove(role);
            return await _context.SaveChangesAsync() > 0;
        }

        // =========================================================================
        // 3. دوال التحقق السريع (Existence Checks)
        // =========================================================================

        public async Task<bool> IsRoleExistByIdAsync(int roleId)
        {
            if (roleId <= 0) return false;

            return await _context.UserRoles
                .AnyAsync(r => r.RoleId == roleId);
        }
    }
}