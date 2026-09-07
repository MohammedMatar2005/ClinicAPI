using System;
using System.Collections.Generic;

namespace ClinicAPIBusiness.Models;

public partial class InvoiceStatus
{
    public byte StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
