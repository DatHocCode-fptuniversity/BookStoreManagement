using System;
using System.Collections.Generic;

namespace BookStoreManagement.Entities;

public partial class InvoiceDetail
{
    public int InvoiceDetailId { get; set; }

    public int InvoiceId { get; set; }

    public int BookId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual Invoice Invoice { get; set; } = null!;
}
