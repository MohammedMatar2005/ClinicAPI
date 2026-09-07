using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class PatientVisit
{
    public int VisitId { get; set; }

    public int AppointmentId { get; set; }

    public DateTime VisitDate { get; set; }

    public string? Symptoms { get; set; }

    public string? Diagnosis { get; set; }

    public string? TreatmentPlan { get; set; }

    public string? BloodPressure { get; set; }

    public decimal? Temperature { get; set; }

    public int? HeartRate { get; set; }

    public int? RespiratoryRate { get; set; }

    public decimal? Weight { get; set; }

    public decimal? Height { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public byte VisitStatusId { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public virtual VisitStatus VisitStatus { get; set; } = null!;
}
