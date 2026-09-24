using ClinicAPIBusiness.DTO.UsersDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/Users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly clsUser _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(clsUser userService, ILogger<UserController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب قائمة جميع المستخدمين النشطين في النظام
        /// </summary>
        [HttpGet("GetAll", Name = "GetAllUsers")]
        [ProducesResponseType(typeof(List<UserViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserViewDTO>>> GetAllUsers()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "Audit: Admin action executed. AdminId={AdminId}, Action={Action}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "GetAllUsers", "UserList", ipAddress);

            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// جلب بيانات مستخدم بناءً على معرفه
        /// </summary>
        [Authorize]
        [HttpGet("GetById/{userId:int}", Name = "GetUserById")]
        [ProducesResponseType(typeof(UserViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UserViewDTO>> GetUserById(
         int userId,
         [FromServices] IAuthorizationService authorizationService)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (userId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Get user failed (Invalid ID). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    currentUserId, userId, "GetUserById", ipAddress);
                return BadRequest("Invalid user ID.");
            }

            // التحقق عبر الـ Policy (ClinicOwnerOrAdmin)
            var authResult = await authorizationService.AuthorizeAsync(
                User,
                userId,
                "ClinicOwnerOrAdmin");

            if (!authResult.Succeeded)
            {
                _logger.LogWarning(
                    "Audit: Get user failed (Forbidden/Unauthorized access). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    currentUserId, userId, "GetUserById", ipAddress);
                return Forbid(); // 403 Forbidden
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning(
                    "Audit: Get user failed (Not Found). UserId={UserId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    currentUserId, userId, "GetUserById", ipAddress);
                return NotFound();
            }

            return Ok(user);
        }

        /// <summary>
        /// إنشاء مستخدم جديد في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("Create", Name = "CreateUser")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<int>> CreateUser([FromBody] UserSaveDTO userSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (userSaveDto == null)
            {
                _logger.LogWarning(
                    "Audit: Create user failed (Data is null). AdminId={AdminId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, "CreateUser", ipAddress);
                return BadRequest("User data is required.");
            }

            try
            {
                int newUserId = await _userService.AddNewUserAsync(userSaveDto);

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "CreateUser", newUserId, "User", ipAddress);

                return CreatedAtRoute("GetUserById", new { userId = newUserId }, newUserId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Create user failed (Validation Error). AdminId={AdminId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, "CreateUser", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// تحديث بيانات مستخدم في النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{userId:int}", Name = "UpdateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserSaveDTO userSaveDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (userSaveDto == null)
            {
                return BadRequest("User data is required.");
            }

            // تأكيد مطابقة الـ ID بالـ Route مع البيانات داخل الـ DTO
            if (userId != userSaveDto.UserId)
            {
                _logger.LogWarning(
                    "Audit: Update user failed (ID Mismatch). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, userId, "UpdateUser", ipAddress);
                return BadRequest("Mismatched user id between route and body.");
            }

            try
            {
                bool isUpdated = await _userService.UpdateUserAsync(userSaveDto);

                if (!isUpdated)
                {
                    _logger.LogWarning(
                        "Audit: Update user failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, userId, "UpdateUser", ipAddress);
                    return NotFound($"No user found with id {userId}.");
                }

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "UpdateUser", userId, "User", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Update user failed (Validation Error). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, userId, "UpdateUser", ex.Message, ipAddress);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف مستخدم من النظام
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{userId:int}", Name = "DeleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (userId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Delete user failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, userId, "DeleteUser", ipAddress);
                return BadRequest("Invalid user ID.");
            }

            bool isDeleted = await _userService.DeleteUserAsync(userId);

            if (!isDeleted)
            {
                _logger.LogWarning(
                    "Audit: Delete user failed (Not Found). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, userId, "DeleteUser", ipAddress);
                return NotFound($"User with ID {userId} not found.");
            }

            _logger.LogInformation(
                "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                adminId, "DeleteUser", userId, "User", ipAddress);

            return NoContent();
        }

        /// <summary>
        /// تغيير كلمة مرور مستخدم بعد التحقق من كلمة المرور الحالية
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{userId:int}/change-password", Name = "ChangePassword")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordDTO dto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (userId <= 0)
            {
                _logger.LogWarning(
                    "Audit: Change password failed (Invalid ID). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, userId, "ChangePassword", ipAddress);
                return BadRequest("Invalid user ID.");
            }

            if (dto == null)
            {
                _logger.LogWarning(
                    "Audit: Change password failed (Data is null). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                    adminId, userId, "ChangePassword", ipAddress);
                return BadRequest("Password data is required.");
            }

            try
            {
                bool isChanged = await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);

                if (!isChanged)
                {
                    _logger.LogWarning(
                        "Audit: Change password failed (Incorrect Old Password). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, IpAddress={IpAddress}",
                        adminId, userId, "ChangePassword", ipAddress);
                    return BadRequest("Old password is incorrect.");
                }

                _logger.LogInformation(
                    "Audit: Admin action executed successfully. AdminId={AdminId}, Action={Action}, TargetId={TargetId}, TargetType={TargetType}, IpAddress={IpAddress}",
                    adminId, "ChangePassword", userId, "User", ipAddress);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Audit: Change password failed (Not Found / Error). AdminId={AdminId}, TargetId={TargetId}, Action={Action}, ErrorMessage={Message}, IpAddress={IpAddress}",
                    adminId, userId, "ChangePassword", ex.Message, ipAddress);
                return NotFound(ex.Message);
            }
        }
    }
}