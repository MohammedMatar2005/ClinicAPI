using ClinicAPIBusiness.DTO.UsersDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly clsUser _userService;
        private readonly clsSecurity _securityService;

        public AuthController(clsUser userService, clsSecurity securityService)
        {
            _userService = userService;
            _securityService = securityService;
        }


        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDTO request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var user = await _userService.GetUserByUsernameAsync(request.Username);

            // 1. حماية من كشف وجود اسم المستخدم وتوحيد الرد بـ Unauthorized
            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            
            if (!user.IsActive)
            {
                return Unauthorized("Your account is deactivated. Please contact the administrator.");
            }

            bool isValidPassword = false;

            try
            {
                // 2. المقارنة الصحيحة باستخدام BCrypt.Verify
                isValidPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // 3. معالجة حالة الحقول غير المشفرة قديمة بالداتابيز لتفادي انهيار API
                if (user.PasswordHash == request.Password)
                {
                    isValidPassword = true;
                    // تحديث السجل فوراً ليكون مشفراً في المرات القادمة
                    await _userService.UpdatePasswordToHashAsync(user.UserId, request.Password);
                }
            }

            if (!isValidPassword)
            {
                return Unauthorized("Invalid username or password.");
            }

            // 4. توليد التوكين (عملية Synch في الـ Memory غالباً بدون await)
            var token = await _securityService.GenerateAccessToken(user.UserId, user.Username, user.RoleName);

            return Ok(new { Token = token });
        }



    }
}
