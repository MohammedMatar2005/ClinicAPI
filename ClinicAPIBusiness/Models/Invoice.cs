using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int VisitId { get; set; }

    public string InvoiceNumber { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }

    public decimal ConsultationFee { get; set; }

    public decimal LabTestFee { get; set; }

    public decimal ProcedureFee { get; set; }

    public decimal OtherCharges { get; set; }

    public decimal? TaxPercentage { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public decimal? DiscountAmount { get; set; }

    public byte StatusId { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsActive { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? FinalAmount { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual InvoiceStatus Status { get; set; } = null!;

    public virtual PatientVisit Visit { get; set; } = null!;
}
