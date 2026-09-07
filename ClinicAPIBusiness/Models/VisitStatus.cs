using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class VisitStatus
{
    public byte StatusId { get; set; }

    public string StatusTitle { get; set; } = null!;

    public virtual ICollection<PatientVisit> PatientVisits { get; set; } = new List<PatientVisit>();
}
