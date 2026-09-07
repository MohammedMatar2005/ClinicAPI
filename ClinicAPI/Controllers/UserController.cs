using ClinicAPIBusiness.DTO.UsersDTOs;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Route("api/Users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly clsUser _userService;

        public UserController(clsUser userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        /// <summary>
        /// جلب قائمة جميع المستخدمين النشطين في النظام
        /// </summary>
        /// <returns>قائمة بجميع المستخدمين الفعّالين</returns>
        [HttpGet("GetAll", Name = "GetAllUsers")]
        [ProducesResponseType(typeof(List<UserViewDTO>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserViewDTO>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// جلب بيانات مستخدم بناءً على معرفه
        /// </summary>
        /// <param name="userId">معرف المستخدم</param>
        /// <returns>بيانات المستخدم</returns>
        [HttpGet("GetById/{userId:int}", Name = "GetUserById")]
        [ProducesResponseType(typeof(UserViewDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserViewDTO>> GetUserById(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID.");
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        /// <summary>
        /// إنشاء مستخدم جديد في النظام
        /// </summary>
        /// <param name="userSaveDto">بيانات المستخدم الجديد</param>
        /// <returns>معرف المستخدم الجديد</returns>
        [HttpPost("Create", Name = "CreateUser")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<int>> CreateUser([FromBody] UserSaveDTO userSaveDto)
        {
            // مع [ApiController]، الـ Model Binding والـ Data Annotations بتترفض تلقائياً
            // قبل ما توصل هون لو الـ body ناقص أو فيه خطأ Validation، بس هاد الفحص اليدوي حماية إضافية
            if (userSaveDto == null)
            {
                return BadRequest("User data is required.");
            }

            try
            {
                int newUserId = await _userService.AddNewUserAsync(userSaveDto);

                return CreatedAtRoute("GetUserById", new { userId = newUserId }, newUserId);
            }
            catch (ArgumentException ex)
            {
                // بزنس فاليديشن فشل (زي username مكرر) - رسالة واضحة للمستخدم بدل 500
                return BadRequest(ex.Message);
            }
        }


        /// <summary>
        /// تحديث بيانات مستخدم في النظام
        /// </summary>
        /// <param name="userId">معرف المستخدم المراد تحديثه (من الـ Route)</param>
        /// <param name="userSaveDto">بيانات المستخدم المحدثة</param>
        [HttpPut("{userId:int}", Name = "UpdateUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserSaveDTO userSaveDto)
        {
            if (userSaveDto == null)
            {
                return BadRequest("User data is required.");
            }

            // تأكيد إنه الـ ID بالـ Route مطابق للـ ID بالـ Body
            if (userId != userSaveDto.UserId)
            {
                return BadRequest("Mismatched user id between route and body.");
            }

            try
            {
                bool isUpdated = await _userService.UpdateUserAsync(userSaveDto);

                if (!isUpdated)
                {
                    return NotFound($"No user found with id {userId}.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// حذف مستخدم من النظام
        /// </summary>
        /// <param name="userId">معرف المستخدم المراد حذفه</param>
        [HttpDelete("{userId:int}", Name = "DeleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID.");
            }

            bool isDeleted = await _userService.DeleteUserAsync(userId);

            if (!isDeleted)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            return NoContent();

            // ملاحظة: ما في try-catch هون لأنه ما في قاعدة بزنس متوقعة ممكن تفشل بعملية الحذف
            // (زي username مكرر بالإضافة/التعديل). أي استثناء غير متوقع (خطأ داتابيز مثلاً)
            // رح يلتقطه الـ Global Exception Middleware ويرجع 500 نظيف تلقائياً
        }


        /// <summary>
        /// تغيير كلمة مرور مستخدم بعد التحقق من كلمة المرور الحالية
        /// </summary>
        /// <param name="userId">معرف المستخدم</param>
        /// <param name="dto">كلمة المرور الحالية والجديدة</param>
        [HttpPut("{userId:int}/change-password", Name = "ChangePassword")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordDTO dto)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid user ID.");
            }

            try
            {
                bool isChanged = await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);

                if (!isChanged)
                {
                    // كلمة المرور الحالية غلط - 400 لأنه خطأ إدخال من المستخدم، مش خطأ سيرفر
                    return BadRequest("Old password is incorrect.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                // المستخدم غير موجود بالنظام أصلاً
                return NotFound(ex.Message);
            }
        }
    }
}