using System;
using System.Security.Cryptography;

namespace ClinicAPIBusiness.Helpers
{
    public static class InvoiceNumberGenerator
    {
        /// <summary>
        /// توليد رقم فاتورة ذكي وله معنى دلالي وقابل للتتبع المالي
        /// </summary>
        /// <param name="departmentCode">كود القسم الاختياري (الافتراضي: CLIN)</param>
        /// <returns>رقم الفاتورة بتنسيق: INV-YYYYMMDD-DEPT-XXXX</returns>
        public static string GenerateSemanticInvoiceNumber(string departmentCode = "CLIN")
        {
            // 1. الجزء الدلالي الزمني: تاريخ اليوم بالسنة والشهر واليوم
            string datePart = DateTime.Now.ToString("yyyyMMdd");

            // 2. الجزء الدلالي الهيكلي: تنظيف واختصار كود القسم ليكون 3 إلى 4 حروف
            string deptPart = string.IsNullOrWhiteSpace(departmentCode)
                ? "GEN"
                : departmentCode.Trim().ToUpper();

            if (deptPart.Length > 4)
            {
                deptPart = deptPart.Substring(0, 4);
            }

            // 3. الجزء العشوائي الآمن (Cryptographically Secure Random) لمنع التصادم والتكرار
            string randomPart = GetRandomHexString(4);

            // 4. تجميع الرقم النهائي المعبر
            return $"INV-{datePart}-{deptPart}-{randomPart}";
        }

        /// <summary>
        /// دالة مساعدة لتوليد نص هكسا ديسيمال عشوائي وفريد
        /// </summary>
        private static string GetRandomHexString(int length)
        {
            byte[] bytes = new byte[length / 2];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}