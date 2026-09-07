using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public decimal PaymentAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public int PaymentStatusId { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual PaymentStatus PaymentStatus { get; set; } = null!;
}
