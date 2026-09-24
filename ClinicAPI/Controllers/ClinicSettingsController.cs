using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Route("api/ClinicSettings")]
    [ApiController]
    public class ClinicSettingsController : ControllerBase
    {
        private readonly clsClinicSettings _settingsService;
        private readonly ILogger<ClinicSettingsController> _logger;

        public ClinicSettingsController(clsClinicSettings settingsService, ILogger<ClinicSettingsController> logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// جلب إعدادات العيادة العامة والمعلومات الأساسية
        /// </summary>
        /// <returns>كائن إعدادات العيادة</returns>
        [HttpGet(Name = "GetClinicSettings")]
        [ProducesResponseType(typeof(ClinicSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClinicSettings>> GetClinicSettings()
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (settings == null)
            {
                return NotFound("Clinic settings record not found.");
            }

            return Ok(settings);
        }

        /// <summary>
        /// تحديث إعدادات العيادة (الشعار، أوقات العمل، النسبة المئوية للضريبة، بيانات الاتصال)
        /// </summary>
        /// <param name="settings">بيانات الإعدادات المحدثة</param>
        [Authorize(Roles = "Admin")]
        [HttpPut(Name = "UpdateClinicSettings")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateClinicSettings([FromBody] ClinicSettings settings)
        {
            if (settings == null)
            {
                return BadRequest("Clinic settings data is required.");
            }

            try
            {
                bool isUpdated = await _settingsService.UpdateClinicSettingsAsync(settings);

                if (!isUpdated)
                {
                    _logger.LogWarning("Clinic settings record could not be updated or does not exist.");
                    return NotFound("Clinic settings record could not be updated or does not exist.");
                }

                _logger.LogInformation("Clinic settings updated successfully.");
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Failed to update clinic settings due to business validation error: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}