using ClinicAPIBusiness.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicAPIBusiness.Models 
{
    [Table("SystemLogs")]
    public class SystemLog
    {
        [Key]
        public int LogID { get; set; }

        [Required]
        [StringLength(150)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public byte SeverityLevel { get; set; } 

        public DateTime LogTimestamp { get; set; } = DateTime.Now;

        public int? CreatedByUserID { get; set; }

        [ForeignKey("CreatedByUserID")]
        public virtual User? CreatorUser { get; set; }
    }
}