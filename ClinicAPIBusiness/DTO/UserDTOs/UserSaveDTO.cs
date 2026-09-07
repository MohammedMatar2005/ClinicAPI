
using ClinicAPIBusiness.DTO.PeopleDTOs;
using ClinicAPIBusiness.Models;
using ClinicAPIBusiness.Services;

namespace ClinicAPIBusiness.DTO.UsersDTOs
{
    public class UserSaveDTO
    {
        public PersonSaveDTO Person { get; set; } = new PersonSaveDTO();
        public UserDetailsDTO UserDetails { get; set; } = new UserDetailsDTO();
    }
}