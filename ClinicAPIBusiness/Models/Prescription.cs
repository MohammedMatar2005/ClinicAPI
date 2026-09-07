using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class Prescription
{
    public int PrescriptionId { get; set; }

    public int VisitId { get; set; }

    public string MedicineName { get; set; } = null!;

    public string Dosage { get; set; } = null!;

    public string Frequency { get; set; } = null!;

    public int? Duration { get; set; }

    public string? Instructions { get; set; }

    public int? Quantity { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual PatientVisit Visit { get; set; } = null!;
}
