using System;

namespace ClinicAPIBusiness.DTO.PeopleDTOs
{
    public class PersonViewDTO
    {
        public int PersonId { get; set; }
        public string NationalNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address {  get; set; } = string.Empty;
    }
}