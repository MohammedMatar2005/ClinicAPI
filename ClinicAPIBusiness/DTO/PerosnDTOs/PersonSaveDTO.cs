using System;

namespace ClinicAPIBusiness.DTO.PeopleDTOs
{
    public class PersonSaveDTO
    {
        public int PersonId { get; set; } // 0 في الإضافة ويحمل المعرف في التعديل
        public string NationalNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? SecondName { get; set; }
        public string? ThirdName { get; set; }
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {SecondName} {ThirdName} {LastName}".Replace("  ", " ").Trim();
        public bool Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}