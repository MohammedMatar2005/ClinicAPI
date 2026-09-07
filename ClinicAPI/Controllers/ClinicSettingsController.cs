using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicAPI.Controllers
{
    [Route("api/ClinicSettings")]
    [ApiController]
    public class ClinicSettingsController : ControllerBase
    {
        private readonly clsClinicSettings _settingsService;

        public ClinicSettingsController(clsClinicSettings settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
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
                    return NotFound("Clinic settings record could not be updated or does not exist.");
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}