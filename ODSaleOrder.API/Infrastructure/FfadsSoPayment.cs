using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Infrastructure;

public partial class FfadsSoPayment
{
    public Guid Id { get; set; }

    public string OrderRefNumber { get; set; }

    public string VisitId { get; set; }

    public string External_OrdNBR { get; set; }

    public bool? IsDirect { get; set; }

    public string OrderType { get; set; }

    public string CustomerId { get; set; }

    public string CustomerShiptoId { get; set; }

    public string CustomerShiptoName { get; set; }

    public string CustomerName { get; set; }

    public string CustomerAddress { get; set; }

    public string CustomerPhone { get; set; }

    public double? Orig_Ord_Extend_Amt { get; set; }

    public string PaymentType { get; set; }

    public double? PaymentValue { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string CreatedBy { get; set; }

    public string UpdatedBy { get; set; }
}
