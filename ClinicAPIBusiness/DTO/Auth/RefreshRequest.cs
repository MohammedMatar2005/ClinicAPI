using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicAPIBusiness.DTO.Auth
{

    public class RefreshRequest
    {
        public string RefreshToken { get; set; }
        public string username { get; set; }
    }

}
