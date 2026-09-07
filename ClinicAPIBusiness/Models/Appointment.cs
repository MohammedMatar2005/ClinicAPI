using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateTime AppointmentDate { get; set; }

    public string? ReasonForVisit { get; set; }

    public int StatusId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PatientVisit> PatientVisits { get; set; } = new List<PatientVisit>();

    public virtual AppointmentStatus Status { get; set; } = null!;
}
