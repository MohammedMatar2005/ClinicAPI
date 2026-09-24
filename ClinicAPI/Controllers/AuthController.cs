using Azure.Core;
using ClinicAPIBusiness.DTO.Auth;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StudentApi.DTOs.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;


namespace ClinicAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly clsUser _userService;
        private readonly clsSecurity _securityService;

        private readonly ILogger<AuthController> _logger;

        public AuthController(clsUser userService, clsSecurity securityService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _securityService = securityService;
            _logger = logger;
        }


        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [EnableRateLimiting("AuthLimiter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDTO request)
        {

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();


            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt failed: Username or password is missing. IP: {IPAddress}", ipAddress);
                return BadRequest("Username and password are required.");
            }

            var user = await _userService.GetUserByUsernameAsync(request.Username);

            // 1. حماية من كشف وجود اسم المستخدم وتوحيد الرد بـ Unauthorized
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: Invalid username or password for username {Username}. IP: {IPAddress}",
                    request.Username, ipAddress);

                return Unauthorized("Invalid username or password.");
            }

            
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt failed: User account is deactivated. IP: {IPAddress}", ipAddress);
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
                _logger.LogWarning("Login attempt failed: Invalid Username or password for user {Username}. IP: {IPAddress}",
                    request.Username, ipAddress);
                return Unauthorized("Invalid username or password.");
            }

            // 4. توليد التوكين (عملية Synch في الـ Memory غالباً بدون await)
            var AccessToken = await _securityService.GenerateAccessToken(user.UserId, user.Username, user.RoleName);

            var refreshToken = GenerateRefreshToken();

            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7); // Set refresh AccessToken expiration to 7 days
            user.RefreshTokenRevokedAt = null; // Reset revoked time on new login

            await _userService.UpdateUserRefreshTokenAsync(user);

            // 6. الإرجاع باستخدام الـ DTO المخصص
            return Ok(new TokenResponseDTO
            {
                AccessToken = AccessToken,
                RefreshToken = refreshToken
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        [EnableRateLimiting("AuthLimiter")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshAsync([FromBody] ClinicAPIBusiness.DTO.Auth.RefreshRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var user = await _userService.GetUserByUsernameAsync(request.username);

            if (user == null)
            {
                _logger.LogWarning("Refresh attempt failed: User not found with username {Username}. IP: {IPAddress}", request.username, ipAddress);
                return Unauthorized("Error: User not found with this username."); // لتعرف هل المشكلة في اسم المستخدم؟
            }

            if (user.RefreshTokenRevokedAt != null)
            {
                _logger.LogWarning("Refresh attempt failed: Refresh Token is revoked for user {Username}. IP: {IPAddress}", request.username, ipAddress);
                return Unauthorized("Error: Refresh Token is revoked."); // لتعرف هل تم إبطاله؟
            }

            if (string.IsNullOrEmpty(user.RefreshTokenHash) || string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Refresh attempt failed: Missing refresh token or stored hash for user {Username}. IP: {IPAddress}", request.username, ipAddress);
                return Unauthorized("Error: Refresh token or stored hash is missing/null.");
            }

            // 1. افحص مطابقة الـ BCrypt أولاً
            bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash);
            if (!refreshValid)
            {
                _logger.LogWarning("Refresh attempt failed: Invalid Hash/Token match for user {Username}. IP: {IPAddress}", request.username, ipAddress);
                return Unauthorized("Error: Invalid Hash/Token match (BCrypt failed).");
            }

            // 2. ثم افحص انتهاء الوقت
            if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh attempt failed: Refresh Token expired for user {Username}. IP: {IPAddress}", request.username, ipAddress);
                return Unauthorized("Error: Refresh Token expired.");
            }
            // 3. إصدار Access Token جديد (باستخدام نفس خدمة الحماية لضمان تطابق الإعدادات)
            var AccessToken = await _securityService.GenerateAccessToken(user.UserId, user.Username, user.RoleName);

            // 4. تدوير (Rotation) الـ Refresh Token بإنشاء واحد جديد وتحديثه
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            user.RefreshTokenRevokedAt = null;

            // 5. حفظ التعديلات الجديدة في قاعدة البيانات (تم التصحيح من student إلى user واستدعاء دالة التحديث)
            await _userService.UpdateUserRefreshTokenAsync(user);

            // 6. الإرجاع بالـ DTO الخاص بالتوكن
            return Ok(new TokenResponseDTO
            {
                AccessToken = AccessToken,
                RefreshToken = newRefreshToken
            });
        }


        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("Username and refresh token are required.");
            }

            var user = await _userService.GetUserByUsernameAsync(request.Username);
            if (user == null)
                return Ok(new { message = "Logged out successfully" }); // لأسباب أمنية لا تفصح عما إذا كان المستخدم موجوداً أم لا

            bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash);
            if (!refreshValid)
                return Ok(new { message = "Logged out successfully" });

            // 1. تسجيل وقت الإبطال
            user.RefreshTokenRevokedAt = DateTime.UtcNow;

            // 2. [مهم جداً] حفظ التعديل في قاعدة البيانات حتى يتم إبطال التوكن فعلياً
            await _userService.UpdateUserRefreshTokenAsync(user);

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
