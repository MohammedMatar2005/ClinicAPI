using ClinicAPIBusiness.DTO.PeopleDTOs;
using ClinicAPIBusiness.Models;
using System;

namespace ClinicAPIBusiness.DTO.PatientsDTOs
{
    public class PatientSaveDTO
    {
       
         public PersonSaveDTO Person { get; set; } = new PersonSaveDTO();
         public PatientViewDTO PatientDetails { get; set; }


        

    }     

}