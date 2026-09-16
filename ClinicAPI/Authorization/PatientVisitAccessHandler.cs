namespace ClinicAPI.Authorization
{
    using ClinicAPIBusiness.DTO.PatientVisitsDTOs;
    using ClinicAPIBusiness.Services;
    using Microsoft.AspNetCore.Authorization;
    using System.Security.Claims;

    public class PatientVisitAccessRequirement : IAuthorizationRequirement { }

    public class PatientVisitAccessHandler : AuthorizationHandler<PatientVisitAccessRequirement, PatientVisitViewDTO>
    {
        private readonly clsDoctor _doctorService;
        private readonly clsAppointment _appointmentService;

        public PatientVisitAccessHandler(clsDoctor doctorService, clsAppointment appointmentService)
        {
            _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
            _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PatientVisitAccessRequirement requirement,
            PatientVisitViewDTO visit)
        {
            // 1. الآدمن والاستقبال يملكون صلاحية كاملة لرؤية جميع الزيارات
            if (context.User.IsInRole("Admin") || context.User.IsInRole("Receptionist"))
            {
                context.Succeed(requirement);
                return;
            }

            // 2. استخراج الـ User ID الخاص بالمستخدم الحالي من الـ Token
            var userIdClaim = context.User.FindFirstValue("UserId")
                              ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return;
            }

            // 3. جلب الموعد المرتبط بالزيارة بأمان
            var appointment = await _appointmentService.GetAppointmentByIdAsync(visit.AppointmentId);
            if (appointment == null)
            {
                return; // إذا لم يُجد الموعد، تفشل السياسة تلقائياً
            }

            // 4. جلب بيانات الطبيب (تأكد هل تمرر DoctorId أو UserId حسب تصميم الداتابيز لديك)
            var doctor = await _doctorService.GetDoctorByIdAsync(appointment.DoctorId);
            // ملاحظة: لو كنت متأكد أن appointment.DoctorId هو نفسه UserId، استخدم GetDoctorByUserIdAsync

            if (doctor == null || doctor.User == null)
            {
                return;
            }

            // 5. المقارنة النهائية بين الـ User ID للطبيب وصاحب الطلب الحالي
            if (doctor.User.UserId == currentUserId)
            {
                context.Succeed(requirement);
            }
        }
    }
}